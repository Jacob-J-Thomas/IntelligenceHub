using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Newtonsoft.Json;
using IntelligenceHub.API.DTOs.ClientDTOs.ToolDTOs;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using IntelligenceHub.Controllers.DTOs;
using IntelligenceHub.Business;
using IntelligenceHub.DAL.DTOs;
using IntelligenceHub.Common.Extensions;
using System.Runtime.InteropServices;

namespace IntelligenceHub.API.DTOs.ClientDTOs.AICompletionDTOs
{
    public class BaseCompletionDTO
    {
        public string? Model { get; set; }
        [JsonProperty("frequency_penalty")]
        public float? Frequency_Penalty { get; set; }
        [JsonProperty("presence_penalty")]
        public float? Presence_Penalty { get; set; }
        public float? Temperature { get; set; }
        [JsonProperty("top_p")]
        public float? Top_P { get; set; }
        [JsonProperty("max_tokens")]
        public int? Max_Tokens { get; set; }
        public int? N { get; set; }
        public int? Seed { get; set; }
        public bool? Stream { get; set; }
        [JsonProperty("top_logprobs")]
        public int? Top_Logprobs { get; set; }
        public bool? Logprobs { get; set; } // set using Top_Logprobs
        public string? User { get; set; }
        [JsonProperty("tool_choice")]
        public string? Tool_Choice { get; set; } // add validation for these (and probably more)
        [JsonProperty("response_format")]
        public string? Response_Format { get; set; }
        public virtual string? System_Message { get; set; } // maybe move this
        public string[]? Stop { get; set; }
        public List<ToolDTO> Tools { get; set; } //= new List<ToolDTO>();
        public virtual string? Reference_Description { get; set; } // probably move this
        public virtual bool? Return_Recursion { get; set; }

        // not including for now
        //public string[]? Logit_Bias { get; set; } 

        public BaseCompletionDTO() { }

        public BaseCompletionDTO(Profile completion)
        {   
            // create a seperate method for this instead of passing null
            ConvertAPIProfileAndSetModifiers(completion, null);
        }

        public BaseCompletionDTO(Profile openAIRequest, BaseCompletionDTO? modifiers)
        {
            ConvertAPIProfileAndSetModifiers(openAIRequest, modifiers);
        }

        public void ConvertAPIProfileAndSetModifiers(Profile openAIRequest, BaseCompletionDTO? modifiers)
        {
            if (modifiers == null)
            {
                modifiers = new BaseCompletionDTO();
            }
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
            Return_Recursion = modifiers.Return_Recursion ?? openAIRequest.Return_Recursion;

            SetTools(openAIRequest.Tools, modifiers.Tools);
            SetStop(openAIRequest.Stop, modifiers.Stop);
            SetLogProbs();
        }

        private void SetTools(List<ToolDTO>? requestTools, List<ToolDTO>? modifierTools)
        {
            if (modifierTools != null && modifierTools.Count > 0)
            {
                Tools = modifierTools;
            }
            else if (requestTools != null && requestTools.Count > 0)
            {
                Tools = requestTools;
            }
        }

        private void SetStop(string[]? stopArray, string[]? modifierStopArray)
        {
            if (modifierStopArray != null && modifierStopArray.Length > 0)
            {
                Stop = modifierStopArray;
            }
            else if (stopArray != null && stopArray.Length > 0)
            {
                Stop = stopArray;
            }
        }

        private void SetLogProbs()
        {
            if (Top_Logprobs != null && Top_Logprobs > 0)
            {
                Logprobs = true;
            }
            else
            {
                Logprobs = false;
            }
        }
    }
}
