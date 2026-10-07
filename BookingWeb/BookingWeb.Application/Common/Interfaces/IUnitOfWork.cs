namespace BookingWeb.Application.Interfaces;

public interface IUnitOfWork
{ 
    Task SaveChangesAsync(CancellationToken ct = default);
    
    Task<IUnitOfWorkTransaction>  BeginTransactionAsync(CancellationToken ct = default);
}

public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);
}