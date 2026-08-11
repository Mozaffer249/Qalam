namespace Qalam.Data.DTOs.OpenSessionRequests;

public enum OpenSessionRequestPublishFailureKind
{
    NotFound = 1,
    Forbidden = 2,
    BadRequest = 3
}

/// <summary>
/// Result of publishing a draft open session request (service → MediatR HTTP mapping).
/// On success, <see cref="RequestId"/> is set; the handler loads/maps the detail DTO.
/// </summary>
public class OpenSessionRequestPublishResultDto
{
    public bool Succeeded { get; init; }
    public OpenSessionRequestPublishFailureKind? FailureKind { get; init; }
    public string? Message { get; init; }
    public int? RequestId { get; init; }

    public static OpenSessionRequestPublishResultDto Success(int requestId) => new()
    {
        Succeeded = true,
        RequestId = requestId
    };

    public static OpenSessionRequestPublishResultDto Fail(
        OpenSessionRequestPublishFailureKind kind,
        string message) => new()
    {
        Succeeded = false,
        FailureKind = kind,
        Message = message
    };
}
