using AutoMapper;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IMapper _mapper;

    public DocumentService(IDocumentRepository documentRepository, IMapper mapper)
    {
        _documentRepository = documentRepository;
        _mapper = mapper;
    }

    public async Task<DocumentResponse> UploadDocumentAsync(int teacherId, DocumentCreateRequest request)
    {
        var newDoc = new Document
        {
            TeacherId = teacherId,
            FileUrl = request.FileUrl,
            UploadedAt = DateTime.Now
        };
        await _documentRepository.AddAsync(newDoc);
        await _documentRepository.SaveChangesAsync();
        return _mapper.Map<DocumentResponse>(newDoc);
    }

    public async Task<IEnumerable<DocumentResponse>> GetMyDocumentsAsync(int teacherId)
    {
        var myDocs = await _documentRepository.GetByTeacherIdAsync(teacherId);

        return _mapper.Map<IEnumerable<DocumentResponse>>(myDocs);
    }

    public async Task<DocumentResponse?> GetDocumentByIdAsync(int id)
    {
        var doc = await _documentRepository.GetByIdAsync(id);
        return _mapper.Map<DocumentResponse>(doc);
    }

    public async Task<bool> UpdateDocumentAsync(int id, int teacherId, string newFileUrl)
    {
        var doc = await _documentRepository.GetByIdAsync(id);
        if (doc == null || doc.TeacherId != teacherId) return false;
        doc.FileUrl = newFileUrl;
        _documentRepository.Update(doc);
        return await _documentRepository.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteDocumentAsync(int id, int teacherId)
    {
        var doc = await _documentRepository.GetByIdAsync(id);
        if (doc == null || doc.TeacherId != teacherId) return false;
        _documentRepository.Delete(doc);
        return await _documentRepository.SaveChangesAsync() > 0;
    }

}