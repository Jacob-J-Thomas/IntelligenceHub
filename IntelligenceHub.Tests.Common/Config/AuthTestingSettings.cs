﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelligenceHub.Tests.Common.Config
{
    public class AuthTestingSettings
    {
        public string AuthEndpoint { get; set; }
        public string AuthClientId { get; set; }
        public string AuthClientSecret { get; set; }
        public string ElevatedAuthClientId { get; set; }
        public string ElevatedAuthClientSecret { get; set; }
        public string Audience { get; set; }
    }
}