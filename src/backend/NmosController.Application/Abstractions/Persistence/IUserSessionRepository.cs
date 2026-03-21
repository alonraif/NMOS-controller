using NmosController.Domain.Entities;

namespace NmosController.Application.Abstractions.Persistence;

public interface IUserSessionRepository
{
    Task<UserSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(UserSession session, CancellationToken cancellationToken);
    Task UpdateAsync(UserSession session, CancellationToken cancellationToken);
}
