namespace QLNS_BE.Models.Dtos.Auth
{
    public class ChangePasswordDto
    {
        /// <summary>
        /// Mật khẩu cũ
        /// </summary>
        public string OldPassword { get; set; } = string.Empty;

        /// <summary>
        /// Mật khẩu mới (tối thiểu 6 ký tự)
        /// </summary>
        public string NewPassword { get; set; } = string.Empty;
    }
}
