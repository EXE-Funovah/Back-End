using AutoMapper;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Mascoteach.Service.Implementations;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly IS3Service _s3Service;
    private readonly IConfiguration _configuration;
    private const int DefaultFreemiumActiveDocumentLimit = 5;

    public DocumentService(
        IDocumentRepository documentRepository,
        IUserRepository userRepository,
        IMapper mapper,
        IS3Service s3Service,
        IConfiguration configuration)
    {
        _documentRepository = documentRepository;
        _userRepository = userRepository;
        _mapper = mapper;
        _s3Service = s3Service;
        _configuration = configuration;
    }

    private static void EnsureZipS3Key(string s3Key)
    {
        if (string.IsNullOrWhiteSpace(s3Key))
            throw new ArgumentException("S3 key is required.");

        if (!s3Key.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"S3 key must end with .zip. Got: {s3Key}");
    }

    private int GetFreemiumActiveDocumentLimit()
    {
        var configuredLimit = _configuration["Plans:FreemiumActiveDocumentLimit"];

        if (int.TryParse(configuredLimit, out var limit) && limit > 0)
            return limit;

        return DefaultFreemiumActiveDocumentLimit;
    }

    private async Task EnsureCanActivateDocumentAsync(User user)
    {
        var isPremium = string.Equals(user.SubscriptionTier, "Premium", StringComparison.OrdinalIgnoreCase);
        if (isPremium) return;

        var activeDocumentCount = await _documentRepository.CountActiveByOwnerIdAsync(user.Id);
        var activeDocumentLimit = GetFreemiumActiveDocumentLimit();

        if (activeDocumentCount >= activeDocumentLimit)
            throw new InvalidOperationException(
                $"You have reached the limit of {activeDocumentLimit} active documents for the Freemium tier. Please upgrade to Premium.");
    }

    public async Task<DocumentResponse> UploadDocumentAsync(int ownerId, DocumentCreateRequest request)
    {
        EnsureZipS3Key(request.S3Key);

        var user = await _userRepository.GetByIdAsync(ownerId)
            ?? throw new KeyNotFoundException($"User with id {ownerId} not found.");

        await EnsureCanActivateDocumentAsync(user);

        using var transaction = await _documentRepository.BeginTransactionAsync();
        try
        {
            var newDoc = new Document
            {
                OwnerId = ownerId,
                FileUrl = request.S3Key,
                FileName = string.IsNullOrWhiteSpace(request.FileName) ? null : request.FileName.Trim(),
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

    public async Task<IEnumerable<DocumentResponse>> GetMyDocumentsAsync(int ownerId)
    {
        var myDocs = await _documentRepository.GetByOwnerIdAsync(ownerId);
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

    public async Task<bool> UpdateDocumentAsync(int id, int ownerId, string newS3Key)
    {
        EnsureZipS3Key(newS3Key);

        var doc = await _documentRepository.GetByIdAsync(id);
        if (doc == null || doc.OwnerId != ownerId) return false;
        doc.FileUrl = newS3Key;
        _documentRepository.Update(doc);
        return await _documentRepository.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteDocumentAsync(int id, int ownerId)
    {
        var doc = await _documentRepository.GetByIdAsync(id);
        if (doc == null || doc.OwnerId != ownerId) return false;
        _documentRepository.Delete(doc);
        return await _documentRepository.SaveChangesAsync() > 0;
    }

    public async Task<DocumentResponse?> ToggleDeleteAsync(int id, int ownerId)
    {
        // Bypass soft-delete filter to find any doc (including already-deleted ones)
        var doc = await _documentRepository.GetByIdIncludingDeletedAsync(id);
        if (doc == null || doc.OwnerId != ownerId) return null;

        if (doc.IsDeleted)
        {
            var user = await _userRepository.GetByIdAsync(ownerId)
                ?? throw new KeyNotFoundException($"User with id {ownerId} not found.");
            await EnsureCanActivateDocumentAsync(user);
        }

        doc.IsDeleted = !doc.IsDeleted;
        _documentRepository.Update(doc);
        await _documentRepository.SaveChangesAsync();
        
        var response = _mapper.Map<DocumentResponse>(doc);
        response.PresignedUrl = await _s3Service.GeneratePresignedDownloadUrlAsync(response.S3Key);
        return response;
    }
}
