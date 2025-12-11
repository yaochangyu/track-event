using System.Text.Json.Serialization;

namespace TrackEvent.WebApi.Contracts.Responses;

/// <summary>
/// 追蹤事件成功回應
/// </summary>
public record TrackEventResponse
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = "ok";

    [JsonPropertyName("event_id")]
    public string EventId { get; init; } = string.Empty;

    [JsonPropertyName("received_at")]
    public string ReceivedAt { get; init; } = string.Empty;
}
