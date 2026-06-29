using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;
using QLNS_BE.Models.Dtos.Admin.ThamSo;
using QLNS_BE.Models.Entities;

namespace QLNS_BE.Services
{
    public class ThamSoHeThongService
    {
        private readonly AppDbContext _context;

        public ThamSoHeThongService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ThamSoHeThongDto>> GetAllAsync()
        {
            return await _context.ThamSoHeThongs
                .OrderBy(x => x.MaThamSo)
                .Select(x => new ThamSoHeThongDto
                {
                    Id = x.Id,
                    MaThamSo = x.MaThamSo,
                    GiaTri = x.GiaTri,
                    MoTa = x.MoTa,
                    NgayBatDauHieuLuc = x.NgayBatDauHieuLuc,
                    NgayKetThucHieuLuc = x.NgayKetThucHieuLuc
                })
                .ToListAsync();
        }

        public async Task<ThamSoHeThongDto?> GetByIdAsync(int id)
        {
            var x = await _context.ThamSoHeThongs.FindAsync(id);
            if (x == null) return null;
            return new ThamSoHeThongDto
            {
                Id = x.Id,
                MaThamSo = x.MaThamSo,
                GiaTri = x.GiaTri,
                MoTa = x.MoTa,
                NgayBatDauHieuLuc = x.NgayBatDauHieuLuc,
                NgayKetThucHieuLuc = x.NgayKetThucHieuLuc
            };
        }

        public async Task<ThamSoHeThongDto> CreateAsync(ThamSoHeThongCreateDto dto)
        {
            if (await _context.ThamSoHeThongs.AnyAsync(x => x.MaThamSo == dto.MaThamSo))
                throw new InvalidOperationException($"M\u00e3 tham s\u1ed1 '{dto.MaThamSo}' đ\u00e3 t\u1ed3n t\u1ea1i.");

            var entity = new ThamSoHeThong
            {
                MaThamSo = dto.MaThamSo.ToUpper().Trim(),
                GiaTri = dto.GiaTri.Trim(),
                MoTa = dto.MoTa?.Trim(),
                NgayBatDauHieuLuc = dto.NgayBatDauHieuLuc,
                NgayKetThucHieuLuc = dto.NgayKetThucHieuLuc
            };
            _context.ThamSoHeThongs.Add(entity);
            await _context.SaveChangesAsync();
            return (await GetByIdAsync(entity.Id))!;
        }

        public async Task UpdateAsync(int id, ThamSoHeThongUpdateDto dto)
        {
            var entity = await _context.ThamSoHeThongs.FindAsync(id)
                ?? throw new KeyNotFoundException("Kh\u00f4ng t\u00ecm th\u1ea5y tham s\u1ed1.");

            entity.GiaTri = dto.GiaTri.Trim();
            entity.MoTa = dto.MoTa?.Trim();
            entity.NgayBatDauHieuLuc = dto.NgayBatDauHieuLuc;
            entity.NgayKetThucHieuLuc = dto.NgayKetThucHieuLuc;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.ThamSoHeThongs.FindAsync(id)
                ?? throw new KeyNotFoundException("Kh\u00f4ng t\u00ecm th\u1ea5y tham s\u1ed1.");
            _context.ThamSoHeThongs.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
