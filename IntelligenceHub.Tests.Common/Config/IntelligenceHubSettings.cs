using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelligenceHub.Tests.Common.Config
{
    public class IntelligenceHubSettings
    {
        public string TestingUrl { get; set; }
        public string ProfileName { get; set; }
        public string RagDatabase { get; set; }
        public List<string> Completions { get; set; }
    }
}
