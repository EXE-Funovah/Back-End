using System.Data;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Mascoteach.Data.Repositories;

public class AdminUserCommandRepository : IAdminUserCommandRepository
{
    private readonly MascoteachDbContext _context;

    public AdminUserCommandRepository(MascoteachDbContext context) => _context = context;

    public Task<User?> GetActiveByIdAsync(int id) =>
        _context.Users.FirstOrDefaultAsync(user => user.Id == id && !user.IsDeleted);

    public Task<int> CountActiveAdminsAsync() =>
        _context.Users.CountAsync(user => !user.IsDeleted && user.Role == "Admin");

    public void Update(User user) => _context.Users.Update(user);

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

    public Task<IDbContextTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.Serializable) =>
        _context.Database.BeginTransactionAsync(isolationLevel);
}

