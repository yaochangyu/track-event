using System.Text.Json.Serialization;

namespace TrackEvent.WebApi.Contracts.Responses;

/// <summary>
/// 錯誤回應
/// </summary>
public record ErrorResponse
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = "error";

    [JsonPropertyName("error_code")]
    public string ErrorCode { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("details")]
    public object? Details { get; init; }
}

/// <summary>
/// 錯誤詳細資訊
/// </summary>
public record ErrorDetails
{
    [JsonPropertyName("field")]
    public string? Field { get; init; }

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }
}
