using System;
using System.Collections.Generic;
using System.Linq;
using Nest;
using IntelligenceHub.Controllers.DTOs;

namespace IntelligenceHub.DAL
{
    public interface IAssociativeRepository<T>
    {
        Task<bool> AddAssociationsByProfileIdAsync(int id, List<int> toolIDs);
        Task<int> DeleteAllProfileAssociationsAsync(int id);
    }
}