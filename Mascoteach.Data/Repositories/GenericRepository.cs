using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mascoteach.Data.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly MascoteachDbContext _context;
        public GenericRepository(MascoteachDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            var isDeletedProp = typeof(T).GetProperty("IsDeleted");
            if (isDeletedProp != null)
                return await _context.Set<T>()
                    .Where(e => EF.Property<bool>(e, "IsDeleted") == false)
                    .ToListAsync();
            return await _context.Set<T>().ToListAsync();
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            var entity = await _context.Set<T>().FindAsync(id);
            if (entity == null) return null;
            var isDeletedProp = typeof(T).GetProperty("IsDeleted");
            if (isDeletedProp != null && (bool)isDeletedProp.GetValue(entity)! == true)
                return null;
            return entity;
        }

        public async Task AddAsync(T entity) => await _context.Set<T>().AddAsync(entity);

        public void Update(T entity) => _context.Set<T>().Update(entity);

        public void Delete(T entity)
        {
            var isDeletedProp = typeof(T).GetProperty("IsDeleted");
            if (isDeletedProp != null)
                isDeletedProp.SetValue(entity, true);
            else
                _context.Set<T>().Remove(entity);
        }

        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

        public async Task<IDbContextTransaction> BeginTransactionAsync()
            => await _context.Database.BeginTransactionAsync();
    }
}
