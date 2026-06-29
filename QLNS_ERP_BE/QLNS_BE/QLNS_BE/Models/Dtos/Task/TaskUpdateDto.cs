namespace QLNS_BE.Models.Dtos.Task
{
    public class TaskUpdateDto
    {
        public string? TrangThai { get; set; }  // MOI, DANG_LAM, CHO_REVIEW, HOAN_THANH, HUY
        public int? PhanTramHoanThanh { get; set; }
        public string? GhiChu { get; set; }
    }
}
