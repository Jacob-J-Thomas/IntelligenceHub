using System;
using System.Collections.Generic;
using System.Linq;
using Nest;
using OpenAICustomFunctionCallingAPI.Controllers.DTOs;

namespace OpenAICustomFunctionCallingAPI.DAL
{
    public interface IAssociativeRepository<T>
    {
        Task<bool> AddAssociationsByProfileIdAsync(int id, List<int> toolIDs);
        Task<int> DeleteAllProfileAssociationsAsync(int id);
    }
}