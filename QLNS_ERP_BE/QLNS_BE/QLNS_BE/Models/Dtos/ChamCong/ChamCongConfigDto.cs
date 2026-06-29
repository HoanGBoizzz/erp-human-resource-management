namespace QLNS_BE.Models.Dtos.ChamCong
{
    public class ChamCongConfigDto
    {
        public string GioVao { get; set; } = "08:00";
        public string GioRa { get; set; } = "17:00";
        public int LateGraceMinutes { get; set; } = 15;
        public int EarlyLeaveGraceMinutes { get; set; } = 15;
    }
}
