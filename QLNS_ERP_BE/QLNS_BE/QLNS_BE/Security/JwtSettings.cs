namespace QLNS_BE.Security
{
    public class JwtSettings
    {
        public string Issuer { get; set; } = null!;
        public string Audience {  get; set; } = null!;
        public string SecretKey { get; set; } = null!;
        public int AccessTokenMinutes { get; set; } = 60;
        public int RefreshTokenDats { get; set; } = 7;
    }
}
