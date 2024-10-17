

namespace IntelligenceHub.DAL
{
    public interface IAssociativeRepository<T>
    {
        Task<bool> AddAssociationsByProfileIdAsync(int id, List<int> toolIDs);
        Task<int> DeleteAllProfileAssociationsAsync(int id);
    }
}