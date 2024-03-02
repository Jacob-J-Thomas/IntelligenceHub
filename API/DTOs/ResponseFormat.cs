namespace OpenAICustomFunctionCallingAPI.API.DTOs
{
    public class ResponseFormat
    {
        public ResponseFormat(string type)
        {
            Type = type;
        }

        public string Type { get; set; }
    }
}
