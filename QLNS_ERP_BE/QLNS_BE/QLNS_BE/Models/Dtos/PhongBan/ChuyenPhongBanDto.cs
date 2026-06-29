using System.ComponentModel.DataAnnotations;

namespace QLNS_BE.Models.Dtos.PhongBan
{
    /// <summary>
    /// DTO điều chuyển nhân viên sang phòng ban khác
    /// </summary>
    public class ChuyenPhongBanDto
    {
        [Required(ErrorMessage = "ID công việc nhân viên là bắt buộc")]
        public int NvCongViecId { get; set; }

        [Required(ErrorMessage = "Phòng ban mới là bắt buộc")]
        public int PhongBanMoiId { get; set; }
    }
}
