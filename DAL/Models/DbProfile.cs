﻿using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using IntelligenceHub.API.DTOs;
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
        public float? FrequencyPenalty { get; set; }
        public float? PresencePenalty { get; set; }
        public float? Temperature { get; set; }
        public float? TopP { get; set; }
        public int? TopLogprobs { get; set; }
        public int? MaxTokens { get; set; }
        public int? Seed { get; set; }
        public string ToolChoice { get; set; }
        public string ResponseFormat { get; set; }
        public string User { get; set; }
        public string SystemMessage { get; set; }
        public string Stop { get; set; }
        public string ReferenceProfiles { get; set; }
        public string ReferenceDescription { get; set; }
        public bool? ReturnRecursion { get; set; }
        public int MaxMessageHistory { get; set; }
    }
}