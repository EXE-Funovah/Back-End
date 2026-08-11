using System.Data;
using Mascoteach.Data.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace Mascoteach.Data.Interfaces;

public interface IAdminSessionCommandRepository
{
    Task<LiveSession?> GetByIdIncludingDeletedAsync(int id);
    void Update(LiveSession session);
    Task<int> SaveChangesAsync();
    Task<IDbContextTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.Serializable);
}
