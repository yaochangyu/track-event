using Elastic.Clients.Elasticsearch;
using TrackEvent.WebApi.Domain.Entities;

namespace TrackEvent.WebApi.Infrastructure.Repositories;

/// <summary>
/// 使用者事件儲存庫實作（Elasticsearch）
/// </summary>
public class UserEventRepository : IUserEventRepository
{
    private readonly ElasticsearchClient _client;
    private const string WriteAlias = "user-events-write";
    private const string ReadAlias = "user-events-read";

    public UserEventRepository(ElasticsearchClient client)
    {
        _client = client;
    }

    public async Task<UserEvent> AddAsync(UserEvent userEvent, CancellationToken cancellationToken = default)
    {
        // 寫入到 Elasticsearch
        var response = await _client.IndexAsync(
            userEvent,
            idx => idx
                .Index(WriteAlias)
                .Id(userEvent.EventId), // 使用 event_id 作為 document ID
            cancellationToken
        );

        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException(
                $"Failed to index event: {response.DebugInformation}"
            );
        }

        return userEvent;
    }

    public async Task<UserEvent?> GetByEventIdAsync(string eventId, CancellationToken cancellationToken = default)
    {
        var response = await _client.GetAsync<UserEvent>(
            eventId,
            idx => idx.Index(ReadAlias),
            cancellationToken
        );

        return response.IsValidResponse ? response.Source : null;
    }
}
