using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mascoteach.Service.Implementations
{
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _documentRepository;

        public DocumentService(IDocumentRepository documentRepository)
        {
            _documentRepository = documentRepository;
        }

        public async Task<bool> DeleteDocumentAsync(int id)
        {
            var doc = await _documentRepository.GetByIdAsync(id);
            if (doc == null)  return false;

            _documentRepository.Delete(doc);
            return await _documentRepository.SaveChangesAsync() > 0;
        }

        public async Task<Document?> GetDocumentByIdAsync(int id)
        {
            return await _documentRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Document>> GetDocumentsByTeacherAsync(int teacherId)
        {
            var allDocs = await _documentRepository.GetAllAsync();
            return allDocs.Where(d => d.TeacherId == teacherId);
        }

        public async Task<Document> UploadDocumentAsync(int teacherId, string fileUrl)
        {
            var newDoc = new Document
            {
                TeacherId = teacherId,
                FileUrl = fileUrl,
                UploadedAt = DateTime.Now
            };
            await _documentRepository.AddAsync(newDoc);
            await _documentRepository.SaveChangesAsync();
            return newDoc;
        }
    }
}
