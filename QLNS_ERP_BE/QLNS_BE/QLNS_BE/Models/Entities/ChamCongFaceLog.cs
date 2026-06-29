using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS_BE.Models.Entities
{
    [Table("cham_cong_face_log")]
    public class ChamCongFaceLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("cham_cong_id")]
        public int? ChamCongId { get; set; }

        [Column("nv_ho_so_id")]
        public int? NvHoSoId { get; set; }

        [Required]
        [Column("thoi_gian")]
        public DateTime ThoiGian { get; set; }

        [Required]
        [Column("loai")]
        [MaxLength(10)]
        public string Loai { get; set; } = string.Empty; // VAO, RA

        [Column("face_image_url")]
        [MaxLength(500)]
        public string? FaceImageUrl { get; set; }

        [Column("confidence_score", TypeName = "decimal(5,4)")]
        public decimal? ConfidenceScore { get; set; }

        [Required]
        [Column("trang_thai")]
        [MaxLength(20)]
        public string TrangThai { get; set; } = "THANH_CONG"; // THANH_CONG, THAT_BAI, NGHI_NGO, DA_XU_LY

        [Column("ly_do_that_bai")]
        [MaxLength(500)]
        public string? LyDoThatBai { get; set; }

        [Column("ghi_chu")]
        [MaxLength(500)]
        public string? GhiChu { get; set; }

        [Column("ip_address")]
        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [Column("device_info")]
        [MaxLength(200)]
        public string? DeviceInfo { get; set; }

        [Column("location")]
        [MaxLength(200)]
        public string? Location { get; set; }

        [Column("processing_time_ms")]
        public int? ProcessingTimeMs { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("ChamCongId")]
        public virtual ChamCong? ChamCong { get; set; }

        [ForeignKey("NvHoSoId")]
        public virtual NvHoSo? NhanVien { get; set; }
    }
}
