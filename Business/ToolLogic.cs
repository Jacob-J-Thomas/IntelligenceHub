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
//    public class ToolLogic
//    {
//        private readonly ToolRepository _toolDb;
//        private readonly ProfileToolsRepository _profileToolsDb;
//        private readonly PropertyRepository _propertyDb;

//        private readonly ToolLogic _toolLogic;
//        private readonly ValidationLogic _validationLogic;
//        public ToolLogic(string connectionString) 
//        {
//            _toolDb = new ToolRepository(connectionString);
//            _propertyDb = new PropertyRepository(connectionString);
//            _profileToolsDb = new ProfileToolsRepository(connectionString);

//            _toolLogic = new ToolLogic(connectionString);   
//            _validationLogic = new ValidationLogic();
//        }

//        // probably move these tool methods to another class (leave above because it is only used here)
//        public async Task<Tool> GetTool(string name)
//        {
//            return await _toolDb.GetToolByNameAsync(name);
//        }

//        public async Task<Tool> GetToolById(int id)
//        {
//            return await _toolDb.GetToolByIdAsync(id);
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
//            foreach (var tool in toolList)
//            {
//                var errorMessage = _validationLogic.ValidateTool(tool);
//                if (errorMessage != null)
//                {
//                    return errorMessage;
//                }
//            }
//            foreach (var tool in toolList)
//            {
//                var dbToolDTO = new DbToolDTO(tool);
//                var existingToolDTO = await _toolDb.GetToolByNameAsync(tool.Function.Name);
//                if (existingToolDTO != null)
//                {
//                    var existingTool = new DbToolDTO(existingToolDTO);
//                    await _toolDb.UpdateAsync(existingTool, dbToolDTO);
//                    await AddOrUpdateToolProperties(existingToolDTO, tool.Function.Parameters.Properties);
//                }
//                else
//                {
//                    var newTool = await _toolDb.AddAsync(dbToolDTO);
//                    var newToolDTO = new Tool(newTool);
//                    await AddOrUpdateToolProperties(newToolDTO, tool.Function.Parameters.Properties);
//                }
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
//                    return "something went wrong...";
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
//                    await _profileToolsDb.DeleteAssociationAsync(tool.Id, profile);
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
//                    await _propertyDb.DeleteAsync(dto);
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
//        public async Task<bool> AddOrUpdateToolProperties(Tool existingTool, Dictionary<string, PropertyDTO> newProperties)
//        {
//            var existingProperties = await _propertyDb.GetToolProperties(existingTool.Id);
//            foreach (var property in existingProperties)
//            {
//                await _propertyDb.DeleteAsync(property);
//            }

//            foreach (var property in newProperties)
//            {
//                await _propertyDb.AddAsync(new DbPropertyDTO(existingTool.Id, property.Key, property.Value));
//            }


//            //if (existingProps == null || existingProps.Count < 1)
//            //{
//            //    foreach (var property in newProperties)
//            //    {
//            //        await _propertyDb.AddAsync(new DbPropertyDTO(toolId, property.Key, property.Value));
//            //    }
//            //}
//            //else
//            //{
//            //    foreach (var property in existingProps)
//            //    {
//            //        if (!newProperties.ContainsKey(property.Key))
//            //        {
//            //            var varcheck = new DbPropertyDTO(property.Key, property.Value);
//            //            varcheck.ToolId = toolId;
//            //            await _propertyDb.DeleteAsync(varcheck);
//            //        }
//            //    }

//            //    foreach (var property in newProperties)
//            //    {
//            //        if (existingProps.ContainsKey(property.Key))
//            //        {
//            //            var existingDbProp = new DbPropertyDTO(property.Key, existingProps[property.Key]);
//            //            var newDbProp = new DbPropertyDTO(property.Key, property.Value);
//            //            existingDbProp.ToolId = toolId;
//            //            await _propertyDb.UpdatePropertyAsync(existingDbProp, newDbProp);
//            //        }
//            //        else
//            //        {
//            //            var varcheck = new DbPropertyDTO(toolId, property.Key, property.Value);
//            //            await _propertyDb.AddAsync(varcheck);
//            //        }
//            //    }
//            //}
//            return true;
//        }
//    }
//}
