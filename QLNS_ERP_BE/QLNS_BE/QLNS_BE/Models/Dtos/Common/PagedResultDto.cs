namespace QLNS_BE.Models.Dtos.Common
{/// <summary>
 /// Kết quả phân trang cho danh sách.
 /// </summary>
    public class PagedResultDto<T>
    {
        public IReadOnlyList<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }                  // tổng số bản ghi
        public int PageIndex { get; set; }                   // trang hiện tại
        public int PageSize { get; set; }                    // số dòng mỗi trang
        public int TotalRecords { get; internal set; }
    }
}
