using BookingWeb.Application.Bookings;
using BookingWeb.Application.Common.Interfaces;
using BookingWeb.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

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
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
                                                  {
                                                      SqlState: PostgresErrorCodes.ExclusionViolation,
                                                      ConstraintName: BookingConfiguration.NoOverlapConstraintName
                                                  })
        {
            throw new BookingOverlapException(exception);
        }
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