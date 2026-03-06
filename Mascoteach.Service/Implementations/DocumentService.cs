using AutoMapper;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public DocumentService(IDocumentRepository documentRepository, IUserRepository userRepository, IMapper mapper)
    {
        _documentRepository = documentRepository;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<DocumentResponse> UploadDocumentAsync(int teacherId, DocumentCreateRequest request)
    {
        var user = await _userRepository.GetByIdAsync(teacherId)
            ?? throw new KeyNotFoundException($"User with id {teacherId} not found.");

        if (user.SubscriptionTier == "Freemium" && (user.DocumentsProcessed ?? 0) >= 5)
            throw new InvalidOperationException(
                "You have reached the limit of 5 documents for the Freemium tier. Please upgrade to Premium.");

        using var transaction = await _documentRepository.BeginTransactionAsync();
        try
        {
            var newDoc = new Document
            {
                TeacherId = teacherId,
                FileUrl = request.FileUrl,
                UploadedAt = DateTime.Now
            };
            await _documentRepository.AddAsync(newDoc);
            await _documentRepository.SaveChangesAsync();

            user.DocumentsProcessed = (user.DocumentsProcessed ?? 0) + 1;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            await transaction.CommitAsync();
            return _mapper.Map<DocumentResponse>(newDoc);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
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

    public async Task<DocumentResponse?> ToggleDeleteAsync(int id, int teacherId)
    {
        // Bypass soft-delete filter to find any doc (including already-deleted ones)
        var doc = await _documentRepository.GetAllIncludingDeletedAsync(id);
        if (doc == null || doc.TeacherId != teacherId) return null;
        doc.IsDeleted = !doc.IsDeleted;
        _documentRepository.Update(doc);
        await _documentRepository.SaveChangesAsync();
        return _mapper.Map<DocumentResponse>(doc);
    }
}