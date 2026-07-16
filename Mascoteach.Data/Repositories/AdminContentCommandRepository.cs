using System.Data;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Mascoteach.Data.Repositories;

public class AdminContentCommandRepository : IAdminContentCommandRepository
{
    private readonly MascoteachDbContext _context;

    public AdminContentCommandRepository(MascoteachDbContext context) =>
        _context = context;

    public Task<Document?> GetDocumentByIdIncludingDeletedAsync(int id) =>
        _context.Documents.FirstOrDefaultAsync(document => document.Id == id);

    public void UpdateDocument(Document document) =>
        _context.Documents.Update(document);

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

    public Task<IDbContextTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.Serializable) =>
        _context.Database.BeginTransactionAsync(isolationLevel);
}
