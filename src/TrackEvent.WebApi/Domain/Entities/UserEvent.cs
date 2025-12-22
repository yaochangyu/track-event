namespace TrackEvent.WebApi.Domain.Entities;

/// <summary>
/// 使用者事件實體 (對應 Elasticsearch Document)
/// </summary>
public class UserEvent
{
    /// <summary>
    /// 事件唯一識別碼 (對應 Elasticsearch _id)
    /// </summary>
    public string EventId { get; set; } = string.Empty;

    // ===== 系統識別（多產品） =====
    /// <summary>
    /// 產品/系統識別碼 (多產品用，必填)
    /// </summary>
    public string ProductId { get; set; } = string.Empty;

    // ===== 身份識別 =====
    /// <summary>
    /// 已登入用戶 ID
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// 匿名用戶 ID
    /// </summary>
    public string? AnonymousId { get; set; }

    /// <summary>
    /// 裝置或瀏覽器實體 ID (必填)
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Session ID (必填)
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    // ===== 事件核心資訊 =====
    /// <summary>
    /// 事件時間 (UTC)
    /// </summary>
    public DateTime EventTime { get; set; }

    /// <summary>
    /// 事件來源平台 (web, app_ios, app_android)
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// 事件類型 (click, page_view, screen_view, submit)
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    // ===== 功能與行為資訊 =====
    /// <summary>
    /// 功能唯一 ID (必填)
    /// </summary>
    public string FeatureId { get; set; } = string.Empty;

    /// <summary>
    /// 功能可讀名稱
    /// </summary>
    public string? FeatureName { get; set; }

    /// <summary>
    /// 功能類型 (button, tab, link, icon, menu_item, cta)
    /// </summary>
    public string? FeatureType { get; set; }

    /// <summary>
    /// 行為動作 (click, open, close, toggle)
    /// </summary>
    public string? Action { get; set; }

    // ===== 場景上下文 (Web) =====
    /// <summary>
    /// 當前頁面 URL
    /// </summary>
    public string? PageUrl { get; set; }

    /// <summary>
    /// 當前頁面邏輯名稱
    /// </summary>
    public string? PageName { get; set; }

    /// <summary>
    /// 上一頁 URL
    /// </summary>
    public string? PreviousPageUrl { get; set; }

    /// <summary>
    /// 上一頁邏輯名稱
    /// </summary>
    public string? PreviousPageName { get; set; }

    // ===== 場景上下文 (App) =====
    /// <summary>
    /// 當前畫面名稱
    /// </summary>
    public string? ScreenName { get; set; }

    /// <summary>
    /// 上一畫面名稱
    /// </summary>
    public string? PreviousScreenName { get; set; }

    // ===== 環境資訊 =====
    /// <summary>
    /// 裝置類型 (desktop, mobile, tablet)
    /// </summary>
    public string? DeviceType { get; set; }

    /// <summary>
    /// 作業系統 (Windows, macOS, iOS, Android)
    /// </summary>
    public string? Os { get; set; }

    /// <summary>
    /// 作業系統版本
    /// </summary>
    public string? OsVersion { get; set; }

    /// <summary>
    /// 瀏覽器名稱
    /// </summary>
    public string? Browser { get; set; }

    /// <summary>
    /// 瀏覽器版本
    /// </summary>
    public string? BrowserVersion { get; set; }

    /// <summary>
    /// App 版本號
    /// </summary>
    public string? AppVersion { get; set; }

    /// <summary>
    /// App build number
    /// </summary>
    public string? BuildNumber { get; set; }

    /// <summary>
    /// 網路類型 (wifi, 4G, 5G)
    /// </summary>
    public string? NetworkType { get; set; }

    /// <summary>
    /// 語系 (zh-TW, en-US)
    /// </summary>
    public string? Locale { get; set; }

    // ===== 實驗 / 附加資訊 =====
    /// <summary>
    /// 實驗資訊 (動態物件)
    /// </summary>
    public object? Experiments { get; set; }

    /// <summary>
    /// 彈性附加資訊 (動態物件)
    /// </summary>
    public object? Metadata { get; set; }

    // ===== 系統欄位 =====
    /// <summary>
    /// 後端接收時間 (UTC)
    /// </summary>
    public DateTime ReceivedAt { get; set; }
}
