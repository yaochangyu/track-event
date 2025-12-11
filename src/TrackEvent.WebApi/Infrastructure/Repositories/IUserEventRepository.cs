using TrackEvent.WebApi.Domain.Entities;

namespace TrackEvent.WebApi.Infrastructure.Repositories;

/// <summary>
/// 使用者事件儲存庫介面
/// </summary>
public interface IUserEventRepository
{
    /// <summary>
    /// 新增事件
    /// </summary>
    Task<UserEvent> AddAsync(UserEvent userEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根據 EventId 查詢事件
    /// </summary>
    Task<UserEvent?> GetByEventIdAsync(string eventId, CancellationToken cancellationToken = default);
}
