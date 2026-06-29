namespace QLNS_BE.Models.Dtos.PhongBan
{
    /// <summary>
    /// DTO chi tiết phòng ban kèm danh sách nhân viên
    /// </summary>
    public class PhongBanDetailDto
    {
        public int Id { get; set; }
        public string MaPhongBan { get; set; } = null!;
        public string TenPhongBan { get; set; } = null!;
        public int? PhongBanChaId { get; set; }
        public string? TenPhongBanCha { get; set; }
        public bool TrangThai { get; set; }
        public string? GhiChu { get; set; }
        public List<NhanVienTrongPhongBanDto> DanhSachNhanVien { get; set; } = new();
    }

    /// <summary>
    /// DTO nhân viên trong phòng ban (dùng cho danh sách chi tiết)
    /// </summary>
    public class NhanVienTrongPhongBanDto
    {
        public int NvHoSoId { get; set; }
        public int NvCongViecId { get; set; }
        public string MaNhanVien { get; set; } = null!;
        public string HoTen { get; set; } = null!;
        public string? TenChucVu { get; set; }
        public byte TrangThaiLamViec { get; set; }  // 1 = Đang làm, 0 = Nghỉ việc
        public DateTime NgayVaoLam { get; set; }
    }
}
