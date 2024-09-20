using IntelligenceHub.API.DTOs.ClientDTOs.MessageDTOs;
using IntelligenceHub.API.DTOs.ClientDTOs.ToolDTOs;
using IntelligenceHub.API.MigratedDTOs;
using IntelligenceHub.API.MigratedDTOs.ToolDTOs;
using IntelligenceHub.Common.Extensions;
using IntelligenceHub.DAL.DTOs;
using static IntelligenceHub.Common.GlobalVariables;
using System.Reflection;

namespace IntelligenceHub.DAL
{
    public static class DbMappingHandler
    {
        #region Profiles
        public static Profile MapFromDbProfile(DbProfile dbProfile)
        {
            throw new NotImplementedException();
        }

        public static DbProfile MapToDbProfile(DbProfile? existingProfile = null, Profile? profileUpdate = null)
        {
            return new DbProfile()
            {
                // update or set existing value
                Id = existingProfile.Id,
                Name = profileUpdate.Name ?? existingProfile.Name,
                Response_Format = profileUpdate.Response_Format ?? existingProfile.Response_Format,
                Seed = profileUpdate.Seed ?? existingProfile.Seed,
                User = profileUpdate.User ?? existingProfile.User,
                System_Message = profileUpdate.System_Message ?? existingProfile.System_Message,
                Top_Logprobs = profileUpdate.Top_Logprobs ?? existingProfile.Top_Logprobs,
                Tool_Choice = profileUpdate.Tool_Choice ?? existingProfile.Tool_Choice,

                // Variables with default values during first database entry
                Model = profileUpdate.Model
                    ?? existingProfile.Model
                    ?? "gpt-3.5-turbo",

                Frequency_Penalty = profileUpdate.Frequency_Penalty
                    ?? existingProfile.Frequency_Penalty
                    ?? 0,

                Presence_Penalty = profileUpdate.Presence_Penalty
                    ?? existingProfile.Presence_Penalty
                    ?? 0,

                Temperature = profileUpdate.Temperature
                    ?? existingProfile.Temperature
                    ?? 1,

                Top_P = profileUpdate.Top_P
                    ?? existingProfile.Top_P
                    ?? 1,

                Max_Tokens = profileUpdate.Max_Tokens
                    ?? existingProfile.Max_Tokens
                    ?? 1200,

                Return_Recursion = profileUpdate.Return_Recursion
                    ?? existingProfile.Return_Recursion
                    ?? false,
                Stop = profileUpdate.Stop?.ToCommaSeparatedString() ?? existingProfile.Stop,
                Reference_Profiles = profileUpdate.Reference_Profiles?.ToCommaSeparatedString() ?? existingProfile.Reference_Profiles,
            };
        }
        #endregion

        #region Tools
        public static Tool MapFromDbTool(DbTool dbTool, List<DbProperty>? dbProperties = null)
        {
            var tool = new Tool()
            {
                Id = dbTool.Id,
                Function = new Function()
                {
                    Name = dbTool.Name,
                    Description = dbTool.Description
                }
            };

            foreach (var property in dbProperties) 
            {
                var convertedProp = new Property()
                {
                    Id = property.Id,
                    Type = property.Type,
                    Description = property.Description,
                };
                tool.Function.Parameters.Properties.Add(property.Name, convertedProp);
            }
            return tool;
        }

        public static DbTool MapToDbTool(Tool tool, List<Property>? dbProperties = null)
        {
            return new DbTool()
            {
                Id = tool.Id,
                Name = tool.Function.Name,
                Description = tool.Function.Description ?? string.Empty,
                Required = tool.Function.Parameters.Required?.ToCommaSeparatedString() ?? string.Empty,
            };
        }
        #endregion

        #region Messaging
        public static Message MapFromDbMessage(DbMessage dbMessage)
        {
            return new Message()
            {
                Content = dbMessage.Content,
                Role = dbMessage.Role,
            };
        }

        public static DbMessage MapToDbMessage(Message message, Guid? conversationId = null, string[]? toolsCalled = null)
        {
            return new DbMessage()
            {
                Content = message.Content,
                Role = message.Role,
                ConversationId = conversationId,
                ToolsCalled = toolsCalled?.ToCommaSeparatedString() ?? string.Empty,
                TimeStamp = DateTime.UtcNow,
            };
        }
        #endregion
    }
}
