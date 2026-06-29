namespace QLNS_BE.Models.Dtos.Admin.Account
{/// <summary>
 /// Dùng khi tạo mới tài khoản đăng nhập.
 /// </summary>
    public class AccountCreateDto
    {
        public string TenDangNhap { get; set; } = null!;     // TAI_KHOAN.ten_dang_nhap
        public string MatKhau { get; set; } = null!;         // raw password - sẽ hash vào mat_khau_hash + mat_khau_salt
        public int VaiTroId { get; set; }                    // FK VAI_TRO.id
        public int? NvHoSoId { get; set; }                   // FK NV_HO_SO.id
        public bool TrangThai { get; set; } = true;          // TAI_KHOAN.trang_thai
    }
}
