using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;
using QLNS_BE.Models.Dtos.DeXuatGiamDoc;
using QLNS_BE.Models.Entities;

namespace QLNS_BE.Services
{
    public class DeXuatGiamDocService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DeXuatGiamDocService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // ─── 1. DANH SÁCH (HR xem của mình, GD xem tất cả) ───────────────────
        public async Task<List<DeXuatGiamDocListItemDto>> GetListAsync(int? taiKhoanTaoId = null)
        {
            var query = _context.DeXuatGiamDocs
                .Include(x => x.TaiKhoanTao)
                .AsQueryable();

            if (taiKhoanTaoId.HasValue)
                query = query.Where(x => x.TaiKhoanTaoId == taiKhoanTaoId.Value);

            var list = await query
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new DeXuatGiamDocListItemDto
                {
                    Id = x.Id,
                    TenDeXuat = x.TenDeXuat,
                    MoTa = x.MoTa,
                    NgayDeXuat = x.NgayDeXuat,
                    TrangThai = x.TrangThai,
                    TepTinUrl = x.TepTinUrl,
                    TepTinTenGoc = x.TepTinTenGoc,
                    TenNguoiTao = x.TaiKhoanTao.TenDangNhap,
                    CreatedAt = x.CreatedAt,
                    NgayGuiDuyet = x.NgayGuiDuyet,
                    NgayDuyet = x.NgayDuyet,
                })
                .ToListAsync();

            return list;
        }

        // ─── 2. CHI TIẾT ─────────────────────────────────────────────────────
        public async Task<DeXuatGiamDocDetailDto?> GetDetailAsync(int id)
        {
            var x = await _context.DeXuatGiamDocs
                .Include(d => d.TaiKhoanTao).ThenInclude(tk => tk.NvHoSo)
                .Include(d => d.TaiKhoanDuyet).ThenInclude(tk => tk!.NvHoSo)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (x == null) return null;

            string? tenNguoiTao = x.TaiKhoanTao.NvHoSo?.HoTen ?? x.TaiKhoanTao.TenDangNhap;
            string? tenNguoiDuyet = x.TaiKhoanDuyet?.NvHoSo?.HoTen ?? x.TaiKhoanDuyet?.TenDangNhap;

            return new DeXuatGiamDocDetailDto
            {
                Id = x.Id,
                TenDeXuat = x.TenDeXuat,
                MoTa = x.MoTa,
                NgayDeXuat = x.NgayDeXuat,
                TrangThai = x.TrangThai,
                TepTinUrl = x.TepTinUrl,
                TepTinTenGoc = x.TepTinTenGoc,
                TepTinMime = x.TepTinMime,
                TepTinSize = x.TepTinSize,
                TaiKhoanTaoId = x.TaiKhoanTaoId,
                TenNguoiTao = tenNguoiTao,
                TaiKhoanDuyetId = x.TaiKhoanDuyetId,
                TenNguoiDuyet = tenNguoiDuyet,
                NgayGuiDuyet = x.NgayGuiDuyet,
                NgayDuyet = x.NgayDuyet,
                LyDoTuChoi = x.LyDoTuChoi,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
            };
        }

        // ─── 3. TẠO MỚI ──────────────────────────────────────────────────────
        public async Task<int> CreateAsync(DeXuatGiamDocCreateDto dto, int userId)
        {
            var entity = new DeXuatGiamDoc
            {
                TenDeXuat = dto.TenDeXuat.Trim(),
                MoTa = dto.MoTa?.Trim(),
                NgayDeXuat = dto.NgayDeXuat,
                TrangThai = "NHAP",
                TaiKhoanTaoId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            _context.DeXuatGiamDocs.Add(entity);
            await _context.SaveChangesAsync();
            return entity.Id;
        }

        // ─── 4. CẬP NHẬT (chỉ NHAP hoặc DA_THU_HOI) ─────────────────────────
        public async Task<(bool ok, string msg)> UpdateAsync(int id, DeXuatGiamDocUpdateDto dto, int userId)
        {
            var entity = await _context.DeXuatGiamDocs.FindAsync(id);
            if (entity == null) return (false, "Không tìm thấy đề xuất");
            if (entity.TaiKhoanTaoId != userId) return (false, "Bạn không có quyền sửa đề xuất này");
            if (entity.TrangThai != "NHAP" && entity.TrangThai != "DA_THU_HOI")
                return (false, "Chỉ có thể sửa đề xuất ở trạng thái Nháp hoặc Đã thu hồi");

            entity.TenDeXuat = dto.TenDeXuat.Trim();
            entity.MoTa = dto.MoTa?.Trim();
            entity.NgayDeXuat = dto.NgayDeXuat;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return (true, "Cập nhật thành công");
        }

        // ─── 5. XÓA (chỉ NHAP hoặc DA_THU_HOI) ──────────────────────────────
        public async Task<(bool ok, string msg)> DeleteAsync(int id, int userId)
        {
            var entity = await _context.DeXuatGiamDocs.FindAsync(id);
            if (entity == null) return (false, "Không tìm thấy đề xuất");
            if (entity.TaiKhoanTaoId != userId) return (false, "Bạn không có quyền xóa đề xuất này");
            if (entity.TrangThai != "NHAP" && entity.TrangThai != "DA_THU_HOI")
                return (false, "Phải thu hồi yêu cầu trước khi xóa");

            // Xóa file vật lý nếu có
            if (!string.IsNullOrEmpty(entity.TepTinUrl))
            {
                var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var filePath = Path.Combine(webRoot, entity.TepTinUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(filePath)) File.Delete(filePath);
            }

            _context.DeXuatGiamDocs.Remove(entity);
            await _context.SaveChangesAsync();
            return (true, "Xóa thành công");
        }

        // ─── 6. GỬI DUYỆT (NHAP → CHO_DUYET) ────────────────────────────────
        public async Task<(bool ok, string msg)> GuiDuyetAsync(int id, int userId)
        {
            var entity = await _context.DeXuatGiamDocs.FindAsync(id);
            if (entity == null) return (false, "Không tìm thấy đề xuất");
            if (entity.TaiKhoanTaoId != userId) return (false, "Bạn không có quyền gửi duyệt đề xuất này");
            if (entity.TrangThai != "NHAP" && entity.TrangThai != "DA_THU_HOI")
                return (false, "Chỉ có thể gửi duyệt đề xuất ở trạng thái Nháp hoặc Đã thu hồi");

            entity.TrangThai = "CHO_DUYET";
            entity.NgayGuiDuyet = DateTime.UtcNow;
            entity.LyDoTuChoi = null;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return (true, "Đã gửi đề xuất để giám đốc duyệt");
        }

        // ─── 7. THU HỒI (CHO_DUYET → NHAP) ──────────────────────────────────
        public async Task<(bool ok, string msg)> ThuHoiAsync(int id, int userId)
        {
            var entity = await _context.DeXuatGiamDocs.FindAsync(id);
            if (entity == null) return (false, "Không tìm thấy đề xuất");
            if (entity.TaiKhoanTaoId != userId) return (false, "Bạn không có quyền thu hồi đề xuất này");
            if (entity.TrangThai != "CHO_DUYET")
                return (false, "Chỉ có thể thu hồi đề xuất đang ở trạng thái Chờ duyệt");

            entity.TrangThai = "NHAP";
            entity.NgayGuiDuyet = null;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return (true, "Đã thu hồi đề xuất thành công");
        }

        // ─── 8. DUYỆT / TỪ CHỐI (Giám đốc) ──────────────────────────────────
        public async Task<(bool ok, string msg)> DuyetAsync(int id, DeXuatGiamDocApproveDto dto, int userId)
        {
            var entity = await _context.DeXuatGiamDocs.FindAsync(id);
            if (entity == null) return (false, "Không tìm thấy đề xuất");
            if (entity.TrangThai != "CHO_DUYET")
                return (false, "Đề xuất không ở trạng thái chờ duyệt");

            if (!dto.DongY && string.IsNullOrWhiteSpace(dto.LyDoTuChoi))
                return (false, "Vui lòng nhập lý do từ chối");

            entity.TrangThai = dto.DongY ? "DA_DUYET" : "TU_CHOI";
            entity.TaiKhoanDuyetId = userId;
            entity.NgayDuyet = DateTime.UtcNow;
            entity.LyDoTuChoi = dto.DongY ? null : dto.LyDoTuChoi?.Trim();
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return (true, dto.DongY ? "Đã duyệt đề xuất" : "Đã từ chối đề xuất");
        }

        // ─── HELPER: Lấy id tài khoản Giám đốc ──────────────────────────────
        public async Task<List<int>> GetDirectorAccountIdsAsync()
        {
            return await _context.TaiKhoans
                .Include(x => x.VaiTro)
                .Where(x => x.VaiTro != null && x.VaiTro.MaVaiTro == "GIAM_DOC")
                .Select(x => x.Id)
                .ToListAsync();
        }

        // ─── 9. UPLOAD FILE ───────────────────────────────────────────────────
        public async Task<(bool ok, string msg, string? url, string? tenGoc)> UploadFileAsync(
            int id, IFormFile file, int userId)
        {
            var entity = await _context.DeXuatGiamDocs.FindAsync(id);
            if (entity == null) return (false, "Không tìm thấy đề xuất", null, null);
            if (entity.TaiKhoanTaoId != userId) return (false, "Không có quyền", null, null);
            if (entity.TrangThai != "NHAP" && entity.TrangThai != "DA_THU_HOI")
                return (false, "Chỉ upload file khi đề xuất ở trạng thái Nháp hoặc Đã thu hồi", null, null);

            const long maxSize = 10 * 1024 * 1024; // 10MB
            if (file.Length > maxSize) return (false, "File không được vượt quá 10MB", null, null);

            // Xóa file cũ nếu có
            if (!string.IsNullOrEmpty(entity.TepTinUrl))
            {
                var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var oldPath = Path.Combine(webRoot, entity.TepTinUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(oldPath)) File.Delete(oldPath);
            }

            // Lưu file mới
            var ext = Path.GetExtension(file.FileName);
            var fileName = $"dxgd_{id}_{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "de-xuat");
            Directory.CreateDirectory(uploadDir);
            var savePath = Path.Combine(uploadDir, fileName);

            using (var stream = new FileStream(savePath, FileMode.Create))
                await file.CopyToAsync(stream);

            var relativeUrl = $"/uploads/de-xuat/{fileName}";

            entity.TepTinUrl = relativeUrl;
            entity.TepTinTenGoc = file.FileName;
            entity.TepTinMime = file.ContentType;
            entity.TepTinSize = file.Length;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return (true, "Upload thành công", relativeUrl, file.FileName);
        }
    }
}
