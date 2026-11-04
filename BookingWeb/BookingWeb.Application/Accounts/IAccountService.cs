using BookingWeb.Application.Results;

namespace BookingWeb.Application.Accounts;

public interface IAccountService
{
    Task<Result> DeleteAsync(Guid userId, DeleteAccountRequest request, CancellationToken ct = default);
}
