namespace OpenAICustomFunctionCallingAPI.API.DTOs
{
    public class Message
    {
        public string Role { get; set; }
        public string Content { get; set; }
        public Message(string role, string content)
        {
            // validate user
            Role = role;
            Content = content;
        }
    }
}
