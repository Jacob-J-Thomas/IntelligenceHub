using IntelligenceHub.API.DTOs.ClientDTOs.MessageDTOs;
using IntelligenceHub.API.MigratedDTOs;
using IntelligenceHub.API.MigratedDTOs.ToolDTOs;
using IntelligenceHub.Common.Extensions;
using IntelligenceHub.DAL.DTOs;
using static IntelligenceHub.Common.GlobalVariables;
using System.Reflection;
using IntelligenceHub.Common;

namespace IntelligenceHub.DAL
{
    public static class DbMappingHandler
    {
        #region Profiles
        public static Profile MapFromDbProfile(DbProfile dbProfile, List<Tool>? tools = null)
        {
            var profile = new Profile()
            {
                Id = dbProfile.Id,
                Name = dbProfile.Name,
                Model = dbProfile.Model,
                Frequency_Penalty = dbProfile.FrequencyPenalty,
                Presence_Penalty = dbProfile.PresencePenalty,
                Temperature = dbProfile.Temperature,
                Top_P = dbProfile.TopP,
                Max_Tokens = dbProfile.MaxTokens,
                Top_Logprobs = dbProfile.TopLogprobs,
                Response_Format = dbProfile.ResponseFormat,
                Seed = dbProfile.Seed,
                User = dbProfile.User,
                System_Message = dbProfile.SystemMessage,
                Stop = dbProfile.Stop.ToStringArray(),
                Reference_Profiles = dbProfile.ReferenceProfiles.ToStringArray(),
                Tool_Choice = dbProfile.ToolChoice,
                Tools = tools,
                MaxMessageHistory = dbProfile.MaxMessageHistory,
                Return_Recursion = dbProfile.ReturnRecursion,
            };
            profile.Logprobs = profile.Top_Logprobs > 0 ? true : false;
            return profile;
        }

        public static DbProfile MapToDbProfile(DbProfile? existingProfile = null, Profile? profileUpdate = null)
        {
            return new DbProfile()
            {
                // update or set existing value
                Id = existingProfile.Id,
                Name = profileUpdate.Name ?? existingProfile.Name,
                ResponseFormat = profileUpdate.Response_Format ?? existingProfile.ResponseFormat,
                Seed = profileUpdate.Seed ?? existingProfile.Seed,
                User = profileUpdate.User ?? existingProfile.User,
                SystemMessage = profileUpdate.System_Message ?? existingProfile.SystemMessage,
                TopLogprobs = profileUpdate.Top_Logprobs ?? existingProfile.TopLogprobs,
                ToolChoice = profileUpdate.Tool_Choice ?? existingProfile.ToolChoice,

                // Variables with default values during first database entry
                Model = profileUpdate.Model
                    ?? existingProfile.Model
                    ?? "gpt-3.5-turbo",

                FrequencyPenalty = profileUpdate.Frequency_Penalty
                    ?? existingProfile.FrequencyPenalty
                    ?? 0,

                PresencePenalty = profileUpdate.Presence_Penalty
                    ?? existingProfile.PresencePenalty
                    ?? 0,

                Temperature = profileUpdate.Temperature
                    ?? existingProfile.Temperature
                    ?? 1,

                TopP = profileUpdate.Top_P
                    ?? existingProfile.TopP
                    ?? 1,

                MaxTokens = profileUpdate.Max_Tokens
                    ?? existingProfile.MaxTokens
                    ?? 1200,

                ReturnRecursion = profileUpdate.Return_Recursion
                    ?? existingProfile.ReturnRecursion
                    ?? false,
                Stop = profileUpdate.Stop?.ToCommaSeparatedString() ?? existingProfile.Stop,
                ReferenceProfiles = profileUpdate.Reference_Profiles?.ToCommaSeparatedString() ?? existingProfile.ReferenceProfiles,
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
                tool.Function.Parameters.Required = dbTool.Required.ToStringArray();
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

        public static DbProperty MapToDbProperty(string name, Property property)
        {
            return new DbProperty()
            {
                Id = property.Id ?? 0,
                ToolId = property.Id ?? 0,
                Name = name,
                Type = property.Type,
                Description = property.Description ?? string.Empty,
            };
        }
        #endregion

        #region Messaging
        public static Message MapFromDbMessage(DbMessage dbMessage)
        {
            return new Message()
            {
                Content = dbMessage.Content,
                Role = GlobalVariables.ConvertStringToRole(dbMessage.Role),
            };
        }

        public static DbMessage MapToDbMessage(Message message, Guid? conversationId = null, string[]? toolsCalled = null)
        {
            return new DbMessage()
            {
                Content = message.Content,
                Role = message.Role.ToString(),
                ConversationId = conversationId,
                ToolsCalled = toolsCalled?.ToCommaSeparatedString() ?? string.Empty,
                TimeStamp = DateTime.UtcNow,
            };
        }
        #endregion
    }
}
