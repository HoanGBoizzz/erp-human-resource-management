namespace QLNS_BE.Models.Dtos.Admin.Account
{/// <summary>
 /// Dùng cho màn danh sách tài khoản (Admin/Giám đốc).
 /// </summary>
    public class AccountListItemDto
    {
        public int Id { get; set; }                          // TAI_KHOAN.id
        public string TenDangNhap { get; set; } = null!;     // TAI_KHOAN.ten_dang_nhap

        public int VaiTroId { get; set; }                    // TAI_KHOAN.vai_tro_id
        public string MaVaiTro { get; set; } = null!;        // VAI_TRO.ma_vai_tro
        public string TenVaiTro { get; set; } = null!;       // VAI_TRO.ten_vai_tro

        public bool TrangThai { get; set; }                  // TAI_KHOAN.trang_thai
        public int? NvHoSoId { get; set; }                   // TAI_KHOAN.nv_ho_so_id
        public string? HoTen { get; set; }                   // NV_HO_SO.ho_ten
        public DateTime? LanDangNhapCuoi { get; set; }       // TAI_KHOAN.lan_dang_nhap_cuoi
    }
}
