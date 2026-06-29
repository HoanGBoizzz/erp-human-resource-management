namespace QLNS_BE.Models.Dtos.Auth
{
    public class LoginResponseDto
    {
        public string Username { get; set; } = null!;
        public string Role {  get; set; } = null!;
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public DateTime ExpiresAt {  get; set; }
        public int? EmployeeId { get; set; }
    }
}
