﻿using IntelligenceHub.Common;
using IntelligenceHub.Common.Attributes;

namespace IntelligenceHub.DAL.DTOs
{
    [TableName("ProfileTools")]
    public class DbProfileTool
    {
        public int ProfileID { get; set; }
        public int ToolID { get; set; }
    }
}