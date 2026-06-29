namespace QLNS_BE.Models.Dtos.ChamCong
{
    public class ChamCongPagedResponseDto
    {
        public List<ChamCongChiTietDto> Items { get; set; } = new();
        public int TotalRecords { get; set; }           // Tổng số bản ghi
        public int TotalPages { get; set; }             // Tổng số trang
        public int CurrentPage { get; set; }            // Trang hiện tại
        public int PageSize { get; set; }
    }
}
