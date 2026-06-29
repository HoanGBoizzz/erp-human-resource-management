using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;
using QLNS_BE.Models.Dtos.DonPhep;
using QLNS_BE.Models.Entities;

namespace QLNS_BE.Services
{
    public class DonPhepService
    {
        private readonly AppDbContext _context;

        public DonPhepService(AppDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // LẤY DS ĐƠN PHÉP
        // ============================================================
        public async Task<List<DonPhepListItemDto>> GetListAsync()
        {
            return await _context.DonXinPheps
                .Include(x => x.NvHoSo)
                .Include(x => x.LoaiPhep)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new DonPhepListItemDto
                {
                    Id = x.Id,
                    NvHoSoId = x.NvHoSoId,
                    HoTen = x.NvHoSo.HoTen,
                    TenLoaiPhep = x.LoaiPhep.TenLoaiPhep,
                    TuNgay = x.TuNgay,
                    DenNgay = x.DenNgay,
                    SoNgay = x.SoNgay,
                    TrangThai = x.TrangThai
                })
                .ToListAsync();
        }

        // ============================================================
        // LẤY DS LOẠI PHÉP
        // ============================================================
        public async Task<List<LoaiPhepDto>> GetLoaiPhepsAsync()
        {
            return await _context.LoaiPheps
                .Where(x => x.TrangThai) // Chỉ lấy loại phép đang hoạt động
                .OrderBy(x => x.TenLoaiPhep)
                .Select(x => new LoaiPhepDto
                {
                    Id = x.Id,
                    MaLoaiPhep = x.MaLoaiPhep,
                    TenLoaiPhep = x.TenLoaiPhep
                })
                .ToListAsync();
        }
        // ============================================================
        // LẤY CHI TIẾT
        // ============================================================
        public async Task<DonPhepDetailDto?> GetDetailAsync(int id)
        {
            var item = await _context.DonXinPheps
                .Include(x => x.NvHoSo)
                .Include(x => x.LoaiPhep)
                .Include(x => x.NguoiDuyet)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (item == null) return null;

            // Lấy TaiKhoanId từ NvHoSoId (cho notification)
            var taiKhoanId = await _context.TaiKhoans
                .Where(tk => tk.NvHoSoId == item.NvHoSoId)
                .Select(tk => (int?)tk.Id)
                .FirstOrDefaultAsync();

            return new DonPhepDetailDto
            {
                Id = item.Id,
                NvHoSoId = item.NvHoSoId,
                HoTen = item.NvHoSo.HoTen,
                TaiKhoanId = taiKhoanId,
                LoaiPhepId = item.LoaiPhepId,
                TenLoaiPhep = item.LoaiPhep.TenLoaiPhep,
                TuNgay = item.TuNgay,
                DenNgay = item.DenNgay,
                SoNgay = item.SoNgay,
                LyDo = item.LyDo,
                TrangThai = item.TrangThai,
                NguoiDuyetId = item.NguoiDuyetId,
                TenNguoiDuyet = item.NguoiDuyet?.TenDangNhap,
                NgayDuyet = item.NgayDuyet,
                LyDoTuChoi = item.LyDoTuChoi
            };
        }
        // ============================================================
        // TẠO MỚI
        // ============================================================
        public async Task<int> CreateAsync(DonPhepCreateDto dto)
        {
            // Validate NvHoSoId tồn tại
            await ValidateNvHoSoExistsAsync(dto.NvHoSoId);
            
            // Validate LoaiPhepId tồn tại
            await ValidateLoaiPhepExistsAsync(dto.LoaiPhepId);
            
            // Validate ngày
            ValidateDateRange(dto.TuNgay, dto.DenNgay);
            
            // Kiểm tra trùng lịch nghỉ
            await EnsureNoOverlapAsync(dto.NvHoSoId, dto.TuNgay, dto.DenNgay);

            var soNgay = (decimal)(dto.DenNgay.Date - dto.TuNgay.Date).TotalDays + 1;

            var entity = new DonXinPhep
            {
                NvHoSoId = dto.NvHoSoId,
                LoaiPhepId = dto.LoaiPhepId,
                TuNgay = dto.TuNgay,
                DenNgay = dto.DenNgay,
                SoNgay = soNgay,
                LyDo = dto.LyDo,
                TrangThai = "CHO_DUYET",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.DonXinPheps.Add(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }
        // ============================================================
        // DUYỆT ĐƠN / TỪ CHỐI
        // ============================================================
        public async Task DuyetDonAsync(DuyetDonPhepRequestDto dto, int taiKhoanId)
        {
            var entity = await _context.DonXinPheps.FirstOrDefaultAsync(x => x.Id == dto.DonPhepId);
            if (entity == null) throw new Exception("Không tìm thấy đơn phép.");

            if (entity.TrangThai != "CHO_DUYET")
                throw new Exception("Đơn phép đã được xử lý trước đó.");

            entity.TrangThai = dto.ChapNhan ? "DA_DUYET" : "TU_CHOI";
            entity.NguoiDuyetId = taiKhoanId;
            entity.NgayDuyet = DateTime.UtcNow;
            entity.LyDoTuChoi = dto.ChapNhan ? null : dto.LyDoTuChoi;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }


        // ============================================================
        // ADD - VALIDATION HELPERS (EMPLOYEE UPDATE/DELETE)
        // ============================================================
        private const int MAX_LEAVE_DAYS = 60;                // giới hạn số ngày 1 đơn (tuỳ bạn chỉnh)
        private const int MAX_PAST_DAYS = 30;                 // cho phép tạo/sửa lùi tối đa (tuỳ bạn chỉnh)
        private const int MAX_FUTURE_DAYS = 365;              // cho phép tương lai tối đa

        // ADD
        private static string NormalizeLyDo(string? lyDo)
        {
            return (lyDo ?? string.Empty).Trim();
        }

        // ADD
        private static void ValidateDateRange(DateTime tuNgay, DateTime denNgay)
        {
            var tu = tuNgay.Date;
            var den = denNgay.Date;

            if (den < tu)
                throw new Exception("Đến ngày phải >= Từ ngày.");

            var soNgay = (den - tu).TotalDays + 1;
            if (soNgay <= 0) throw new Exception("Số ngày nghỉ không hợp lệ.");

            if (soNgay > MAX_LEAVE_DAYS)
                throw new Exception($"Một đơn không được vượt quá {MAX_LEAVE_DAYS} ngày.");

            var today = DateTime.UtcNow.Date;

            // chặn quá khứ quá xa (tránh backdate bừa)
            if (tu < today.AddDays(-MAX_PAST_DAYS))
                throw new Exception($"Ngày bắt đầu không được lùi quá {MAX_PAST_DAYS} ngày.");

            // chặn tương lai quá xa
            if (tu > today.AddDays(MAX_FUTURE_DAYS))
                throw new Exception($"Ngày bắt đầu không được vượt quá {MAX_FUTURE_DAYS} ngày trong tương lai.");
        }

        // ADD
        private async Task ValidateNvHoSoExistsAsync(int nvHoSoId)
        {
            if (nvHoSoId <= 0) throw new Exception("NvHoSoId không hợp lệ.");

            var exists = await _context.Set<NvHoSo>().AnyAsync(x => x.Id == nvHoSoId);
            if (!exists) throw new Exception("Không tìm thấy hồ sơ nhân viên.");
        }

        // ADD
        private async Task ValidateLoaiPhepExistsAsync(int loaiPhepId)
        {
            if (loaiPhepId <= 0) throw new Exception("LoaiPhepId không hợp lệ.");

            var exists = await _context.Set<LoaiPhep>().AnyAsync(x => x.Id == loaiPhepId);
            if (!exists) throw new Exception("Không tìm thấy loại phép.");
        }

        // ADD - chặn trùng khoảng thời gian (đơn đang chờ/đã duyệt), loại trừ đơn hiện tại khi update
        private async Task EnsureNoOverlapAsync(int nvHoSoId, DateTime tuNgay, DateTime denNgay, int? excludeDonId = null)
        {
            var tu = tuNgay.Date;
            var den = denNgay.Date;

            var query = _context.DonXinPheps.AsQueryable()
                .Where(x => x.NvHoSoId == nvHoSoId)
                .Where(x => x.TrangThai != "TU_CHOI")
                .Where(x => x.TuNgay.Date <= den && x.DenNgay.Date >= tu);

            if (excludeDonId.HasValue)
                query = query.Where(x => x.Id != excludeDonId.Value);

            var hasOverlap = await query.AnyAsync();
            if (hasOverlap)
                throw new Exception("Khoảng thời gian nghỉ bị trùng với một đơn khác (đang chờ duyệt/đã duyệt).");
        }

        // ============================================================
        // EMPLOYEE - SỬA ĐƠN (CHẶT HƠN)
        // ============================================================
        // CHANGE (chỉ trong phần ADD trước đó)
        public async Task UpdateByEmployeeAsync(int id, DonPhepEmployeeUpdateDto dto)
        {
            if (id <= 0) throw new Exception("Id đơn phép không hợp lệ.");

            // ADD - validate input chặt
            await ValidateNvHoSoExistsAsync(dto.NvHoSoId);
            await ValidateLoaiPhepExistsAsync(dto.LoaiPhepId);

            dto.LyDo = NormalizeLyDo(dto.LyDo);
            if (string.IsNullOrWhiteSpace(dto.LyDo))
                throw new Exception("Lý do không được để trống.");

            ValidateDateRange(dto.TuNgay, dto.DenNgay);
            await EnsureNoOverlapAsync(dto.NvHoSoId, dto.TuNgay, dto.DenNgay, excludeDonId: id);

            var entity = await _context.DonXinPheps.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null) throw new Exception("Không tìm thấy đơn phép.");

            // chỉ chủ đơn mới được sửa
            if (entity.NvHoSoId != dto.NvHoSoId)
                throw new Exception("Bạn không có quyền sửa đơn phép này.");

            // chỉ sửa khi chờ duyệt
            if (entity.TrangThai != "CHO_DUYET")
                throw new Exception("Chỉ được sửa khi đơn đang chờ duyệt.");

            // không cho sửa nếu đã có thông tin duyệt (phòng trường hợp trạng thái chưa sync)
            if (entity.NgayDuyet.HasValue || entity.NguoiDuyetId.HasValue)
                throw new Exception("Đơn phép đã được xử lý trước đó.");

            var soNgay = (decimal)(dto.DenNgay.Date - dto.TuNgay.Date).TotalDays + 1;

            entity.LoaiPhepId = dto.LoaiPhepId;
            entity.TuNgay = dto.TuNgay.Date;
            entity.DenNgay = dto.DenNgay.Date;
            entity.SoNgay = soNgay;
            entity.LyDo = dto.LyDo;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        // ============================================================
        // EMPLOYEE - XÓA ĐƠN (CHẶT HƠN)
        // ============================================================
        // CHANGE (chỉ trong phần ADD trước đó)
        public async Task DeleteByEmployeeAsync(int id, int nvHoSoId)
        {
            if (id <= 0) throw new Exception("Id đơn phép không hợp lệ.");
            await ValidateNvHoSoExistsAsync(nvHoSoId);

            var entity = await _context.DonXinPheps.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null) throw new Exception("Không tìm thấy đơn phép.");

            if (entity.NvHoSoId != nvHoSoId)
                throw new Exception("Bạn không có quyền xóa đơn phép này.");

            if (entity.TrangThai != "CHO_DUYET")
                throw new Exception("Chỉ được xóa khi đơn đang chờ duyệt.");

            // chặn xóa nếu đã có dấu vết duyệt
            if (entity.NgayDuyet.HasValue || entity.NguoiDuyetId.HasValue)
                throw new Exception("Đơn phép đã được xử lý trước đó.");

            // chặn xóa nếu ngày bắt đầu đã qua (tránh xóa đơn đã bắt đầu hiệu lực)
            var today = DateTime.UtcNow.Date;
            if (entity.TuNgay.Date < today)
                throw new Exception("Không được xóa đơn đã bắt đầu hoặc trong quá khứ.");

            _context.DonXinPheps.Remove(entity);
            await _context.SaveChangesAsync();
        }
        // ============================================================
        // NOTIFICATION HELPERS
        // ============================================================
        /// <summary>
        /// Lấy danh sách tài khoản HR để gửi thông báo
        /// </summary>
        public async Task<List<int>> GetHrAccountIdsAsync()
        {
            return await _context.TaiKhoans
                .Include(x => x.VaiTro)
                .Where(x => x.VaiTro != null && x.VaiTro.MaVaiTro == "HR_ACC")
                .Select(x => x.Id)
                .ToListAsync();
        }

    }
}
