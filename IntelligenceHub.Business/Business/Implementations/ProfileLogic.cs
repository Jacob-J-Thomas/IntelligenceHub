﻿using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.Business.Handlers;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.DAL;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.Business.Implementations
{
    public class ProfileLogic : IProfileLogic
    {
        private readonly IProfileRepository _profileDb;
        private readonly IProfileToolsAssociativeRepository _profileToolsDb;
        private readonly IToolRepository _toolDb;
        private readonly IPropertyRepository _propertyDb;
        private readonly IValidationHandler _validationLogic;

        private readonly string _unknownErroMessage = "something went wrong...";
        private readonly string _missingProfileMessage = "No profile with the specified name was found. Name: "; // make sure to use interpolation here
        private readonly string _missingToolMessage = "No tool with the specified name was found. Name: "; // make sure to use interpolation here

        public ProfileLogic(IProfileRepository profileDb, IProfileToolsAssociativeRepository profileToolsDb, IToolRepository toolDb, IPropertyRepository propertyDb, IValidationHandler validationLogic)
        {
            _profileDb = profileDb;
            _profileToolsDb = profileToolsDb;
            _toolDb = toolDb;
            _propertyDb = propertyDb;
            _validationLogic = validationLogic;
        }

        // else shouldn't be required here
        public async Task<Profile?> GetProfile(string name)
        {
            var dbProfile = await _profileDb.GetByNameAsync(name);
            if (dbProfile != null)
            {
                var profile = DbMappingHandler.MapFromDbProfile(dbProfile);
                // package this into a seperate method (same one as in GetAllProfiles())
                var profileToolDTOs = await _profileToolsDb.GetToolAssociationsAsync(dbProfile.Id);
                profile.Tools = new List<Tool>();
                foreach (var association in profileToolDTOs)
                {
                    var dbTool = await _toolDb.GetByIdAsync(association.ToolID);
                    if (dbTool == null) continue;
                    var mappedTool = DbMappingHandler.MapFromDbTool(dbTool);
                    profile.Tools.Add(mappedTool);
                }
                return profile;
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
            if (response != null)
            {
                foreach (var profile in response)
                {
                    // package this into a seperate method (same one as in GetProfiles())
                    var apiProfileDto = DbMappingHandler.MapFromDbProfile(profile);
                    var profileToolDTOs = await _profileToolsDb.GetToolAssociationsAsync(profile.Id);
                    apiProfileDto.Tools = new List<Tool>();
                    foreach (var association in profileToolDTOs)
                    {
                        var tool = await _toolDb.GetByIdAsync(association.ToolID);
                        if (tool == null) continue;
                        var mappedTool = DbMappingHandler.MapFromDbTool(tool);
                        apiProfileDto.Tools.Add(mappedTool);
                    }
                    apiResponseList.Add(apiProfileDto);
                }
            }
            return apiResponseList;
        }

        // refactor this
        public async Task<string?> CreateOrUpdateProfile(Profile profileDto)
        {
            var errorMessage = _validationLogic.ValidateAPIProfile(profileDto); // move to controller
            if (errorMessage != null) return errorMessage;
            var existingProfile = await _profileDb.GetByNameAsync(profileDto.Name);
            var success = true;
            if (existingProfile != null)
            {
                var updateProfileDto = DbMappingHandler.MapToDbProfile(existingProfile.Name, existingProfile, profileDto);
                var rows = await _profileDb.UpdateAsync(existingProfile, updateProfileDto);
                if (rows != 1) success = false;
            }
            else
            {
                var updateProfileDto = DbMappingHandler.MapToDbProfile(profileDto.Name, null, profileDto);
                var newTool = await _profileDb.AddAsync(updateProfileDto);
                if (newTool == null) success = false;
            }

            if (profileDto.Tools != null && profileDto.Tools.Count > 0)
            {
                if (existingProfile == null) success = await AddOrUpdateProfileTools(profileDto, null);
                else success = await AddOrUpdateProfileTools(profileDto, DbMappingHandler.MapFromDbProfile(existingProfile));
            }
            if (!success) return _unknownErroMessage;
            return null;
        }

        // could also use some refactoring
        public async Task<string> DeleteProfile(string name)
        {
            var dbProfile = await _profileDb.GetByNameAsync(name);
            int rows;
            if (dbProfile != null)
            {
                var tools = new List<Tool>();
                var dbTools = dbProfile.ProfileTools.Select(pt => pt.Tool).ToList();
                foreach (var tool in dbTools) tools.Add(DbMappingHandler.MapFromDbTool(tool));
                var profile = DbMappingHandler.MapFromDbProfile(dbProfile, tools);

                if (profile.Tools != null && profile.Tools.Count > 0) await _profileToolsDb.DeleteAllProfileAssociationsAsync(dbProfile.Id);

                rows = await _profileDb.DeleteAsync(dbProfile);
            }
            else
            {
                var toolLessProfile = await _profileDb.GetByNameAsync(name);
                if (toolLessProfile == null) return _missingProfileMessage + $"'{name}'";
                rows = await _profileDb.DeleteAsync(toolLessProfile);
            }
            if (rows != 1) return _unknownErroMessage;
            return null;
        }

        #region Tool Logic
        private async Task<bool> AddOrUpdateProfileTools(Profile profileDto, Profile existingProfile)
        {
            var toolIds = new List<int>();
            await _profileToolsDb.DeleteAllProfileAssociationsAsync(existingProfile.Id);
            if (profileDto.Tools != null && profileDto.Tools.Count > 0)
            {
                await CreateOrUpdateTools(profileDto.Tools);
                foreach (var tool in profileDto.Tools)
                {
                    var dbTool = await _toolDb.GetByNameAsync(tool.Function.Name);
                    toolIds.Add(dbTool.Id);
                }
                await _profileToolsDb.AddAssociationsByProfileIdAsync(existingProfile.Id, toolIds);
            }
            return true;
        }

        public async Task<Tool> GetTool(string name)
        {
            var dbTool = await _toolDb.GetByNameAsync(name);
            return DbMappingHandler.MapFromDbTool(dbTool);
        }

        public async Task<IEnumerable<Tool>> GetAllTools()
        {
            var returnList = new List<Tool>();
            var dbTools = await _toolDb.GetAllAsync();
            foreach (var dbTool in dbTools)
            {
                var properties = await _propertyDb.GetToolProperties(dbTool.Id);
                var tool = DbMappingHandler.MapFromDbTool(dbTool, properties.ToList());
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
                if (errorMessage != null) return errorMessage;
            }
            foreach (var tool in toolList)
            {
                var dbToolDTO = DbMappingHandler.MapToDbTool(tool);
                var existingDbTool = await _toolDb.GetByNameAsync(tool.Function.Name);
                var existingToolDTO = DbMappingHandler.MapFromDbTool(existingDbTool);
                if (existingToolDTO != null)
                {
                    var existingTool = DbMappingHandler.MapToDbTool(existingToolDTO);
                    await _toolDb.UpdateAsync(existingTool, dbToolDTO);
                    await AddOrUpdateToolProperties(existingToolDTO, tool.Function.Parameters.properties);
                }
                else
                {
                    var newDbTool = await _toolDb.AddAsync(dbToolDTO);
                    var newTool = DbMappingHandler.MapFromDbTool(newDbTool);
                    await AddOrUpdateToolProperties(newTool, tool.Function.Parameters.properties);
                }
            }
            return null;
        }

        public async Task<string> AddToolToProfiles(string name, List<string> profiles)
        {
            var tool = await _toolDb.GetByNameAsync(name);
            if (tool != null)
            {
                var success = await _profileToolsDb.AddAssociationsByToolIdAsync(tool.Id, profiles);
                if (success) return null;
                else return _unknownErroMessage;
            }
            return _missingToolMessage + $"'{name}'";
        }

        public async Task<string> AddProfileToTools(string name, List<string> tools)
        {
            var toolIDs = new List<int>();
            foreach (var toolName in tools)
            {
                var tool = await _toolDb.GetByNameAsync(toolName);
                if (tool is null) return _missingToolMessage + $"'{toolName}'"; ;
                toolIDs.Add(tool.Id);
            }
            var profile = await _profileDb.GetByNameAsync(name);
            if (profile != null && toolIDs.Count > 0)
            {
                var success = await _profileToolsDb.AddAssociationsByProfileIdAsync(profile.Id, toolIDs);
                if (success) return null;
            }
            return _missingProfileMessage + $"'{profile.Name}'";
        }

        public async Task<string> DeleteToolAssociations(string name, List<string> profiles)
        {
            var tool = await _toolDb.GetByNameAsync(name);
            if (tool != null)
            {
                foreach (var profile in profiles) await _profileToolsDb.DeleteToolAssociationAsync(tool.Id, profile);
                return null;
            }
            return _missingToolMessage + $"'{name}'";
        }

        public async Task<string> DeleteProfileAssociations(string name, List<string> tools)
        {
            var profile = await _profileDb.GetByNameAsync(name);
            if (profile != null)
            {
                foreach (var tool in tools) await _profileToolsDb.DeleteProfileAssociationAsync(profile.Id, tool);
                return null;
            }
            return _missingToolMessage + $"'{name}'";
        }

        public async Task<bool> DeleteTool(string name)
        {
            var existingDbTool = await _toolDb.GetByNameAsync(name);
            var dbProperites = await _propertyDb.GetToolProperties(existingDbTool.Id);
            var existingTool = DbMappingHandler.MapFromDbTool(existingDbTool, dbProperites.ToList());
            if (existingTool != null)
            {
                foreach (var property in existingTool.Function.Parameters.properties)
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
