using Microsoft.AspNetCore.Mvc;
using TrackEvent.WebApi.Contracts.Requests;
using TrackEvent.WebApi.Contracts.Responses;
using TrackEvent.WebApi.Handlers;

namespace TrackEvent.WebApi.Controllers;

/// <summary>
/// 追蹤事件 API 控制器
/// </summary>
[ApiController]
[Route("api/v1/track")]
[Produces("application/json")]
public class TrackController : ControllerBase
{
    private readonly TrackEventHandler _handler;
    private readonly ILogger<TrackController> _logger;

    public TrackController(
        TrackEventHandler handler,
        ILogger<TrackController> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    /// <summary>
    /// 追蹤使用者行為事件
    /// </summary>
    /// <param name="request">事件資料</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>追蹤結果</returns>
    [HttpPost("event")]
    [ProducesResponseType(typeof(TrackEventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TrackEvent(
        [FromBody] TrackEventRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _handler.HandleAsync(request, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        // 處理錯誤
        var errorResponse = new ErrorResponse
        {
            ErrorCode = result.Error.Code,
            Message = result.Error.Message,
            Details = result.Error.Details
        };

        // 根據錯誤類型回傳不同的 HTTP 狀態碼
        return result.Error.Code switch
        {
            "INVALID_PAYLOAD" => BadRequest(errorResponse),
            "INTERNAL_ERROR" => StatusCode(500, errorResponse),
            _ => StatusCode(500, errorResponse)
        };
    }
}
