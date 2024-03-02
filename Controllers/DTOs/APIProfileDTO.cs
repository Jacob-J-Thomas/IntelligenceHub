using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using OpenAICustomFunctionCallingAPI.API.DTOs;
using OpenAICustomFunctionCallingAPI.Common.Extensions;
using OpenAICustomFunctionCallingAPI.DAL.DTOs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenAICustomFunctionCallingAPI.Controllers.DTOs
{
    // extend this from a common DTO?
    public class APIProfileDTO : CompletionBaseDTO
    {
        [Required]
        public string Name { get; set; }
        [JsonIgnore] // stop ignoring this
        public string[]? Reference_Profiles { get; set; }
        //public List<Tool>? Tools { get; set; }

        [JsonConstructor]
        public APIProfileDTO() { }

        public APIProfileDTO(CompletionBaseDTO completionDTO)
        {
            Id = completionDTO.Id;
            Model = completionDTO.Model;
            Frequency_Penalty = completionDTO.Frequency_Penalty;
            Presence_Penalty = completionDTO.Presence_Penalty;
            Temperature = completionDTO.Temperature;
            Top_P = completionDTO.Top_P;
            Stream = completionDTO.Stream;
            Max_Tokens = completionDTO.Max_Tokens;
            N = completionDTO.N;
            Top_Logprobs = completionDTO.Top_Logprobs;
            Seed = completionDTO.Seed;
            User = completionDTO.User;
            Tool_Choice = completionDTO.Tool_Choice;
            Response_Format = completionDTO.Response_Format;
            System_Message = completionDTO.System_Message;
            Stop = completionDTO.Stop;
            Tools = completionDTO.Tools;
            Messages = completionDTO.Messages;
        }

        // Used to make Completions
        public APIProfileDTO(DbProfileDTO dbDto)
        {
            Id = dbDto.Id;
            Name = dbDto.Name;
            Model = dbDto.Model;
            Frequency_Penalty = dbDto.Frequency_Penalty;
            Presence_Penalty = dbDto.Presence_Penalty;
            Temperature = dbDto.Temperature;
            Top_P = dbDto.Top_P;
            Stream = dbDto.Stream;
            Max_Tokens = dbDto.Max_Tokens;
            N = dbDto.N;
            //Logprobs = dbDto.Logprobs;
            Top_Logprobs = dbDto.Top_Logprobs;
            Response_Format = dbDto.Response_Format;
            Seed = dbDto.Seed;
            User = dbDto.User;
            System_Message = dbDto.System_Message;
            if (dbDto.Stop != null && dbDto.Stop.Length > 0)
            {
                Stop = dbDto.Stop_Sequences.ToStringArray();
            }
            if (dbDto.Reference_Profiles != null && dbDto.Reference_Profiles.Length > 0)
            {
                Reference_Profiles = dbDto.Reference_Profiles.ToStringArray();
            }
        }

        public APIProfileDTO(DbProfileDTO dbDto, List<Tool> tools)
        {
            Name = dbDto.Name;
            Model = dbDto.Model;
            Frequency_Penalty = dbDto.Frequency_Penalty;
            Presence_Penalty = dbDto.Presence_Penalty;
            Temperature = dbDto.Temperature;
            Top_P = dbDto.Top_P;
            Stream = dbDto.Stream;
            Max_Tokens = dbDto.Max_Tokens;
            N = dbDto.N;
            //Logprobs = dbDto.Logprobs;
            Top_Logprobs = dbDto.Top_Logprobs;
            Response_Format = dbDto.Response_Format;
            Seed = dbDto.Seed;
            User = dbDto.User;
            System_Message = dbDto.System_Message;
            Tools = tools;
            if (dbDto.Stop != null && dbDto.Stop.Length > 0)
            {
                Stop = dbDto.Stop_Sequences.ToStringArray();
            }
            if (dbDto.Reference_Profiles != null && dbDto.Reference_Profiles.Length > 0)
            {
                Reference_Profiles = dbDto.Reference_Profiles.ToStringArray();
            }
        }
    }
}
