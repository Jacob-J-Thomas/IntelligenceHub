using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using OpenAICustomFunctionCallingAPI.API.DTOs;
using OpenAICustomFunctionCallingAPI.Client.DTOs;
using OpenAICustomFunctionCallingAPI.Common;
using OpenAICustomFunctionCallingAPI.Common.Attributes;
using OpenAICustomFunctionCallingAPI.Controllers.DTOs;
using OpenAICustomFunctionCallingAPI.DAL;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenAICustomFunctionCallingAPI.DAL.DTOs
{
    // extend this from a common DTO?
    [TableName("Profiles")]
    public class DbProfileDTO : CompletionBaseDTO
    {
        // Don't think below ?'s are necessary
        public string Name { get; set; }
        public bool? Logprobs { get; set; }
        //public List<Tool>? Tools { get; set; } // check the size and validation
        public string? Reference_Profiles { get; set; }
        public string? Stop_Sequences { get; set; }
        //public string? System_Message { get; set; }

        public DbProfileDTO() { }

        // Improve this somehow
        public DbProfileDTO(bool createNew, APIProfileDTO apiDto)
        {
            Id = apiDto.Id;
            Name = apiDto.Name;
            N = apiDto.N;
            Response_Format = apiDto.Response_Format;
            Seed = apiDto.Seed;
            User = apiDto.User;
            System_Message = apiDto.System_Message; // add a default sys message and move into below if statement

            if (createNew)
            {
                Model = apiDto.Model ?? "gpt-3.5-turbo";
                Frequency_Penalty = apiDto.Frequency_Penalty ?? 0;
                Presence_Penalty = apiDto.Presence_Penalty ?? 0;
                Max_Tokens = apiDto.Max_Tokens ?? 1200;
                Temperature = apiDto.Temperature ?? 1;
                Top_P = apiDto.Top_P ?? 1;
                Stream = apiDto.Stream ?? false; // deafult to false for now
                Top_Logprobs = apiDto.Top_Logprobs;
                if (Top_Logprobs != null)
                {
                    Logprobs = true;
                }
                else
                {
                    Logprobs = false;
                }
            }
            else
            {
                Model = apiDto.Model;
                Frequency_Penalty = apiDto.Frequency_Penalty;
                Presence_Penalty = apiDto.Presence_Penalty;
                Temperature = apiDto.Temperature;
                Top_P = apiDto.Top_P;
                Stream = apiDto.Stream;
                Top_Logprobs = apiDto.Top_Logprobs;
                if (apiDto.Top_Logprobs != null)
                {
                    Logprobs = true;
                }
                else
                {
                    Logprobs = false;
                }

                // Convert data
                if (apiDto.Stop != null)
                {
                    Stop_Sequences = string.Join(", ", apiDto.Stop);
                }
                if (apiDto.Reference_Profiles != null)
                {
                    Reference_Profiles = string.Join(", ", apiDto.Reference_Profiles);
                }
            }
        }
        public DbProfileDTO(APIProfileDTO existingDto, APIProfileDTO updateDto)
        {
            if (existingDto.Id != null)
            {
                Id = existingDto.Id;
            }
            
            Name = updateDto.Name ?? existingDto.Name;
            N = updateDto.N ?? existingDto.N;
            Response_Format = updateDto.Response_Format ?? existingDto.Response_Format;
            Seed = updateDto.Seed ?? existingDto.Seed;
            User = updateDto.User ?? existingDto.User;
            System_Message = updateDto.System_Message ?? existingDto.System_Message;
            Model = updateDto.Model ?? existingDto.Model;
            Frequency_Penalty = updateDto.Frequency_Penalty ?? existingDto.Frequency_Penalty;
            Presence_Penalty = updateDto.Presence_Penalty ?? existingDto.Presence_Penalty;
            Temperature = updateDto.Temperature ?? existingDto.Temperature;
            Top_P = updateDto.Top_P ?? existingDto.Top_P;
            Stream = updateDto.Stream ?? existingDto.Stream;
            Top_Logprobs = updateDto.Top_Logprobs ?? existingDto.Top_Logprobs;
            Stream = updateDto.Stream ?? existingDto.Stream;
            Max_Tokens = updateDto.Max_Tokens ?? existingDto.Max_Tokens;
            Tool_Choice = updateDto.Tool_Choice ?? existingDto.Tool_Choice;
            System_Message = updateDto.System_Message ?? existingDto.System_Message;
            Messages = updateDto.Messages ?? existingDto.Messages;
            Tools = updateDto.Tools ?? existingDto.Tools;

            // this probably only needs to be done in the OpenAI Completion DTO
            if (Top_Logprobs != null) 
            {
                // Logprobs is always required if using Top_Logprob
                Logprobs = true;
            }
            else
            {
                Logprobs = false;
            }

            // Convert data
            if (updateDto.Stop != null)
            {
                Stop_Sequences = string.Join(", ", updateDto.Stop);
            }
            else if (existingDto.Stop != null)
            {
                Stop_Sequences = string.Join(", ", existingDto.Stop);
            }

            if (updateDto.Reference_Profiles != null)
            {
                Reference_Profiles = string.Join(", ", updateDto.Reference_Profiles);
            }
            else if (existingDto.Reference_Profiles != null)
            {
                Reference_Profiles = string.Join(", ", existingDto.Reference_Profiles);
            }
        }
    }
}
