using Mascoteach.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mascoteach.Service.Interfaces
{
    public interface IDocumentService
    {
        Task<IEnumerable<Document>> GetDocumentsByTeacherAsync(int teacherId);
        Task<Document?> GetDocumentByIdAsync(int id);
        Task<Document> UploadDocumentAsync(int teacherId, string fileUrl);
        Task<bool> DeleteDocumentAsync(int id);
    }
}
