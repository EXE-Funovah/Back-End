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
        public DocumentRepository(MascoteachContext context) :base(context) 
        {
            
        }

        public async Task<IEnumerable<Document>> GetByTeacherIdAsync(int teacherId)
        {
            return await _context.Documents
                        .Where(d => d.TeacherId == teacherId) // Lọc trực tiếp bằng SQL
                                .ToListAsync();
        }
    }
}
