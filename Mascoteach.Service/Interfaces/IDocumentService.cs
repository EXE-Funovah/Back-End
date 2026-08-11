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
        Task<IEnumerable<DocumentResponse>> GetAllDocumentsAsync();
        Task<IEnumerable<DocumentResponse>> GetMyDocumentsAsync(int ownerId);
        Task<IEnumerable<DocumentResponse>> GetMyDeletedDocumentsAsync(int ownerId);
        Task<DocumentResponse?> GetDocumentByIdAsync(int id);
        Task<DocumentResponse> UploadDocumentAsync(int ownerId, DocumentCreateRequest request);
        Task<bool> UpdateDocumentAsync(int id, int ownerId, string newS3Key);
        Task<bool> DeleteDocumentAsync(int id, int ownerId);
        Task<DocumentResponse?> ToggleDeleteAsync(int id, int ownerId);
    }
}
