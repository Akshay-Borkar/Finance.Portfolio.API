using Finance.AlertService.Application.Contracts.Persistence;
using Finance.AlertService.Domain.Common;
using Finance.AlertService.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;

namespace Finance.AlertService.Persistence.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
{
    protected readonly AlertDbContext _context;

    public GenericRepository(AlertDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<T>> GetAsync()
        => await _context.Set<T>().AsNoTracking().ToListAsync();

    public async Task<T?> GetByIdAsync(Guid id)
        => await _context.Set<T>().AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

    public async Task CreateAsync(T entity)
    {
        await _context.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        _context.Entry(entity).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(T entity)
    {
        _context.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
