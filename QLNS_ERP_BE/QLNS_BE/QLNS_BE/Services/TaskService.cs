using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;
using QLNS_BE.Models.Dtos.Task;
using QLNS_BE.Models.Entities;

namespace QLNS_BE.Services
{
    public class TaskService
    {
        private readonly AppDbContext _context;

        public TaskService(AppDbContext context)
        {
            _context = context;
        }

        // Tạo task mới (Trưởng phòng/Admin)
        public async Task<DuAnTask> CreateTaskAsync(int duAnId, TaskCreateDto dto, int nguoiGiaoId)
        {
            // Kiểm tra dự án tồn tại
            var duAn = await _context.DuAns.FindAsync(duAnId);
            if (duAn == null)
                throw new Exception("Không tìm thấy dự án");

            // Kiểm tra nhân viên tồn tại
            var nhanVien = await _context.NvHoSos.FindAsync(dto.NhanVienId);
            if (nhanVien == null)
                throw new Exception("Không tìm thấy nhân viên");

            var task = new DuAnTask
            {
                DuAnId = duAnId,
                TieuDe = dto.TieuDe,
                MoTa = dto.MoTa,
                NhanVienId = dto.NhanVienId,
                NguoiGiaoId = nguoiGiaoId,
                NgayBatDau = dto.NgayBatDau,
                NgayKetThuc = dto.NgayKetThuc,
                UuTien = dto.UuTien,
                TrangThai = "MOI",
                PhanTramHoanThanh = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.DuAnTasks.Add(task);
            await _context.SaveChangesAsync();

            return task;
        }

        // Cập nhật task (Nhân viên cập nhật tiến độ)
        public async Task<bool> UpdateTaskAsync(int taskId, TaskUpdateDto dto, int userId)
        {
            var task = await _context.DuAnTasks.FindAsync(taskId);
            if (task == null) return false;

            // Chỉ nhân viên được giao mới được update
            // (hoặc người giao, admin cũng được)
            // Để đơn giản, tạm chấp nhận update

            if (dto.TrangThai != null)
                task.TrangThai = dto.TrangThai;

            if (dto.PhanTramHoanThanh.HasValue)
                task.PhanTramHoanThanh = dto.PhanTramHoanThanh.Value;

            if (dto.GhiChu != null)
                task.GhiChu = dto.GhiChu;

            // Nếu hoàn thành 100%, tự động set NgayHoanThanh
            if (task.PhanTramHoanThanh == 100 && task.TrangThai == "HOAN_THANH")
            {
                task.NgayHoanThanh = DateTime.Now;
            }

            task.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return true;
        }

        // Danh sách task của tôi (nhân viên xem các task được giao cho mình)
        public async Task<List<TaskListItemDto>> GetMyTasksAsync(int nhanVienId)
        {
            var tasks = await _context.DuAnTasks
                .Include(t => t.DuAn)
                .Include(t => t.NhanVien)
                .Include(t => t.NguoiGiao)
                .Where(t => t.NhanVienId == nhanVienId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TaskListItemDto
                {
                    Id = t.Id,
                    DuAnId = t.DuAnId,
                    DuAnTen = t.DuAn.TenDuAn,
                    TieuDe = t.TieuDe,
                    MoTa = t.MoTa,
                    NhanVienId = t.NhanVienId,
                    NhanVienTen = t.NhanVien.HoTen,
                    NguoiGiaoId = t.NguoiGiaoId,
                    NguoiGiaoTen = t.NguoiGiao.HoTen,
                    NgayBatDau = t.NgayBatDau,
                    NgayKetThuc = t.NgayKetThuc,
                    UuTien = t.UuTien,
                    TrangThai = t.TrangThai,
                    PhanTramHoanThanh = t.PhanTramHoanThanh,
                    GhiChu = t.GhiChu,
                    NgayHoanThanh = t.NgayHoanThanh
                })
                .ToListAsync();

            return tasks;
        }

        // Danh sách task của dự án (Trưởng phòng xem tất cả task trong dự án)
        public async Task<List<TaskListItemDto>> GetTasksByProjectAsync(int duAnId)
        {
            var tasks = await _context.DuAnTasks
                .Include(t => t.DuAn)
                .Include(t => t.NhanVien)
                .Include(t => t.NguoiGiao)
                .Where(t => t.DuAnId == duAnId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TaskListItemDto
                {
                    Id = t.Id,
                    DuAnId = t.DuAnId,
                    DuAnTen = t.DuAn.TenDuAn,
                    TieuDe = t.TieuDe,
                    MoTa = t.MoTa,
                    NhanVienId = t.NhanVienId,
                    NhanVienTen = t.NhanVien.HoTen,
                    NguoiGiaoId = t.NguoiGiaoId,
                    NguoiGiaoTen = t.NguoiGiao.HoTen,
                    NgayBatDau = t.NgayBatDau,
                    NgayKetThuc = t.NgayKetThuc,
                    UuTien = t.UuTien,
                    TrangThai = t.TrangThai,
                    PhanTramHoanThanh = t.PhanTramHoanThanh,
                    GhiChu = t.GhiChu,
                    NgayHoanThanh = t.NgayHoanThanh
                })
                .ToListAsync();

            return tasks;
        }
    }
}
