using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Newtonsoft.Json;
using OpenAICustomFunctionCallingAPI.Client.DTOs;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using OpenAICustomFunctionCallingAPI.Controllers.DTOs;
using OpenAICustomFunctionCallingAPI.Business;

namespace OpenAICustomFunctionCallingAPI.API.DTOs
{
    // This class is modeled after an OpenAI completion request
    public class CompletionBaseDTO
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [JsonIgnore]
        public int Id { get; set; }
        public string? Model { get; set; }
        [JsonProperty("frequency_penalty")]
        public double? Frequency_Penalty { get; set; }
        [JsonProperty("presence_penalty")]
        public double? Presence_Penalty { get; set; }
        public double? Temperature { get; set; }
        [JsonProperty("top_p")]
        public double? Top_P { get; set; }
        public bool? Stream { get; set; }
        [JsonProperty("max_tokens")]
        public int? Max_Tokens { get; set; }
        public int? N { get; set; }
        [JsonProperty("top_logprobs")]
        public int? Top_Logprobs { get; set; }
        public int? Seed { get; set; }
        public string? User { get; set; }
        [JsonProperty("tool_choice")]
        public string? Tool_Choice { get; set; } // add validation for these (and probably more)
        [JsonProperty("response_format")]
        public string? Response_Format { get; set; }
        [JsonIgnore] // this can't be ignored here. move system message to database
        public string? System_Message { get; set; }
        [NotMapped] // not mapped isn't doing anything here
        public string[]? Stop { get; set; }
        [NotMapped]
        public List<Tool>? Tools { get; set; }
        [NotMapped]
        public List<Message> Messages { get; set; } = new List<Message>();

        // not including for now
        //public string[]? Logit_Bias { get; set; } 

        public CompletionBaseDTO() { }

        public CompletionBaseDTO(APIProfileDTO openAIRequest)
        {
            //if (modifiers != null)
            //{
            Model = openAIRequest.Model;
            Frequency_Penalty = openAIRequest.Frequency_Penalty;
            Presence_Penalty = openAIRequest.Presence_Penalty;
            Temperature = openAIRequest.Temperature;
            Top_P = openAIRequest.Top_P;
            Stream = openAIRequest.Stream;
            Max_Tokens = openAIRequest.Max_Tokens;
            N = openAIRequest.N;
            Top_Logprobs = openAIRequest.Top_Logprobs;
            Seed = openAIRequest.Seed;
            User = openAIRequest.User;
            Tool_Choice = openAIRequest.Tool_Choice;
            System_Message = openAIRequest.System_Message;
            //}

            //profile references?
            //profile tools?
            //stop?
            //others?
        }

        public CompletionBaseDTO(APIProfileDTO openAIRequest, CompletionBaseDTO modifiers)
        {
            //if (modifiers != null)
            //{
                Model = modifiers.Model ?? openAIRequest.Model;
                Frequency_Penalty = modifiers.Frequency_Penalty ?? openAIRequest.Frequency_Penalty;
                Presence_Penalty = modifiers.Presence_Penalty ?? openAIRequest.Presence_Penalty;
                Temperature = modifiers.Temperature ?? openAIRequest.Temperature;
                Top_P = modifiers.Top_P ?? openAIRequest.Top_P;
                Stream = modifiers.Stream ?? openAIRequest.Stream;
                Max_Tokens = modifiers.Max_Tokens ?? openAIRequest.Max_Tokens;
                N = modifiers.N ?? openAIRequest.N;
                Top_Logprobs = modifiers.Top_Logprobs ?? openAIRequest.Top_Logprobs;
                Seed = modifiers.Seed ?? openAIRequest.Seed;
                User = modifiers.User ?? openAIRequest.User;
                Tool_Choice = modifiers.Tool_Choice ?? openAIRequest.Tool_Choice;
                System_Message = modifiers.System_Message ?? openAIRequest.System_Message;
            //}
            
            //profile references?
            //profile tools?
            //stop?
            //others?
        }
    }
}
