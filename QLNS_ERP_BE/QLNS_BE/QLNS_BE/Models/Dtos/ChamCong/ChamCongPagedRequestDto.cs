namespace QLNS_BE.Models.Dtos.ChamCong
{
    public class ChamCongPagedRequestDto
    {
        public int BangCongThangId { get; set; }        // ID bảng công tháng
        public int PageIndex { get; set; } = 1;         // Trang hiện tại (bắt đầu từ 1)
        public int PageSize { get; set; } = 20;         // Số bản ghi/trang
        public string? Keyword { get; set; }            // Tìm theo tên/mã NV
        public string? TrangThai { get; set; }
    }
}
