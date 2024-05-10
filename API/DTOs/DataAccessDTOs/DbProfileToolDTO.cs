using OpenAICustomFunctionCallingAPI.Common;
using OpenAICustomFunctionCallingAPI.Common.Attributes;

namespace OpenAICustomFunctionCallingAPI.DAL.DTOs
{
    [TableName("ProfileTools")]
    public class DbProfileToolDTO
    {
        public int ProfileID { get; set; }
        public int ToolID { get; set; }
    }
}
