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
        Task<IEnumerable<Document>> GetByTeacherIdAsync(int teacherId);
    }
}
