namespace QLNS_BE.Models.Dtos.Common
{/// <summary>
 /// Dùng khi lọc theo khoảng ngày (VD: lọc công, lương, dự án).
 /// </summary>
    public class DateRangeFilterDto
    {
        public DateTime? FromDate { get; set; }              // ngày bắt đầu (bao gồm)
        public DateTime? ToDate { get; set; }                // ngày kết thúc (bao gồm)
    }
}
