using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS_BE.Models.Entities
{
    [Table("face_recognition_config")]
    public class FaceRecognitionConfig
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("key_name")]
        [MaxLength(100)]
        public string KeyName { get; set; } = string.Empty;

        [Required]
        [Column("value")]
        [MaxLength(500)]
        public string Value { get; set; } = string.Empty;

        [Column("data_type")]
        [MaxLength(20)]
        public string DataType { get; set; } = "STRING"; // STRING, INT, DECIMAL, BOOLEAN, JSON

        [Column("description")]
        [MaxLength(500)]
        public string? Description { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [Column("updated_by")]
        public int? UpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("UpdatedBy")]
        public virtual TaiKhoan? NguoiCapNhat { get; set; }
    }
}
