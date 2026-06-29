namespace QLNS_BE.Models.Dtos.Common
{/// <summary>
 /// Response chuẩn chung cho API (không generic).
 /// </summary>
    public class ApiResponseDto
    {
        public bool Success { get; set; } = true;
        public string? Message { get; set; }
    }

    /// <summary>
    /// Response chuẩn chung cho API (generic, có Data).
    /// </summary>
    public class ApiResponseDto<T> : ApiResponseDto
    {
        public T? Data { get; set; }
    }
}
