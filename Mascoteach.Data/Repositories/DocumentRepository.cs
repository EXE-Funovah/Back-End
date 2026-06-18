using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mascoteach.Data.Repositories
{
    public class DocumentRepository : GenericRepository<Document>, IDocumentRepository
    {
        public DocumentRepository(MascoteachDbContext context) : base(context) 
        {
            
        }

        public async Task<IEnumerable<Document>> GetByOwnerIdAsync(int ownerId)
        {
            return await _context.Documents
                        .Where(d => d.OwnerId == ownerId && d.IsDeleted == false)
                        .ToListAsync();
        }

        public async Task<Document?> GetByIdIncludingDeletedAsync(int id)
        {
            return await _context.Documents.FindAsync(id);
        }

        public async Task<int> CountActiveByOwnerIdAsync(int ownerId)
        {
            return await _context.Documents
                .CountAsync(d => d.OwnerId == ownerId && d.IsDeleted == false);
        }
    }
}
