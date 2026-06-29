namespace QLNS_BE.Models.Dtos.Admin.Account
{/// <summary>
 /// Dùng khi cập nhật vai trò / gán nhân viên / trạng thái tài khoản.
 /// Không dùng để đổi mật khẩu.
 /// </summary>
    public class AccountUpdateDto
    {
        public int VaiTroId { get; set; }                    // TAI_KHOAN.vai_tro_id
        public int? NvHoSoId { get; set; }                   // TAI_KHOAN.nv_ho_so_id
        public bool TrangThai { get; set; }                  // TAI_KHOAN.trang_thai
    }
}
