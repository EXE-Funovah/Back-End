using Mascoteach.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mascoteach.Data.Interfaces
{
    public interface IDocumentRepository : IGenericRepository<Document> 
    {
        Task<IEnumerable<Document>> GetByOwnerIdAsync(int ownerId);
        Task<IEnumerable<Document>> GetDeletedByOwnerIdAsync(int ownerId);
        Task<Document?> GetByIdIncludingDeletedAsync(int id);
        Task<int> CountActiveByOwnerIdAsync(int ownerId);
    }
}
