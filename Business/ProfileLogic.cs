using OpenAICustomFunctionCallingAPI.Client;
using OpenAICustomFunctionCallingAPI.Controllers.DTOs;
using Newtonsoft.Json.Linq;
using OpenAICustomFunctionCallingAPI.Host.Config;
//using OpenAICustomFunctionCallingAPI.DAL;
using Azure;
using Microsoft.Data.SqlClient;
using OpenAICustomFunctionCallingAPI.DAL;
using System.Web.Mvc;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Nest;
using OpenAICustomFunctionCallingAPI.DAL.DTOs;
using System.Reflection.Metadata;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.ToolDTOs;

namespace OpenAICustomFunctionCallingAPI.Business.ProfileLogic
{
    // this whole class needs some refactoring
    public class ProfileLogic
    {
        private readonly ProfileRepository _profileDb;
        private readonly ProfileToolsRepository _profileToolsDb;
        private readonly ToolRepository _toolDb;
        private readonly PropertyRepository _propertyDb;

        //private readonly ToolLogic _toolLogic;
        private readonly ValidationLogic _validationLogic;
        public ProfileLogic(string connectionString)
        {
            _profileDb = new ProfileRepository(connectionString);
            _profileToolsDb = new ProfileToolsRepository(connectionString);
            _toolDb = new ToolRepository(connectionString);
            _propertyDb = new PropertyRepository(connectionString); 

            //_toolLogic = new ToolLogic(connectionString);
            _validationLogic = new ValidationLogic(); // move this and any logic to controller
        }

        // else shouldn't be required here
        public async Task<APIProfileDTO> GetProfile(string name)
        {
            var dbProfile = await _profileDb.GetByNameWithToolsAsync(name);
            if (dbProfile != null)
            {
                // package this into a seperate method (same one as in GetAllProfiles())
                var profileToolDTOs = await _profileToolsDb.GetToolAssociationsAsync(dbProfile.Id);
                dbProfile.Tools = new List<ToolDTO>();
                foreach (var association in profileToolDTOs)
                {
                    var tool = await _toolDb.GetToolByIdAsync(association.ToolID);
                    dbProfile.Tools.Add(tool);
                }
                return dbProfile;
            }
            else
            {
                var apiProfile = await _profileDb.GetByNameAsync(name);
                if (apiProfile != null)
                {
                    return new APIProfileDTO(apiProfile);
                }
            }
            return null;
        }

        // might need to return as pageable if db starts getting big
        public async Task<IEnumerable<APIProfileDTO>> GetAllProfiles()
        {
            var response = await _profileDb.GetAllAsync();
            var apiResponseList = new List<APIProfileDTO>();
            if (response != null)
            {
                foreach (var profile in response)
                {
                    // package this into a seperate method (same one as in GetProfiles())
                    var apiProfileDto = new APIProfileDTO(profile);
                    var profileToolDTOs = await _profileToolsDb.GetToolAssociationsAsync(profile.Id);
                    apiProfileDto.Tools = new List<ToolDTO>();
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

        public async Task<string> CreateOrUpdateProfile(APIProfileDTO profileDto)
        {
            var errorMessage = _validationLogic.ValidateAPIProfile(profileDto); // move to controller
            if (errorMessage != null)
            {
                return errorMessage;
            }

            var existingProfile = await _profileDb.GetByNameAsync(profileDto.Name);
            var success = true;
            if (existingProfile != null)
            {
                var updateProfileDto = new DbProfileDTO(existingProfile, profileDto);
                var rows = await _profileDb.UpdateAsync(existingProfile, updateProfileDto);
                if (rows != 1)
                {
                    success = false;
                }
            }
            else
            {
                var updateProfileDto = new DbProfileDTO(null, profileDto);
                var newTool = await _profileDb.AddAsync(updateProfileDto);
                if (newTool == null)
                {
                    success = false;
                }
            }

            if (profileDto.Tools != null && profileDto.Tools.Count > 0)
            {
                if (existingProfile == null)
                {
                    await AddOrUpdateProfileTools(profileDto, null);
                }
                else
                {
                    await AddOrUpdateProfileTools(profileDto, new APIProfileDTO(existingProfile));
                }
            }

            var existingProfileWithTools = await _profileDb.GetByNameWithToolsAsync(profileDto.Name);
            if (success && existingProfileWithTools != null)
            {
                success = await AddOrUpdateProfileTools(profileDto, existingProfileWithTools);
                
            }

            if (!success)
            {
                return $"Something went wrong...";
            }
            return null;
        }

        // could use some refactoring
        public async Task<string> DeleteProfile(string name)
        {
            var profileDto = await _profileDb.GetByNameWithToolsAsync(name);
            int rows;
            if (profileDto != null)
            {
                if (profileDto.Tools != null)
                {
                    await _profileToolsDb.DeleteAllProfileAssociationsAsync(profileDto.Id);
                }
                var dbProfileDTO = new DbProfileDTO()
                {
                    Id = profileDto.Id,
                    Name = profileDto.Name
                };
                rows = await _profileDb.DeleteAsync(dbProfileDTO); 
                
            }
            else
            {
                var toolLessProfile = await _profileDb.GetByNameAsync(name);
                if (toolLessProfile == null)
                {
                    return $"No profile with the name '{name}' was found.";
                }
                rows = await _profileDb.DeleteAsync(toolLessProfile);
            }
            if (rows != 1)
            {
                return "something went wrong while deleting the tool. Please note that the profile associations were deleted successfully.";
            }
            return null;
        }

        #region Tool Logic
        private async Task<bool> AddOrUpdateProfileTools(APIProfileDTO profileDto, APIProfileDTO existingProfile)
        {
            var toolIds = new List<int>();
            await _profileToolsDb.DeleteAllProfileAssociationsAsync(existingProfile.Id);
            if (profileDto.Tools != null)
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

        public async Task<ToolDTO> GetTool(string name)
        {
            return await _toolDb.GetToolByNameAsync(name);
        }

        public async Task<IEnumerable<ToolDTO>> GetAllTools()
        {
            var returnList = new List<ToolDTO>();
            var dbTools = await _toolDb.GetAllAsync();
            foreach (var tool in dbTools)
            {
                var properties = await _propertyDb.GetToolProperties(tool.Id);
                returnList.Add(new ToolDTO(tool, properties));
            }
            return returnList;
        }

        public async Task<List<string>> GetToolProfileAssociations(string name)
        {
            var profileNames = await _toolDb.GetToolProfilesAsync(name);
            if (profileNames.Count > 0)
            {
                return profileNames;
            }
            return null;
        }

        public async Task<string> CreateOrUpdateTools(List<ToolDTO> toolList)
        {
            // move below to controller
            foreach (var tool in toolList)
            {
                var errorMessage = _validationLogic.ValidateTool(tool);
                if (errorMessage != null)
                {
                    return errorMessage;
                }
            }
            foreach (var tool in toolList)
            {
                var dbToolDTO = new DbToolDTO(tool);
                var existingToolDTO = await _toolDb.GetToolByNameAsync(tool.Function.Name);
                if (existingToolDTO != null)
                {
                    var existingTool = new DbToolDTO(existingToolDTO);
                    await _toolDb.UpdateAsync(existingTool, dbToolDTO);
                    await AddOrUpdateToolProperties(existingToolDTO, tool.Function.Parameters.Properties);
                }
                else
                {
                    var newTool = await _toolDb.AddAsync(dbToolDTO);
                    var newToolDTO = new ToolDTO(newTool, null);
                    await AddOrUpdateToolProperties(newToolDTO, tool.Function.Parameters.Properties);
                }
            }
            return null;
        }

        public async Task<string> AddToolAssociations(string name, List<string> profiles)
        {
            var tool = await _toolDb.GetByNameAsync(name);
            if (tool != null)
            {
                var success = await _profileToolsDb.AddAssociationsByToolIdAsync(tool.Id, profiles);
                if (success)
                {
                    return null;
                }
                else
                {
                    return "something went wrong...";
                }
            }
            return $"No tool with the name {name} exists.";
        }

        public async Task<string> DeleteToolAssociations(string name, List<string> profiles)
        {
            var tool = await _toolDb.GetByNameAsync(name);
            if (tool != null)
            {
                foreach (var profile in profiles)
                {
                    await _profileToolsDb.DeleteAssociationAsync(tool.Id, profile);
                }
                return null;
            }
            return $"No tool with the name {name} exists.";
        }

        public async Task<bool> DeleteTool(string name)
        {
            var existingTool = await _toolDb.GetToolByNameAsync(name);
            if (existingTool != null)
            {
                foreach (var property in existingTool.Function.Parameters.Properties)
                {
                    var propertyDTO = new DbPropertyDTO(property.Key, property.Value);
                    await _propertyDb.DeleteAsync(propertyDTO);
                }
                await _profileToolsDb.DeleteAllToolAssociationsAsync(existingTool.Id);
                return await _toolDb.DeleteAsync(new DbToolDTO(existingTool)) == 1;
            }
            return false;
        }

        public async Task<bool> AddOrUpdateToolProperties(ToolDTO existingTool, Dictionary<string, PropertyDTO> newProperties)
        {
            var existingProperties = await _propertyDb.GetToolProperties(existingTool.Id);
            foreach (var property in existingProperties)
            {
                await _propertyDb.DeleteAsync(property);
            }

            foreach (var property in newProperties)
            {
                property.Value.Id = existingTool.Id;
                await _propertyDb.AddAsync(new DbPropertyDTO(property.Key, property.Value));
            }
            return true;
        }
        #endregion
    }
}
