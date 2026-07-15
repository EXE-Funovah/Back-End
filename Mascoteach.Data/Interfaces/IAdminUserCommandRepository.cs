using System.Data;
using Mascoteach.Data.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace Mascoteach.Data.Interfaces;

public interface IAdminUserCommandRepository
{
    Task<User?> GetActiveByIdAsync(int id);
    Task<User?> GetByIdIncludingDeletedAsync(int id);
    Task<int> CountActiveAdminsAsync();
    void Update(User user);
    Task<int> SaveChangesAsync();
    Task<IDbContextTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.Serializable);
}
