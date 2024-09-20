using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.ClientDTOs.ToolDTOs;
using IntelligenceHub.Common;
using IntelligenceHub.Common.Attributes; 
using IntelligenceHub.Common.Extensions;
using IntelligenceHub.DAL;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IntelligenceHub.API.MigratedDTOs;

namespace IntelligenceHub.DAL.DTOs
{
    // extend this from a common DTO?
    [TableName("Profiles")]
    public class DbProfile
    {
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
        public int? Seed { get; set; }
        public string Tool_Choice { get; set; }
        public string Response_Format { get; set; }
        public string User { get; set; }
        public string System_Message { get; set; }
        public string Stop { get; set; }
        public string Reference_Profiles { get; set; }
        public string Reference_Description { get; set; }
        public bool? Return_Recursion { get; set; }
    }
}
