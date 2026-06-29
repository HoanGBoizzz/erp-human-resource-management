namespace QLNS_BE.Models.Dtos.PhongBan
{
    /// <summary>
    /// DTO để HR tạo yêu cầu điều chuyển
    /// </summary>
    public class TaoYeuCauDieuChuyenDto
    {
        public int NvCongViecId { get; set; }
        public int PhongBanMoiId { get; set; }
        public string? LyDo { get; set; }
    }

    /// <summary>
    /// DTO để Giám đốc duyệt yêu cầu
    /// </summary>
    public class DuyetYeuCauDieuChuyenDto
    {
        public int YeuCauId { get; set; }
        public bool Duyet { get; set; } // true=duyệt, false=từ chối
        public string? GhiChu { get; set; }
    }

    /// <summary>
    /// DTO hiển thị danh sách yêu cầu điều chuyển
    /// </summary>
    public class YeuCauDieuChuyenListDto
    {
        public int Id { get; set; }
        public string MaNhanVien { get; set; } = "";
        public string HoTenNhanVien { get; set; } = "";
        public string TenPhongBanCu { get; set; } = "";
        public string TenPhongBanMoi { get; set; } = "";
        public string? LyDo { get; set; }
        public int TrangThai { get; set; } // 0=Chờ duyệt, 1=Đã duyệt, 2=Từ chối
        public string TrangThaiText => TrangThai switch
        {
            0 => "Chờ duyệt",
            1 => "Đã duyệt",
            2 => "Từ chối",
            _ => "Không xác định"
        };
        public string TenNguoiTao { get; set; } = "";
        public DateTime NgayTao { get; set; }
        public string? TenNguoiDuyet { get; set; }
        public DateTime? NgayDuyet { get; set; }
        public string? GhiChuDuyet { get; set; }
    }
}
