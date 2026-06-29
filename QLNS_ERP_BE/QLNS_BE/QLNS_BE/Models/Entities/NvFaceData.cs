using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS_BE.Models.Entities
{
    [Table("nv_face_data")]
    public class NvFaceData
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("nv_ho_so_id")]
        public int NvHoSoId { get; set; }

        [Required]
        [Column("face_encoding", TypeName = "TEXT")]
        public string FaceEncoding { get; set; } = string.Empty;

        [Column("face_image_url")]
        [MaxLength(500)]
        public string? FaceImageUrl { get; set; }

        [Column("face_image_thumbnail")]
        [MaxLength(500)]
        public string? FaceImageThumbnail { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("quality_score", TypeName = "decimal(5,4)")]
        public decimal? QualityScore { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [Column("updated_by")]
        public int? UpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("NvHoSoId")]
        public virtual NvHoSo? NhanVien { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual TaiKhoan? NguoiTao { get; set; }
    }
}
