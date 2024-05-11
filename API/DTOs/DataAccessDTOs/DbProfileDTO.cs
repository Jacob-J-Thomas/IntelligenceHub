using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using OpenAICustomFunctionCallingAPI.API.DTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.ToolDTOs;
using OpenAICustomFunctionCallingAPI.Common;
using OpenAICustomFunctionCallingAPI.Common.Attributes; 
using OpenAICustomFunctionCallingAPI.Common.Extensions;
using OpenAICustomFunctionCallingAPI.Controllers.DTOs;
using OpenAICustomFunctionCallingAPI.DAL;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenAICustomFunctionCallingAPI.DAL.DTOs
{
    // extend this from a common DTO?
    [TableName("Profiles")]
    public class DbProfileDTO
    {
        // Don't think below ?'s are necessary
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Model { get; set; }
        public float? Frequency_Penalty { get; set; }
        public float? Presence_Penalty { get; set; }
        public float? Temperature { get; set; }
        public float? Top_P { get; set; }
        public int? Top_Logprobs { get; set; }
        public int? Max_Tokens { get; set; }
        public int? N { get; set; }
        public int? Seed { get; set; }
        public bool? Stream {  get; set; }
        public string Tool_Choice { get; set; }
        public string Response_Format { get; set; }
        public string User { get; set; }
        public string System_Message { get; set; }
        public string Stop { get; set; } // need to rename in database
        public string Reference_Profiles { get; set; }
        public string Reference_Description { get; set; }
        public bool? Return_Recursion { get; set; }

        public DbProfileDTO() { }

        public DbProfileDTO(DbProfileDTO existingDto, APIProfileDTO updateDto)
        {
            if (updateDto == null)
            {
                updateDto = new APIProfileDTO();
            }
            if (existingDto == null)
            {
                existingDto = new DbProfileDTO();
            }

            ConvertToDbDTOAndSetToExistingOrDefaults(existingDto, updateDto);
        }

        public void ConvertToDbDTOAndSetToExistingOrDefaults(DbProfileDTO existingDto, APIProfileDTO updateDto)
        {
            // update or set existing value
            Id = existingDto.Id;
            Name = updateDto.Name ?? existingDto.Name;
            N = updateDto.N ?? existingDto.N;
            Response_Format = updateDto.Response_Format ?? existingDto.Response_Format;
            Seed = updateDto.Seed ?? existingDto.Seed;
            User = updateDto.User ?? existingDto.User;
            System_Message = updateDto.System_Message ?? existingDto.System_Message;
            Top_Logprobs = updateDto.Top_Logprobs ?? existingDto.Top_Logprobs;
            Tool_Choice = updateDto.Tool_Choice ?? existingDto.Tool_Choice;
            Stream = updateDto.Stream ?? existingDto.Stream;
            System_Message = updateDto.System_Message ?? existingDto.System_Message;

            // Variables with default values during first database entry
            Model = updateDto.Model
                ?? existingDto.Model
                ?? "gpt-3.5-turbo";

            Frequency_Penalty = updateDto.Frequency_Penalty
                ?? existingDto.Frequency_Penalty
                ?? 0;

            Presence_Penalty = updateDto.Presence_Penalty
                ?? existingDto.Presence_Penalty
                ?? 0;

            Temperature = updateDto.Temperature
                ?? existingDto.Temperature
                ?? 1;

            Top_P = updateDto.Top_P
                ?? existingDto.Top_P
                ?? 1;

            Stream = updateDto.Stream
                ?? existingDto.Stream
                ?? false;

            Max_Tokens = updateDto.Max_Tokens
                ?? existingDto.Max_Tokens
                ?? 1200;

            Return_Recursion = updateDto.Return_Recursion
                ?? existingDto.Return_Recursion
                ?? false;

            if (updateDto.Stop != null && updateDto.Stop.Length > 0)
            {
                Stop = updateDto.Stop.ToCommaSeparatedString() ?? existingDto.Stop;
            }
            if (updateDto.Reference_Profiles != null && updateDto.Reference_Profiles.Length > 0)
            {
                Reference_Profiles = updateDto.Reference_Profiles.ToCommaSeparatedString() ?? existingDto.Reference_Profiles;
            }
        }








        //public DbProfileDTO(APIProfileDTO profileDto)
        //{
        //    Id = profileDto.Id;
        //    Name = profileDto.Name;
        //    Model = profileDto.Model;
        //    Frequency_Penalty = profileDto.Frequency_Penalty;
        //    Presence_Penalty = profileDto.Presence_Penalty;
        //    Temperature = profileDto.Temperature;
        //    Top_P = profileDto.Top_P;
        //    Top_Logprobs = profileDto.Top_Logprobs;
        //    Max_Tokens = profileDto.Max_Tokens;
        //    N = profileDto.N;
        //    Seed = profileDto.Seed;
        //    Stream = profileDto.Stream;
        //    Tool_Choice = profileDto.Tool_Choice;
        //    Response_Format = profileDto.Response_Format;
        //    User = profileDto.User;
        //    System_Message = profileDto.System_Message;
        //    if (profileDto.Stop != null)
        //    {
        //        Stop = profileDto.Stop.ToCommaSeparatedString();
        //    }
        //    if (profileDto.Reference_Profiles != null)
        //    {
        //        Reference_Profiles = profileDto.Reference_Profiles.ToCommaSeparatedString();
        //    }
        //}

        //  Add updateDto values, or if null default to existing data. If both are
        //  null add default values


        // probably can combine both of below constructors using third layer of ??















        //public DbProfileDTO(bool setDefaults, APIProfileDTO apiDto)
        //{
        //    Id = apiDto.Id;
        //    Name = apiDto.Name;
        //    N = apiDto.N;
        //    Response_Format = apiDto.Response_Format;
        //    Seed = apiDto.Seed;
        //    User = apiDto.User;
        //    Top_Logprobs = apiDto.Top_Logprobs;
        //    Tool_Choice = apiDto.Tool_Choice;
        //    Response_Format = apiDto.Response_Format;
        //    User = apiDto.User;
            
        //    if (setDefaults)
        //    {
        //        Model = apiDto.Model ?? "gpt-3.5-turbo";
        //        Frequency_Penalty = apiDto.Frequency_Penalty ?? 0;
        //        Presence_Penalty = apiDto.Presence_Penalty ?? 0;
        //        Max_Tokens = apiDto.Max_Tokens ?? 1200;
        //        Temperature = apiDto.Temperature ?? 1;
        //        Top_P = apiDto.Top_P ?? 1;
        //        Stream = apiDto.Stream ?? false; // deafult to false for now
        //        System_Message = apiDto.System_Message; // set some default for this
        //    }
        //    else
        //    {
        //        Model = apiDto.Model;
        //        Frequency_Penalty = apiDto.Frequency_Penalty;
        //        Presence_Penalty = apiDto.Presence_Penalty;
        //        Max_Tokens = apiDto.Max_Tokens;
        //        Temperature = apiDto.Temperature;
        //        Top_P = apiDto.Top_P;
        //        Stream = apiDto.Stream;
        //        System_Message = apiDto.System_Message;
        //    }

        //    if (apiDto.Stop != null)
        //    {
        //        Stop = apiDto.Stop.ToCommaSeparatedString();
        //    }
        //    if (apiDto.Reference_Profiles != null)
        //    {
        //        Reference_Profiles = apiDto.Reference_Profiles.ToCommaSeparatedString();
        //    }
        //}
    }
}
