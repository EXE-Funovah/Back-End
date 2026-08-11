using System.Data;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Mascoteach.Data.Repositories;

public sealed class AdminSessionCommandRepository : IAdminSessionCommandRepository
{
    private readonly MascoteachDbContext _context;

    public AdminSessionCommandRepository(MascoteachDbContext context) =>
        _context = context;

    public Task<LiveSession?> GetByIdIncludingDeletedAsync(int id) =>
        _context.LiveSessions.FirstOrDefaultAsync(session => session.Id == id);

    public void Update(LiveSession session) => _context.LiveSessions.Update(session);

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

    public Task<IDbContextTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.Serializable) =>
        _context.Database.BeginTransactionAsync(isolationLevel);
}
