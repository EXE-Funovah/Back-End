using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mascoteach.Service.Interfaces
{
    public interface IDocumentService
    {
        Task<IEnumerable<DocumentResponse>> GetMyDocumentsAsync(int teacherId);
        Task<DocumentResponse?> GetDocumentByIdAsync(int id);
        Task<DocumentResponse> UploadDocumentAsync(int teacherId, DocumentCreateRequest request);
        Task<bool> UpdateDocumentAsync(int id, int requestTeacherId, string newFileUrl);
        Task<bool> DeleteDocumentAsync(int id, int requestTeacherId);
    }
}
