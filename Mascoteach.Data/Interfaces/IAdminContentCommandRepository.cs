using System.Data;
using Mascoteach.Data.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace Mascoteach.Data.Interfaces;

public interface IAdminContentCommandRepository
{
    Task<Document?> GetDocumentByIdIncludingDeletedAsync(int id);
    void UpdateDocument(Document document);
    Task<int> SaveChangesAsync();
    Task<IDbContextTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.Serializable);
}
