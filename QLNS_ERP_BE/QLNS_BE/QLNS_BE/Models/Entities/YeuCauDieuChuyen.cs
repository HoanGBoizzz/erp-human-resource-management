using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS_BE.Models.Entities
{
    /// <summary>
    /// Yêu cầu điều chuyển phòng ban - cần Giám đốc duyệt
    /// </summary>
    [Table("YEU_CAU_DIEU_CHUYEN")]
    public class YeuCauDieuChuyen
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        /// <summary>
        /// ID record NvCongViec cần điều chuyển
        /// </summary>
        [Column("NV_CONG_VIEC_ID")]
        public int NvCongViecId { get; set; }

        /// <summary>
        /// Phòng ban hiện tại
        /// </summary>
        [Column("PHONG_BAN_CU_ID")]
        public int PhongBanCuId { get; set; }

        /// <summary>
        /// Phòng ban mới (đích)
        /// </summary>
        [Column("PHONG_BAN_MOI_ID")]
        public int PhongBanMoiId { get; set; }

        /// <summary>
        /// Lý do điều chuyển
        /// </summary>
        [Column("LY_DO")]
        [StringLength(500)]
        public string? LyDo { get; set; }

        /// <summary>
        /// Trạng thái: 0=Chờ duyệt, 1=Đã duyệt, 2=Từ chối
        /// </summary>
        [Column("TRANG_THAI")]
        public int TrangThai { get; set; } = 0;

        /// <summary>
        /// Ghi chú từ người duyệt
        /// </summary>
        [Column("GHI_CHU_DUYET")]
        [StringLength(500)]
        public string? GhiChuDuyet { get; set; }

        /// <summary>
        /// Tài khoản tạo yêu cầu (HR)
        /// </summary>
        [Column("TAI_KHOAN_TAO_ID")]
        public int TaiKhoanTaoId { get; set; }

        /// <summary>
        /// Tài khoản duyệt (Giám đốc)
        /// </summary>
        [Column("TAI_KHOAN_DUYET_ID")]
        public int? TaiKhoanDuyetId { get; set; }

        /// <summary>
        /// Ngày tạo yêu cầu
        /// </summary>
        [Column("NGAY_TAO")]
        public DateTime NgayTao { get; set; } = DateTime.Now;

        /// <summary>
        /// Ngày duyệt
        /// </summary>
        [Column("NGAY_DUYET")]
        public DateTime? NgayDuyet { get; set; }

        // ===== Navigation Properties =====
        [ForeignKey("NvCongViecId")]
        public virtual NvCongViec? NvCongViec { get; set; }

        [ForeignKey("PhongBanCuId")]
        public virtual PhongBan? PhongBanCu { get; set; }

        [ForeignKey("PhongBanMoiId")]
        public virtual PhongBan? PhongBanMoi { get; set; }

        [ForeignKey("TaiKhoanTaoId")]
        public virtual TaiKhoan? TaiKhoanTao { get; set; }

        [ForeignKey("TaiKhoanDuyetId")]
        public virtual TaiKhoan? TaiKhoanDuyet { get; set; }
    }
}
