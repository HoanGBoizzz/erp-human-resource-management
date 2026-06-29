using QLNS_BE.Models.Dtos.Admin.Role;

namespace QLNS_BE.Models.Dtos
{
    public class PageResultDto<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
}