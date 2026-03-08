using AutoMapper;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;

namespace Mascoteach.Service.Implementations;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly IS3Service _s3Service;

    public DocumentService(IDocumentRepository documentRepository, IUserRepository userRepository, IMapper mapper, IS3Service s3Service)
    {
        _documentRepository = documentRepository;
        _userRepository = userRepository;
        _mapper = mapper;
        _s3Service = s3Service;
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
                FileUrl = request.S3Key,
                UploadedAt = DateTime.Now
            };
            await _documentRepository.AddAsync(newDoc);
            await _documentRepository.SaveChangesAsync();

            user.DocumentsProcessed = (user.DocumentsProcessed ?? 0) + 1;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            await transaction.CommitAsync();
            
            var response = _mapper.Map<DocumentResponse>(newDoc);
            response.PresignedUrl = await _s3Service.GeneratePresignedDownloadUrlAsync(newDoc.FileUrl);
            return response;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<DocumentResponse>> GetAllDocumentsAsync()
    {
        var docs = await _documentRepository.GetAllAsync();
        var responses = _mapper.Map<IEnumerable<DocumentResponse>>(docs).ToList();
        
        foreach (var response in responses)
        {
            response.PresignedUrl = await _s3Service.GeneratePresignedDownloadUrlAsync(response.S3Key);
        }
        
        return responses;
    }

    public async Task<IEnumerable<DocumentResponse>> GetMyDocumentsAsync(int teacherId)
    {
        var myDocs = await _documentRepository.GetByTeacherIdAsync(teacherId);
        var responses = _mapper.Map<IEnumerable<DocumentResponse>>(myDocs).ToList();
        
        foreach (var response in responses)
        {
            response.PresignedUrl = await _s3Service.GeneratePresignedDownloadUrlAsync(response.S3Key);
        }
        
        return responses;
    }

    public async Task<DocumentResponse?> GetDocumentByIdAsync(int id)
    {
        var doc = await _documentRepository.GetByIdAsync(id);
        if (doc == null) return null;
        
        var response = _mapper.Map<DocumentResponse>(doc);
        response.PresignedUrl = await _s3Service.GeneratePresignedDownloadUrlAsync(response.S3Key);
        return response;
    }

    public async Task<bool> UpdateDocumentAsync(int id, int teacherId, string newS3Key)
    {
        var doc = await _documentRepository.GetByIdAsync(id);
        if (doc == null || doc.TeacherId != teacherId) return false;
        doc.FileUrl = newS3Key;
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
        
        var response = _mapper.Map<DocumentResponse>(doc);
        response.PresignedUrl = await _s3Service.GeneratePresignedDownloadUrlAsync(response.S3Key);
        return response;
    }
}