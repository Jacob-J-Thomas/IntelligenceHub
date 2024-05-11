using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.AICompletionDTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.ToolDTOs;
using OpenAICustomFunctionCallingAPI.Common.Extensions;
using OpenAICustomFunctionCallingAPI.DAL.DTOs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenAICustomFunctionCallingAPI.Controllers.DTOs
{
    // extend this from a common DTO?
    public class APIProfileDTO : BaseCompletionDTO
    {
        [JsonIgnore]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string[]? Reference_Profiles { get; set; }

        public APIProfileDTO() { }

        public APIProfileDTO(DbProfileDTO dbDto) : base()
        {
            ConvertToAPIProfileDTO(dbDto);
        }

        public void ConvertToAPIProfileDTO(DbProfileDTO dbDto)
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
            Top_Logprobs = dbDto.Top_Logprobs;
            Response_Format = dbDto.Response_Format;
            Seed = dbDto.Seed;
            User = dbDto.User;
            System_Message = dbDto.System_Message;

            if (dbDto.Stop != null && dbDto.Stop.Length > 0)
            {
                Stop = dbDto.Stop.ToStringArray();
            }
            if (dbDto.Reference_Profiles != null && dbDto.Reference_Profiles.Length > 0)
            {
                Reference_Profiles = dbDto.Reference_Profiles.ToStringArray();
            }
        }
    }
}
