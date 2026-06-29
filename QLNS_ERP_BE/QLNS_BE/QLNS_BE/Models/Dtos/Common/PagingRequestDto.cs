namespace QLNS_BE.Models.Dtos.Common
{/// <summary>
 /// Tham số phân trang & tìm kiếm cơ bản.
 /// </summary>
    public class PagingRequestDto
    {
        public int PageIndex { get; set; } = 1;              // trang hiện tại (bắt đầu từ 1)
        public int PageSize { get; set; } = 20;              // số dòng mỗi trang
        public string? Keyword { get; set; }                 // từ khóa tìm kiếm (tự do)
        public string? SortField { get; set; }               // tên field sort
        public string? SortDirection { get; set; }           // ASC / DESC
    }
}
