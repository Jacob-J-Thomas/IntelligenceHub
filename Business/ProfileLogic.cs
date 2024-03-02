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
using OpenAICustomFunctionCallingAPI.API.DTOs;
using System.Reflection.Metadata;

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
            _validationLogic = new ValidationLogic();
        }

        // else shouldn't be required here
        public async Task<APIProfileDTO> GetProfile(string name)
        {
            var dbProfile = await _profileDb.GetByNameWithToolsAsync(name);
            if (dbProfile != null)
            {
                var apiProfile = dbProfile;

                // package this into a seperate method (same one as in GetAllProfiles())
                var profileToolDTOs = await _profileToolsDb.GetToolIdsAsync(apiProfile.Id);
                apiProfile.Tools = new List<Tool>();
                foreach (var association in profileToolDTOs)
                {
                    var tool = await _toolDb.GetToolByIdAsync(association.ToolID);
                    apiProfile.Tools.Add(tool);
                }
                return apiProfile;
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
                foreach (var dbProfile in response)
                {
                    var apiProfile = new APIProfileDTO(dbProfile);

                    // package this into a seperate method (same one as in GetProfiles())
                    var profileToolDTOs = await _profileToolsDb.GetToolIdsAsync(apiProfile.Id);
                    apiProfile.Tools = new List<Tool>();
                    foreach (var association in profileToolDTOs)
                    {
                        var tool = await _toolDb.GetToolByIdAsync(association.ToolID);
                        apiProfile.Tools.Add(tool);
                    }
                    apiResponseList.Add(apiProfile);
                }
            }
            return apiResponseList;
        }

        // fix this
        public async Task<string> CreateOrUpdateProfile(APIProfileDTO profileDto)
        {
            var errorMessage = _validationLogic.ValidateProfile(profileDto); // move to controller
            if (errorMessage != null)
            {
                return errorMessage;
            }
            var existingProfile = await _profileDb.GetByNameWithToolsAsync(profileDto.Name);
            if (existingProfile != null)
            {
                var existingProfileDbDTO = new DbProfileDTO(createNew: false, existingProfile);
                var profileDbDTO = new DbProfileDTO(existingProfile, profileDto);
                await _profileDb.UpdateAsync(existingProfileDbDTO, profileDbDTO);
                var success = await AddOrUpdateProfileTools(profileDto, existingProfile);
                if (!success)
                {
                    return "Failed to add or update the tools in the database";
                }
            }
            else
            {
                var toolLessProfile = await _profileDb.GetByNameAsync(profileDto.Name);
                if (toolLessProfile != null)
                {
                    var toolLessDbProfileDTO = new APIProfileDTO(toolLessProfile);
                    var profileDbDTO = new DbProfileDTO(toolLessDbProfileDTO, profileDto);
                    await _profileDb.UpdateAsync(toolLessProfile, profileDbDTO);
                    var success = await AddOrUpdateProfileTools(profileDto, toolLessDbProfileDTO);
                    if (!success)
                    {
                        return "Failed to add or update the tools in the database";
                    }
                }
                else
                {
                    var profileDbDTO = new DbProfileDTO(createNew: true, profileDto);
                    var newProfile = await _profileDb.AddAsync(profileDbDTO);
                    var newProfileDTO = new APIProfileDTO(newProfile);
                    var success = await AddOrUpdateProfileTools(profileDto, newProfileDTO);
                    if (!success)
                    {
                        return "Failed to add or update the tools in the database";
                    }
                }
            }
            return null;
        }

        public async Task<string> DeleteProfile(string name)
        {
            var profileDto = await _profileDb.GetByNameWithToolsAsync(name);
            if (profileDto == null)
            {
                return $"No profile with the name '{name}' was found.";
            }

            if (profileDto.Tools != null)
            {
                await _profileToolsDb.DeleteAllProfileAssociationsAsync(profileDto.Id);
            }
            var rows = await _profileDb.DeleteAsync(new DbProfileDTO(createNew: false, profileDto));
            if (rows != 1)
            {
                return "something went wrong while deleting the tool. Please note that the profile associations were deleted successfully.";
            }
            return null;
        }

        #region 
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

        public async Task<Tool> GetTool(string name)
        {
            return await _toolDb.GetToolByNameAsync(name);
        }

        public async Task<IEnumerable<Tool>> GetAllTools()
        {
            var returnList = new List<Tool>();
            var dbTools = await _toolDb.GetAllAsync();
            foreach (var tool in dbTools)
            {
                var properties = await _propertyDb.GetToolProperties(tool.Id);
                returnList.Add(new Tool(tool, properties));
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

        public async Task<string> CreateOrUpdateTools(List<Tool> toolList)
        {
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
                    var newToolDTO = new Tool(newTool);
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
                    var dto = new DbPropertyDTO(existingTool.Id, property.Key, property.Value);
                    await _propertyDb.DeleteAsync(dto);
                }
                await _profileToolsDb.DeleteAllToolAssociationsAsync(existingTool.Id);
                return await _toolDb.DeleteAsync(new DbToolDTO(existingTool)) == 1;
            }
            return false;
        }

        public async Task<bool> AddOrUpdateToolProperties(Tool existingTool, Dictionary<string, PropertyDTO> newProperties)
        {
            var existingProperties = await _propertyDb.GetToolProperties(existingTool.Id);
            foreach (var property in existingProperties)
            {
                await _propertyDb.DeleteAsync(property);
            }

            foreach (var property in newProperties)
            {
                await _propertyDb.AddAsync(new DbPropertyDTO(existingTool.Id, property.Key, property.Value));
            }
            return true;
        }
        #endregion
    }
}
