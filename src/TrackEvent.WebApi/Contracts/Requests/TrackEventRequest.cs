using System.Text.Json.Serialization;

namespace TrackEvent.WebApi.Contracts.Requests;

/// <summary>
/// 追蹤事件請求
/// </summary>
public record TrackEventRequest
{
    // ===== 身份識別 =====
    [JsonPropertyName("user_id")]
    public string? UserId { get; init; }

    [JsonPropertyName("anonymous_id")]
    public string? AnonymousId { get; init; }

    [JsonPropertyName("client_id")]
    public string ClientId { get; init; } = string.Empty;

    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    // ===== 事件核心資訊 =====
    [JsonPropertyName("event_time")]
    public string EventTime { get; init; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; init; } = string.Empty;

    [JsonPropertyName("event_type")]
    public string EventType { get; init; } = string.Empty;

    // ===== 功能與行為資訊 =====
    [JsonPropertyName("feature_id")]
    public string FeatureId { get; init; } = string.Empty;

    [JsonPropertyName("feature_name")]
    public string? FeatureName { get; init; }

    [JsonPropertyName("feature_type")]
    public string? FeatureType { get; init; }

    [JsonPropertyName("action")]
    public string? Action { get; init; }

    // ===== 場景上下文 (Web) =====
    [JsonPropertyName("page_url")]
    public string? PageUrl { get; init; }

    [JsonPropertyName("page_name")]
    public string? PageName { get; init; }

    [JsonPropertyName("previous_page_url")]
    public string? PreviousPageUrl { get; init; }

    [JsonPropertyName("previous_page_name")]
    public string? PreviousPageName { get; init; }

    // ===== 場景上下文 (App) =====
    [JsonPropertyName("screen_name")]
    public string? ScreenName { get; init; }

    [JsonPropertyName("previous_screen_name")]
    public string? PreviousScreenName { get; init; }

    // ===== 環境資訊 =====
    [JsonPropertyName("device_type")]
    public string? DeviceType { get; init; }

    [JsonPropertyName("os")]
    public string? Os { get; init; }

    [JsonPropertyName("os_version")]
    public string? OsVersion { get; init; }

    [JsonPropertyName("browser")]
    public string? Browser { get; init; }

    [JsonPropertyName("browser_version")]
    public string? BrowserVersion { get; init; }

    [JsonPropertyName("app_version")]
    public string? AppVersion { get; init; }

    [JsonPropertyName("build_number")]
    public string? BuildNumber { get; init; }

    [JsonPropertyName("network_type")]
    public string? NetworkType { get; init; }

    [JsonPropertyName("locale")]
    public string? Locale { get; init; }

    // ===== 實驗 / 附加資訊 =====
    [JsonPropertyName("experiments")]
    public Dictionary<string, string>? Experiments { get; init; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; init; }
}
