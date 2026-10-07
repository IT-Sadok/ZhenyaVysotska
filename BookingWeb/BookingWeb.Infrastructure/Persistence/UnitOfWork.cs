using BookingWeb.Application.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace BookingWeb.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _db;
    
    public UnitOfWork(ApplicationDbContext db)
    {
        _db = db;
    }
    
    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        var transaction = await _db.Database.BeginTransactionAsync(ct);
        return new EfUnitOfWorkTransaction(transaction);
    }

    private sealed class EfUnitOfWorkTransaction : IUnitOfWorkTransaction
    {
        private readonly IDbContextTransaction _dbContextTransaction;

        public EfUnitOfWorkTransaction(IDbContextTransaction dbContextTransaction)
        {
            _dbContextTransaction = dbContextTransaction;
        }
        
        public Task CommitAsync(CancellationToken ct = default)
        {
            return _dbContextTransaction.CommitAsync(ct);
        }
        
        public ValueTask DisposeAsync()
        {
            return _dbContextTransaction.DisposeAsync();
        }
    }
}