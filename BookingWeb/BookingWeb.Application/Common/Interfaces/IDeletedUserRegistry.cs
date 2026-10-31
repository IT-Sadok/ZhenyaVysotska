namespace BookingWeb.Application.Common.Interfaces;

public interface IDeletedUserRegistry
{
    void Add(Guid userId);

    bool IsDeleted(Guid userId);
}
