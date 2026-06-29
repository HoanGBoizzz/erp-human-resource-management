using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;
using QLNS_BE.Models.Dtos.Admin.Role;
using QLNS_BE.Models.Dtos.Common;
using QLNS_BE.Models.Entities;

namespace QLNS_BE.Services
{
    public class RoleService
    {
        private readonly AppDbContext _context;

        public RoleService(AppDbContext context)
        {
            _context = context;
        }

        // ============================
        // 1. DANH SÁCH + PHÂN TRANG
        // ============================
        public async Task<PagedResultDto<RoleListItemDto>> GetPagedAsync(PagingRequestDto request)
        {
            if (request.PageIndex <= 0) request.PageIndex = 1;
            if (request.PageSize <= 0) request.PageSize = 20;

            var query = _context.VaiTros.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(request.Keyword))
            {
                string kw = request.Keyword.ToLower();
                query = query.Where(x => x.MaVaiTro.ToLower().Contains(kw) ||
                                         x.TenVaiTro.ToLower().Contains(kw));
            }

            int totalRecords = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.MucDoUuTien) // Sắp xếp theo mức độ ưu tiên
                .ThenBy(x => x.Id)
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new RoleListItemDto
                {
                    Id = x.Id,
                    MaVaiTro = x.MaVaiTro,
                    TenVaiTro = x.TenVaiTro,
                    MoTa = x.MoTa,
                    MucDoUuTien = x.MucDoUuTien, // [Map field]
                    TrangThai = x.TrangThai      // [Map field]
                })
                .ToListAsync();

            return new PagedResultDto<RoleListItemDto>
            {
                Items = items,
                TotalRecords = totalRecords,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize
            };
        }

        // ============================
        // 2. CHI TIẾT
        // ============================
        public async Task<RoleListItemDto?> GetByIdAsync(int id)
        {
            var entity = await _context.VaiTros.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null) return null;

            return new RoleListItemDto
            {
                Id = entity.Id,
                MaVaiTro = entity.MaVaiTro,
                TenVaiTro = entity.TenVaiTro,
                MoTa = entity.MoTa,
                MucDoUuTien = entity.MucDoUuTien, // [Map field]
                TrangThai = entity.TrangThai      // [Map field]
            };
        }

        // ============================
        // 3. TẠO MỚI
        // ============================
        public async Task<RoleListItemDto> CreateAsync(RoleCreateDto dto)
        {
            bool exists = await _context.VaiTros.AnyAsync(x => x.MaVaiTro == dto.MaVaiTro);
            if (exists)
                throw new ArgumentException($"Mã vai trò '{dto.MaVaiTro}' đã tồn tại!");

            var entity = new VaiTro
            {
                MaVaiTro = dto.MaVaiTro,
                TenVaiTro = dto.TenVaiTro,
                MoTa = dto.MoTa,
                MucDoUuTien = dto.MucDoUuTien, // [Map field]
                TrangThai = dto.TrangThai      // [Map field]
            };

            _context.VaiTros.Add(entity);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(entity.Id) ?? new RoleListItemDto();
        }

        // ============================
        // 4. CẬP NHẬT
        // ============================
        public async Task<RoleListItemDto> UpdateAsync(int id, RoleUpdateDto dto)
        {
            var entity = await _context.VaiTros.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy vai trò!");

            if (entity.MaVaiTro != dto.MaVaiTro)
            {
                bool duplicate = await _context.VaiTros.AnyAsync(x => x.MaVaiTro == dto.MaVaiTro && x.Id != id);
                if (duplicate)
                    throw new ArgumentException($"Mã vai trò '{dto.MaVaiTro}' đã được sử dụng!");
            }

            entity.MaVaiTro = dto.MaVaiTro;
            entity.TenVaiTro = dto.TenVaiTro;
            entity.MoTa = dto.MoTa;
            entity.MucDoUuTien = dto.MucDoUuTien; // [Update field]
            entity.TrangThai = dto.TrangThai;     // [Update field]

            await _context.SaveChangesAsync();

            return (await GetByIdAsync(id))!;
        }

        // ============================
        // 5. XÓA
        // ============================
        public async Task DeleteAsync(int id)
        {
            var entity = await _context.VaiTros.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy vai trò!");

            // Kiểm tra ràng buộc
            bool isUsed = await _context.TaiKhoans.AnyAsync(x => x.VaiTroId == id);
            if (isUsed)
            {
                throw new InvalidOperationException("Không thể xóa vai trò đang được sử dụng bởi tài khoản.");
            }

            _context.VaiTros.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}