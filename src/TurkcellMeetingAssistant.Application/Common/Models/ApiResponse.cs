namespace TurkcellMeetingAssistant.Application.Common.Models;

public class ApiResponse<T>
{
    public T? Data { get; set; }
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public List<ApiError>? Errors { get; set; }

    public static ApiResponse<T> Success(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Data = data,
            IsSuccess = true,
            Message = message
        };
    }

    public static ApiResponse<T> Fail(string message, List<ApiError>? errors = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            Message = message,
            Errors = errors
        };
    }
}
