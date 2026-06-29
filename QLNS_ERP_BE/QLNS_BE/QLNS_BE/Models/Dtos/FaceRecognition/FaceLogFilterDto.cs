namespace QLNS_BE.Models.Dtos.FaceRecognition
{
    public class FaceLogFilterDto
    {
        public int? NvId { get; set; }
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }
        public string? Loai { get; set; } // VAO, RA
        public string? TrangThai { get; set; } // THANH_CONG, THAT_BAI, NGHI_NGO
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
