using Nest;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.ToolDTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.DataAccessDTOs;
using OpenAICustomFunctionCallingAPI.Controllers.DTOs;
using OpenAICustomFunctionCallingAPI.DAL.DTOs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace OpenAICustomFunctionCallingAPI.DAL
{
    public class RagMetaRepository : GenericRepository<RagIndexMetaDataDTO>
    {
        public RagMetaRepository(string connectionString) : base(connectionString)
        {
        }
    }
}