//using OpenAICustomFunctionCallingAPI.Client;
//using OpenAICustomFunctionCallingAPI.Controllers.DTOs;
//using Newtonsoft.Json.Linq;
//using OpenAICustomFunctionCallingAPI.Host.Config;
////using OpenAICustomFunctionCallingAPI.DAL;
//using Azure;
//using Microsoft.Data.SqlClient;
//using OpenAICustomFunctionCallingAPI.DAL;
//using System.Web.Mvc;
//using System.Net;
//using System.Net.Mail;
//using Microsoft.AspNetCore.Mvc;
//using Nest;
//using OpenAICustomFunctionCallingAPI.DAL.DTOs;
//using OpenAICustomFunctionCallingAPI.API.DTOs;
//using System.Reflection.Metadata;

//namespace OpenAICustomFunctionCallingAPI.Business
//{
//    // this whole class needs some refactoring
//    public class DbLogic
//    {
//        private readonly ProfileRepository _profileDb;
//        private readonly ToolRepository _toolDb;
//        private readonly PropertyRepository _propertyDb;
//        private readonly ProfileToolsRepository _profileToolsDb;
//        //private readonly PropertyRepository _propertyDb;
//        public DbLogic(string connectionString) 
//        {
//            _profileDb = new ProfileRepository(connectionString);
//            _toolDb = new ToolRepository(connectionString);
//            _propertyDb = new PropertyRepository(connectionString);
//            _profileToolsDb = new ProfileToolsRepository(connectionString);
//        }

//        public async Task<APIProfileDTO> GetProfile(string name)
//        {
//            var dbProfile = await _profileDb.GetByNameWithToolsAsync(name);
//            if (dbProfile != null) 
//            {
//                var apiProfile = dbProfile;

//                //// can we move the below logic into the GetByName's sql query?
//                //var tools = await _toolDb.GetProfileTools(dbProfile.Id);
//                //apiProfile.Tools = tools.ToList();
//                return apiProfile;
//            }
//            return null;
//        }

//        // might need to return as pageable if db starts getting big
//        public async Task<IEnumerable<APIProfileDTO>> GetAllProfiles()
//        {
//            var response = await _profileDb.GetAllAsync();
//            var apiResponseList = new List<APIProfileDTO>();
//            if (response != null)
//            {
//                foreach (var dbProfile in response)
//                {
//                    var apiProfile = new APIProfileDTO(dbProfile);
//                    var profileToolDTOs = await _profileToolsDb.GetToolIdsAsync(apiProfile.Id);
//                    apiProfile.Tools = new List<Tool>();
//                    foreach (var association in profileToolDTOs)
//                    {
//                        var tool = await _toolDb.GetToolByIdAsync(association.ToolID);
//                        apiProfile.Tools.Add(tool);
//                    }
//                    apiResponseList.Add(apiProfile);
//                }
//            }
//            return apiResponseList;
//        }

//        public async Task<string> CreateOrUpdateProfile(APIProfileDTO profileDto)
//        {
//            var errorMessage = ValidateProfile(profileDto);
//            if (errorMessage != null)
//            {
//                return errorMessage;
//            }
//            var existingProfile = await _profileDb.GetByNameWithToolsAsync(profileDto.Name);
//            if (existingProfile != null)
//            {
//                var existingProfileDbDTO = new DbProfileDTO(createNew: false, existingProfile);
//                var profileDbDTO = new DbProfileDTO(createNew: false, profileDto);
//                await _profileDb.UpdateAsync(existingProfileDbDTO, profileDbDTO);
//                var success = await AddOrUpdateProfileTools(profileDto, existingProfile);
//                if (!success)
//                {
//                    return "Failed to add or update the tools in the database";
//                }
//            }
//            else
//            {
//                var toolLessProfile = await _profileDb.GetByNameAsync(profileDto.Name);
//                if (toolLessProfile != null)
//                {
//                    var toolLessDbProfileDTO = new APIProfileDTO(toolLessProfile);
//                    var profileDbDTO = new DbProfileDTO(createNew: false, profileDto);
//                    await _profileDb.UpdateAsync(toolLessProfile, profileDbDTO);
//                    var success = await AddOrUpdateProfileTools(profileDto, toolLessDbProfileDTO);
//                    if (!success)
//                    {
//                        return "Failed to add or update the tools in the database";
//                    }
//                }
//                else
//                {
//                    var profileDbDTO = new DbProfileDTO(createNew: false, profileDto);
//                    var newProfile = await _profileDb.AddAsync(profileDbDTO);
//                    var newProfileDTO = new APIProfileDTO(newProfile);
//                    var success = await AddOrUpdateProfileTools(profileDto, newProfileDTO);
//                    if (!success)
//                    {
//                        return "Failed to add or update the tools in the database";
//                    }
//                }
//            }
//            return null;
//        }

//        public async Task<string> DeleteProfile(string name)
//        {
//            var profileDto = await _profileDb.GetByNameWithToolsAsync(name);
//            if (profileDto != null)
//            {
//                var rows = await _profileDb.DeleteAsync(new DbProfileDTO(createNew: false, profileDto));
//                if (profileDto.Tools != null)
//                {
//                    await _profileToolsDb.DeleteAllProfileAssociationsAsync(profileDto.Id);
//                }
//                return null;
//            }
//            else
//            {
//                return $"No profile with the name '{name}' was found.";
//            }
//        }

//        private async Task<bool> AddOrUpdateProfileTools(APIProfileDTO profileDto, APIProfileDTO existingProfile)
//        {
//            var currentToolId = existingProfile.Id;
//            var toolIds = new List<int>();
//            if (existingProfile != null)
//            {
//                 await _profileToolsDb.DeleteAllProfileAssociationsAsync(existingProfile.Id);
//            }
            
//            foreach (var dtoTool in profileDto.Tools)
//            {
//                if (existingProfile != null && existingProfile.Tools != null)
//                {
//                    var dbTool = existingProfile.Tools.FirstOrDefault(tool => tool.Function.Name == dtoTool.Function.Name);
                    
//                    await _toolDb.UpdateAsync(new DbToolDTO(dbTool), new DbToolDTO(dtoTool));
//                    toolIds.Add(dbTool.Id);
//                }
//                else
//                {
//                    var newTool = await _toolDb.AddAsync(new DbToolDTO(dtoTool));
//                    toolIds.Add(newTool.Id);
//                }
//                var existingTool = await _toolDb.GetToolByNameAsync(dtoTool.Function.Name);
//                await AddOrUpdateToolProperties(currentToolId, existingTool.Function.Parameters.Properties, dtoTool.Function.Parameters.Properties);
//                await _profileToolsDb.AddAssociationsByProfileIdAsync(currentToolId, toolIds);
//            }
//            return true;
//        }

//        // probably move these tool methods to another class (leave above because it is only used here)
//        public async Task<Tool> GetTool(string name)
//        {
//            return await _toolDb.GetToolByNameAsync(name);
//        }

//        public async Task<IEnumerable<Tool>> GetAllTools()
//        {
//            var returnList = new List<Tool>();
//            var dbTools = await _toolDb.GetAllAsync();
//            foreach (var tool in dbTools)
//            {
//                returnList.Add(new Tool(tool));
//            }
//            return returnList;
//        }

//        public async Task<List<string>> GetToolProfileAssociations(string name)
//        {
//            var profileNames = await _toolDb.GetToolProfilesAsync(name);
//            if (profileNames.Count > 0)
//            {
//                return profileNames;
//            }
//            return null;
//        }

//        public async Task<string> CreateOrUpdateTools(List<Tool> toolList)
//        {
//            // validate before processing any data - is there a better way to do this maybe though?
//            foreach (var tool in toolList)
//            {
//                var errorMessage = ValidateTool(tool);
//                if (errorMessage != null)
//                {
//                    return errorMessage;
//                }
//            }
//            foreach (var tool in toolList)
//            {
//                var toolDbDTO = new DbToolDTO(tool);
//                var existingTool = await _toolDb.GetToolByNameAsync(tool.Function.Name);
//                int toolId;
//                if (existingTool != null)
//                {
//                    await AddOrUpdateToolProperties(existingTool.Id, existingTool.Function.Parameters.Properties, tool.Function.Parameters.Properties);
//                    var existingToolDbDTO = new DbToolDTO(existingTool);
//                    await _toolDb.UpdateAsync(existingToolDbDTO, toolDbDTO);
//                }
//                else
//                {
//                    var newTool = await _toolDb.AddAsync(toolDbDTO);
//                    await AddOrUpdateToolProperties(newTool.Id, null, tool.Function.Parameters.Properties);
//                }
//                //existingTool.Function.Parameters.Properties.Add();
//                //tool.Function.Parameters.Properties.Add();
                
//            }
//            return null;
//        }

//        public async Task<string> AddToolAssociations(string name, List<string> profiles)
//        {
//            var tool = await _toolDb.GetByNameAsync(name);
//            if (tool != null)
//            {
//                var success = await _profileToolsDb.AddAssociationsByToolIdAsync(tool.Id, profiles);
//                if (success)
//                {
//                    return null;
//                }
//                else
//                {
//                    return "something went wrong..."; // improve this 
//                }
//            }
//            return $"No tool with the name {name} exists.";
//        }

//        public async Task<string> DeleteToolAssociations(string name, List<string> profiles)
//        {
//            var tool = await _toolDb.GetByNameAsync(name);
//            if (tool != null)
//            {
//                foreach (var profile in profiles)
//                {
//                    await _profileToolsDb.DeleteAssociationAsync(tool.Id, profile); // probably change this function to get by string
//                }
//                return null;
//            }
//            return $"No tool with the name {name} exists.";
//        }

//        public async Task<bool> DeleteTool(string name)
//        {
//            var existingTool = await _toolDb.GetToolByNameAsync(name);
//            if (existingTool != null)
//            {
//                foreach (var property in existingTool.Function.Parameters.Properties)
//                {
//                    var dto = new DbPropertyDTO(existingTool.Id, property.Key, property.Value);
//                    var ro = await _propertyDb.DeleteAsync(dto);
//                    var st = ro.ToString();
//                }
//                await _profileToolsDb.DeleteAllToolAssociationsAsync(existingTool.Id);
//                return await _toolDb.DeleteAsync(new DbToolDTO(existingTool)) == 1;
//            }
//            return false;
//        }

//        // probably keep the below property methods contained in the same document as the tool logic

//        // probably could clean up this method a little
//        //

//        //      Specifically, looks like we should create another method for when its a new tool
//        //      (which means no updates required)
//        public async Task<string> AddOrUpdateToolProperties(int toolId, Dictionary<string, PropertyDTO> existingProps, Dictionary<string, PropertyDTO> newProps)
//        {
//            if (existingProps == null || existingProps.Count < 1)
//            {
//                foreach (var property in newProps)
//                {
//                    await _propertyDb.AddAsync(new DbPropertyDTO(toolId, property.Key, property.Value));
//                }
//            }
//            else
//            {
//                foreach (var property in existingProps)
//                {
//                    if (!newProps.ContainsKey(property.Key))
//                    {
//                        var varcheck = new DbPropertyDTO(property.Key, property.Value);
//                        varcheck.ToolId = toolId;
//                        await _propertyDb.DeleteAsync(varcheck);
//                    }
//                }

//                foreach (var property in newProps)
//                {
//                    if (existingProps.ContainsKey(property.Key))
//                    {
//                        var existingDbProp = new DbPropertyDTO(property.Key, existingProps[property.Key]);
//                        var newDbProp = new DbPropertyDTO(property.Key, property.Value);
//                        existingDbProp.ToolId = toolId;
//                        await _propertyDb.UpdatePropertyAsync(existingDbProp, newDbProp);
//                    }
//                    else
//                    {
//                        var varcheck = new DbPropertyDTO(toolId, property.Key, property.Value);
//                        await _propertyDb.AddAsync(varcheck);
//                    }
//                }
//            }
//            return null;
//        }
            

            

//        // Might want to think about moving this to the controller level?
//        private string ValidateProfile(APIProfileDTO profile)
//        {
//            var validModels = new List<string>()
//            {
//                "babbage-002",
//                "davinci-002",
//                "gpt-3.5-turbo",
//                "gpt-3.5-turbo-16k",
//                "gpt-3.5-turbo-instruct",
//                "gpt-4",
//                "gpt-4-32k",
//                "gpt-4-turbo-preview",
//                "gpt-4-vision-preview",
//                "mixtral",
//                "cusotom" // need to implement this still
//            };

//            // ensure tool and profiles do not have overlapping names since there
//            // would be no way to tell them apart during recursive child/function calls
//            //      - Actually, might be able to avoid this by appending "-tool" and
//            //          "-model" when data is retrieved from the database

//            if (string.IsNullOrWhiteSpace(profile.Name) || profile.Name == null)
//            {
//                return "The 'Name' field is required";
//            }
//            if (profile.Name.ToLower() == "all")
//            {
//                return "Profile name 'all' conflicts with the get/all route";
//            }
//            if (profile.Model != null && validModels.Contains(profile.Model) == false)
//            {
//                return "The model name must match and existing AI model";
//            }
//            if (profile.Frequency_Penalty < -2.0 || profile.Frequency_Penalty > 2.0)
//            {
//                return "Frequency_Penalty must be a value between -2 and 2";
//            }
//            if (profile.Presence_Penalty < -2.0 || profile.Presence_Penalty > 2.0)
//            {
//                return "Presence_Penalty must be a value between -2 and 2";
//            }
//            if (profile.Temperature < 0 || profile.Temperature > 2)
//            {
//                return "Temperature must be a value between 0 and 2";
//            }
//            if (profile.Top_P < 0 || profile.Top_P > 1)
//            {
//                return "Top_P must be a value between 0 and 1";
//            }
//            if (profile.Max_Tokens < 0 || profile.Max_Tokens > 1000000)
//            {
//                return "Max_Tokens must be a value between 0 and 1,000,000";
//            }
//            if (profile.N < 0 || profile.N > 100)
//            {
//                return "N must be a value between 0 and 100";
//            }
//            if (profile.Top_Logprobs < 0 || profile.Top_Logprobs > 5)
//            {
//                return "Top_Logprobs must be a value between 0 and 5";
//            }
//            if (profile.Top_Logprobs != null && profile.Model == "gpt-4-vision-preview")
//            {
//                return "Top_Logprobs cannot be used with gpt-4-vision-preview";
//            }
//            if (profile.Response_Type != null && (profile.Response_Type != "text" && profile.Response_Type != "json_object"))
//            {
//                return "If Response_Type is set, it must either be equal to 'text' or 'json_object'";
//            }

//            foreach (var tool in profile.Tools)
//            {
//                var errorMessage = ValidateTool(tool);
//                if (errorMessage != null)
//                {
//                    return errorMessage;
//                }
//            }
//            return null;
//        }

//        private string ValidateTool(Tool tool)
//        {
//            if (tool.Function.Name == null || string.IsNullOrEmpty(tool.Function.Name))
//            {
//                return "A function name is required for all tools.";
//            }
//            if (tool.Function.Parameters.Required != null && tool.Function.Parameters.Required.Length > 0)
//            {
//                foreach (var str in  tool.Function.Parameters.Required)
//                {
//                    if (!tool.Function.Parameters.Properties.ContainsKey(str))
//                    {
//                        return $"Required property {str} does not exist in the tool {tool.Function.Name}'s properties list.";
//                    }
//                }
//            }

//            if (tool.Function.Parameters.Properties != null && tool.Function.Parameters.Properties.Count > 0)
//            {
//                var errorMessage = ValidateProperties(tool.Function.Parameters.Properties);
//                if (errorMessage != null)
//                {
//                    return errorMessage;
//                }
//            }
//            return null;
//        }

//        private string ValidateProperties(Dictionary<string, PropertyDTO> properties)
//        {
//            var validTypes = new List<string>()
//            {
//                // verify these
//                "char",
//                "string",
//                "bool",
//                "int",
//                "double",
//                "float",
//                "date",
//                "enum",

//                // Don't think these would work currently, but can test:
//                //"array",
//                //"object"
//            };
//            foreach (var prop in properties)
//            {
//                if (prop.Value.Type == null)
//                {
//                    return $"The field 'type' for property {prop.Key} is required";
//                }
//                else if (!validTypes.Contains(prop.Value.Type))
//                {
//                    return $"The 'type' field '{prop.Value.Type}' for property {prop.Key} is invalid. Please ensure one of the following types is selected: '{validTypes}'";
//                }
//            }
//            return null;
//        }
//    }
//}
