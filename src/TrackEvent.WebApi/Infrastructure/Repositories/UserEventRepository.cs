using Microsoft.EntityFrameworkCore;
using TrackEvent.WebApi.Domain.Entities;
using TrackEvent.WebApi.Infrastructure.Data;

namespace TrackEvent.WebApi.Infrastructure.Repositories;

/// <summary>
/// 使用者事件儲存庫實作
/// </summary>
public class UserEventRepository : IUserEventRepository
{
    private readonly TrackEventDbContext _context;

    public UserEventRepository(TrackEventDbContext context)
    {
        _context = context;
    }

    public async Task<UserEvent> AddAsync(UserEvent userEvent, CancellationToken cancellationToken = default)
    {
        await _context.UserEvents.AddAsync(userEvent, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return userEvent;
    }

    public async Task<UserEvent?> GetByEventIdAsync(string eventId, CancellationToken cancellationToken = default)
    {
        return await _context.UserEvents
            .FirstOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
    }
}
