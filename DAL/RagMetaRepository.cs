using Nest;
using IntelligenceHub.API.DTOs.ClientDTOs.ToolDTOs;
using IntelligenceHub.API.DTOs.DataAccessDTOs;
using IntelligenceHub.Controllers.DTOs;
using IntelligenceHub.DAL.DTOs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace IntelligenceHub.DAL
{
    public class RagMetaRepository : GenericRepository<RagIndexMetaDataDTO>
    {
        public RagMetaRepository(string connectionString) : base(connectionString)
        {
        }
    }
}