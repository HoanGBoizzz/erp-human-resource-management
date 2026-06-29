namespace QLNS_BE.Models.Dtos.FaceRecognition
{
    public class ChamCongFaceLogDto
    {
        public int Id { get; set; }
        public int? NvHoSoId { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public DateTime ThoiGian { get; set; }
        public string Loai { get; set; } = string.Empty; // VAO, RA
        public string TrangThai { get; set; } = string.Empty; // THANH_CONG, THAT_BAI, NGHI_NGO
        public double? ConfidenceScore { get; set; }
        public string? IpAddress { get; set; }
        public string? DeviceInfo { get; set; }
    }
}
