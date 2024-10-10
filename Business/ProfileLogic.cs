using IntelligenceHub.DAL;
using IntelligenceHub.DAL.Models;
using IntelligenceHub.Common.Handlers;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.Tools;

namespace IntelligenceHub.Business
{
    // this whole class needs some refactoring
    public class ProfileLogic
    {
        private readonly ProfileRepository _profileDb;
        private readonly ProfileToolsAssociativeRepository _profileToolsDb;
        private readonly ToolRepository _toolDb;
        private readonly PropertyRepository _propertyDb;

        //private readonly ToolLogic _toolLogic;
        private readonly ProfileAndToolValidationHandler _validationLogic;
        public ProfileLogic(string connectionString)
        {
            _profileDb = new ProfileRepository(connectionString);
            _profileToolsDb = new ProfileToolsAssociativeRepository(connectionString);
            _toolDb = new ToolRepository(connectionString);
            _propertyDb = new PropertyRepository(connectionString); 

            //_toolLogic = new ToolLogic(connectionString);
            _validationLogic = new ProfileAndToolValidationHandler(); // move this and any logic to controller
        }

        // else shouldn't be required here
        public async Task<Profile> GetProfile(string name)
        {
            var dbProfile = await _profileDb.GetByNameWithToolsAsync(name);
            if (dbProfile is not null)
            {
                // package this into a seperate method (same one as in GetAllProfiles())
                var profileToolDTOs = await _profileToolsDb.GetToolAssociationsAsync(dbProfile.Id);
                dbProfile.Tools = new List<Tool>();
                foreach (var association in profileToolDTOs) dbProfile.Tools.Add(await _toolDb.GetToolByIdAsync(association.ToolID));
                return dbProfile;
            }
            else
            {
                var apiProfile = await _profileDb.GetByNameAsync(name);
                if (apiProfile != null) return DbMappingHandler.MapFromDbProfile(apiProfile);
            }
            return null;
        }

        public async Task<IEnumerable<Profile>> GetAllProfiles()
        {
            var response = await _profileDb.GetAllAsync();
            var apiResponseList = new List<Profile>();
            if (response is not null)
            {
                foreach (var profile in response)
                {
                    // package this into a seperate method (same one as in GetProfiles())
                    var apiProfileDto = DbMappingHandler.MapFromDbProfile(profile);
                    var profileToolDTOs = await _profileToolsDb.GetToolAssociationsAsync(profile.Id);
                    apiProfileDto.Tools = new List<Tool>();
                    foreach (var association in profileToolDTOs)
                    {
                        var tool = await _toolDb.GetToolByIdAsync(association.ToolID);
                        apiProfileDto.Tools.Add(tool);
                    }
                    apiResponseList.Add(apiProfileDto);
                }
            }
            return apiResponseList;
        }

        // refactor this
        public async Task<string> CreateOrUpdateProfile(Profile profileDto)
        {
            var errorMessage = _validationLogic.ValidateAPIProfile(profileDto); // move to controller
            if (errorMessage is not null) return errorMessage;
            var existingProfile = await _profileDb.GetByNameAsync(profileDto.Name);
            var success = true;
            if (existingProfile is not null)
            {
                var updateProfileDto = DbMappingHandler.MapToDbProfile(existingProfile, profileDto);
                var rows = await _profileDb.UpdateAsync(existingProfile, updateProfileDto);
                if (rows != 1) success = false;
            }
            else
            {
                var updateProfileDto = DbMappingHandler.MapToDbProfile(null, profileDto);
                var newTool = await _profileDb.AddAsync(updateProfileDto);
                if (newTool is null) success = false;
            }

            if (profileDto.Tools is not null && profileDto.Tools.Count > 0)
            {
                if (existingProfile is null) await AddOrUpdateProfileTools(profileDto, null);
                else await AddOrUpdateProfileTools(profileDto, DbMappingHandler.MapFromDbProfile(existingProfile));
            }
            var existingProfileWithTools = await _profileDb.GetByNameWithToolsAsync(profileDto.Name);
            if (success && existingProfileWithTools is not null) success = await AddOrUpdateProfileTools(profileDto, existingProfileWithTools);
            if (!success) return $"Something went wrong...";
            return null;
        }

        // could also use some refactoring
        public async Task<string> DeleteProfile(string name)
        {
            var profileDto = await _profileDb.GetByNameWithToolsAsync(name);
            int rows;
            if (profileDto is not null)
            {
                if (profileDto.Tools is not null && profileDto.Tools.Count > 0) await _profileToolsDb.DeleteAllProfileAssociationsAsync(profileDto.Id);
                var dbProfileDTO = new DbProfile()
                {
                    Id = profileDto.Id,
                    Name = profileDto.Name
                };
                rows = await _profileDb.DeleteAsync(dbProfileDTO);
            }
            else
            {
                var toolLessProfile = await _profileDb.GetByNameAsync(name);
                if (toolLessProfile is null) return $"No profile with the name '{name}' was found.";
                rows = await _profileDb.DeleteAsync(toolLessProfile);
            }
            if (rows != 1) return "something went wrong while deleting the tool. Please note that the profile associations were deleted successfully.";
            return null;
        }

        #region Tool Logic
        private async Task<bool> AddOrUpdateProfileTools(Profile profileDto, Profile existingProfile)
        {
            var toolIds = new List<int>();
            await _profileToolsDb.DeleteAllProfileAssociationsAsync(existingProfile.Id);
            if (profileDto.Tools is not null && profileDto.Tools.Count > 0)
            {
                await CreateOrUpdateTools(profileDto.Tools);
                foreach (var tool in profileDto.Tools)
                {
                    var dbTool = await _toolDb.GetToolByNameAsync(tool.Function.Name);
                    toolIds.Add(dbTool.Id);
                }
                await _profileToolsDb.AddAssociationsByProfileIdAsync(existingProfile.Id, toolIds);
            }
            return true;
        }

        public async Task<Tool> GetTool(string name)
        {
            return await _toolDb.GetToolByNameAsync(name);
        }

        public async Task<IEnumerable<Tool>> GetAllTools()
        {
            var returnList = new List<Tool>();
            var dbTools = await _toolDb.GetAllAsync();
            foreach (var dbTool in dbTools)
            {
                var properties = await _propertyDb.GetToolProperties(dbTool.Id);
                var tool = DbMappingHandler.MapFromDbTool(dbTool, properties);
                returnList.Add(tool);
            }
            return returnList;
        }

        public async Task<List<string>> GetProfileToolAssociations(string name)
        {
            var profileNames = await _toolDb.GetProfileToolsAsync(name);
            if (profileNames.Count > 0) return profileNames;
            else return null;
        }

        public async Task<List<string>> GetToolProfileAssociations(string name)
        {
            var profileNames = await _toolDb.GetToolProfilesAsync(name);
            if (profileNames.Count > 0) return profileNames;
            else return null;
        }

        public async Task<string> CreateOrUpdateTools(List<Tool> toolList)
        {
            // move below to controller
            foreach (var tool in toolList)
            {
                var errorMessage = _validationLogic.ValidateTool(tool);
                if (errorMessage is not null) return errorMessage;
            }
            foreach (var tool in toolList)
            {
                var dbToolDTO = DbMappingHandler.MapToDbTool(tool);
                var existingToolDTO = await _toolDb.GetToolByNameAsync(tool.Function.Name);
                if (existingToolDTO != null)
                {
                    var existingTool = DbMappingHandler.MapToDbTool(existingToolDTO);
                    await _toolDb.UpdateAsync(existingTool, dbToolDTO);
                    await AddOrUpdateToolProperties(existingToolDTO, tool.Function.Parameters.Properties);
                }
                else
                {
                    var newDbTool = await _toolDb.AddAsync(dbToolDTO);
                    var newTool = DbMappingHandler.MapFromDbTool(newDbTool);
                    await AddOrUpdateToolProperties(newTool, tool.Function.Parameters.Properties);
                }
            }
            return null;
        }

        public async Task<string> AddToolToProfiles(string name, List<string> profiles)
        {
            var tool = await _toolDb.GetByNameAsync(name);
            if (tool is not null)
            {
                var success = await _profileToolsDb.AddAssociationsByToolIdAsync(tool.Id, profiles);
                if (success) return null;
                else return "something went wrong...";
            }
            return $"No tool with the name {name} exists.";
        }

        public async Task<string> AddProfileToTools(string name, List<string> tools)
        {
            var toolIDs = new List<int>();
            foreach (var toolName in tools)
            {
                var tool = await _toolDb.GetByNameAsync(toolName);
                if (tool is null) return $"No tool '{toolName}' found.";
                toolIDs.Add(tool.Id);
            }
            var profile = await _profileDb.GetByNameAsync(name);
            if (profile is not null && toolIDs.Count > 0)
            {
                var success = await _profileToolsDb.AddAssociationsByProfileIdAsync(profile.Id, toolIDs);
                if (success) return null;
                else return $"No profile {profile.Name} found.";
            }
            return $"The tools or profile in the request body were not found.";
        }

        public async Task<string> DeleteToolAssociations(string name, List<string> profiles)
        {
            var tool = await _toolDb.GetByNameAsync(name);
            if (tool is not null)
            {
                foreach (var profile in profiles) await _profileToolsDb.DeleteToolAssociationAsync(tool.Id, profile);
                return null;
            }
            return $"No tool with the name {name} exists.";
        }

        public async Task<string> DeleteProfileAssociations(string name, List<string> tools)
        {
            var profile = await _profileDb.GetByNameAsync(name);
            if (profile is not null)
            {
                foreach (var tool in tools) await _profileToolsDb.DeleteProfileAssociationAsync(profile.Id, tool);
                return null;
            }
            return $"No tool with the name {name} exists.";
        }

        public async Task<bool> DeleteTool(string name)
        {
            var existingTool = await _toolDb.GetToolByNameAsync(name);
            if (existingTool is not null)
            {
                foreach (var property in existingTool.Function.Parameters.Properties)
                {
                    var propertyDTO = DbMappingHandler.MapToDbProperty(property.Key, property.Value);
                    await _propertyDb.DeleteAsync(propertyDTO);
                }
                await _profileToolsDb.DeleteAllToolAssociationsAsync(existingTool.Id);
                var dbTool = DbMappingHandler.MapToDbTool(existingTool);
                return await _toolDb.DeleteAsync(dbTool) == 1;
            }
            return false;
        }

        public async Task<bool> AddOrUpdateToolProperties(Tool existingTool, Dictionary<string, Property> newProperties)
        {
            var existingProperties = await _propertyDb.GetToolProperties(existingTool.Id);
            foreach (var property in existingProperties) await _propertyDb.DeleteAsync(property);
            foreach (var property in newProperties)
            {
                property.Value.Id = existingTool.Id;
                await _propertyDb.AddAsync(DbMappingHandler.MapToDbProperty(property.Key, property.Value));
            }
            return true;
        }
        #endregion
    }
}
