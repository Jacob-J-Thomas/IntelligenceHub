namespace OpenAICustomFunctionCallingAPI.API.DTOs
{
    public class CompletionDto : CompletionBaseDTO
    {

        //public List<Message> Messages { get; set; } = new List<Message>();
        public ResponseFormat? Response_Format { get; private set; }
        //public List<Tool>? Tools { get; set; }
        public string[]? Stop { get; set; }
        public bool? Logprobs { get; set; }

        public CompletionDto(string role, ChatRequestDTO completionRequest)
        {
            Model = completionRequest.Modifiers.Model;
            Frequency_Penalty = completionRequest.Modifiers.Frequency_Penalty;
            Presence_Penalty = completionRequest?.Modifiers.Presence_Penalty;
            Temperature = completionRequest?.Modifiers.Temperature;
            Top_P = completionRequest?.Modifiers.Top_P;
            Stream = completionRequest?.Modifiers.Stream;
            Max_Tokens = completionRequest?.Modifiers.Max_Tokens;
            N = completionRequest?.Modifiers.N;
            Top_Logprobs = completionRequest?.Modifiers.Top_Logprobs;
            Seed = completionRequest?.Modifiers.Seed;
            User = completionRequest?.Modifiers.User;
            Tool_Choice = completionRequest?.Modifiers.Tool_Choice;
            Messages = new List<Message>() 
            { 
                new Message(role, completionRequest.Completion) 
            };
            if (System_Message != null)
            {
                Messages.Add(new Message("system", System_Message));
            }
        }
    }
}
