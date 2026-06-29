namespace QLNS_BE.Models.Dtos.Admin.Account
{/// <summary>
 /// Dùng khi Admin reset mật khẩu cho tài khoản.
 /// </summary>
    public class ResetPasswordRequestDto
    {
        public string NewPassword { get; set; } = null!;
    }
}
