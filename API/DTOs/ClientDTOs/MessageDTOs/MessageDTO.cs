namespace OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.MessageDTOs
{
    public class MessageDTO
    {
        public string Role { get; set; }
        public string Content { get; set; }

        public MessageDTO() { }

        public MessageDTO(string role, string content)
        {
            // validate role

            Role = role;
            Content = content;

            // add additional properties

        }
    }
}
