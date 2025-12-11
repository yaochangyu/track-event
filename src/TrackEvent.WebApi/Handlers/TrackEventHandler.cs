using System.Text.Json;
using TrackEvent.WebApi.Contracts.Requests;
using TrackEvent.WebApi.Contracts.Responses;
using TrackEvent.WebApi.Domain.Entities;
using TrackEvent.WebApi.Infrastructure.Repositories;

namespace TrackEvent.WebApi.Handlers;

/// <summary>
/// 追蹤事件處理器
/// </summary>
public class TrackEventHandler
{
    private readonly IUserEventRepository _repository;
    private readonly ILogger<TrackEventHandler> _logger;

    public TrackEventHandler(
        IUserEventRepository repository,
        ILogger<TrackEventHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// 處理追蹤事件
    /// </summary>
    public async Task<Result<TrackEventResponse>> HandleAsync(
        TrackEventRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 驗證必填欄位
            var validationResult = ValidateRequest(request);
            if (validationResult.IsFailure)
            {
                return Result<TrackEventResponse>.Failure(validationResult.Error);
            }

            // 產生唯一 EventId
            var eventId = GenerateEventId();

            // 解析事件時間
            if (!DateTime.TryParse(request.EventTime, out var eventTime))
            {
                return Result<TrackEventResponse>.Failure(
                    new ValidationError("event_time", "Invalid ISO 8601 format"));
            }

            // 建立實體
            var userEvent = new UserEvent
            {
                EventId = eventId,
                UserId = request.UserId,
                AnonymousId = request.AnonymousId,
                ClientId = request.ClientId,
                SessionId = request.SessionId,
                EventTime = eventTime.ToUniversalTime(),
                Source = request.Source,
                EventType = request.EventType,
                FeatureId = request.FeatureId,
                FeatureName = request.FeatureName,
                FeatureType = request.FeatureType,
                Action = request.Action,
                PageUrl = request.PageUrl,
                PageName = request.PageName,
                PreviousPageUrl = request.PreviousPageUrl,
                PreviousPageName = request.PreviousPageName,
                ScreenName = request.ScreenName,
                PreviousScreenName = request.PreviousScreenName,
                DeviceType = request.DeviceType,
                Os = request.Os,
                OsVersion = request.OsVersion,
                Browser = request.Browser,
                BrowserVersion = request.BrowserVersion,
                AppVersion = request.AppVersion,
                BuildNumber = request.BuildNumber,
                NetworkType = request.NetworkType,
                Locale = request.Locale,
                Experiments = request.Experiments != null
                    ? JsonSerializer.Serialize(request.Experiments)
                    : null,
                Metadata = request.Metadata != null
                    ? JsonSerializer.Serialize(request.Metadata)
                    : null,
                ReceivedAt = DateTime.UtcNow
            };

            // 儲存至資料庫
            var savedEvent = await _repository.AddAsync(userEvent, cancellationToken);

            _logger.LogInformation(
                "Event tracked successfully. EventId: {EventId}, FeatureId: {FeatureId}",
                savedEvent.EventId,
                savedEvent.FeatureId);

            // 回傳結果
            var response = new TrackEventResponse
            {
                EventId = savedEvent.EventId,
                ReceivedAt = savedEvent.ReceivedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            return Result<TrackEventResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while tracking event");
            return Result<TrackEventResponse>.Failure(
                new InternalError("Unexpected error occurred"));
        }
    }

    private Result<bool> ValidateRequest(TrackEventRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId))
            return Result<bool>.Failure(new ValidationError("client_id", "missing"));

        if (string.IsNullOrWhiteSpace(request.SessionId))
            return Result<bool>.Failure(new ValidationError("session_id", "missing"));

        if (string.IsNullOrWhiteSpace(request.EventTime))
            return Result<bool>.Failure(new ValidationError("event_time", "missing"));

        if (string.IsNullOrWhiteSpace(request.Source))
            return Result<bool>.Failure(new ValidationError("source", "missing"));

        if (string.IsNullOrWhiteSpace(request.EventType))
            return Result<bool>.Failure(new ValidationError("event_type", "missing"));

        if (string.IsNullOrWhiteSpace(request.FeatureId))
            return Result<bool>.Failure(new ValidationError("feature_id", "missing"));

        return Result<bool>.Success(true);
    }

    private string GenerateEventId()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
        var guid = Guid.NewGuid().ToString("N")[..10];
        return $"evt_{timestamp}_{guid}";
    }
}

/// <summary>
/// Result Pattern 實作
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T Value { get; }
    public ErrorBase Error { get; }

    private Result(bool isSuccess, T value, ErrorBase error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null!);
    public static Result<T> Failure(ErrorBase error) => new(false, default!, error);
}

/// <summary>
/// 錯誤基礎類別
/// </summary>
public abstract class ErrorBase
{
    public string Code { get; }
    public string Message { get; }
    public object? Details { get; }

    protected ErrorBase(string code, string message, object? details = null)
    {
        Code = code;
        Message = message;
        Details = details;
    }
}

/// <summary>
/// 驗證錯誤
/// </summary>
public class ValidationError : ErrorBase
{
    public ValidationError(string field, string reason)
        : base("INVALID_PAYLOAD", $"field '{field}' is required", new { field, reason })
    {
    }
}

/// <summary>
/// 內部錯誤
/// </summary>
public class InternalError : ErrorBase
{
    public InternalError(string message)
        : base("INTERNAL_ERROR", message)
    {
    }
}
