using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;
using QLNS_BE.Models.Dtos.Luong;
using QLNS_BE.Models.Entities;

namespace QLNS_BE.Services
{
    public class PhuCapService
    {
        private readonly AppDbContext _context;

        public PhuCapService(AppDbContext context) => _context = context;

        // ─── PHỤ CẤP LOẠI ────────────────────────────────────────────────

        public async Task<List<PhuCapLoaiDto>> GetAllLoaiAsync()
        {
            return await _context.PhuCapLoais
                .OrderBy(x => x.ThuTu).ThenBy(x => x.TenPhuCap)
                .Select(x => new PhuCapLoaiDto
                {
                    Id = x.Id, TenPhuCap = x.TenPhuCap, MoTa = x.MoTa,
                    LaCoDinh = x.LaCoDinh, DonVi = x.DonVi,
                    ThuTu = x.ThuTu, DangHoatDong = x.DangHoatDong
                }).ToListAsync();
        }

        public async Task<PhuCapLoaiDto> CreateLoaiAsync(PhuCapLoaiCreateDto dto)
        {
            var entity = new PhuCapLoai
            {
                TenPhuCap = dto.TenPhuCap, MoTa = dto.MoTa,
                LaCoDinh = dto.LaCoDinh, DonVi = dto.DonVi,
                ThuTu = dto.ThuTu, DangHoatDong = true
            };
            _context.PhuCapLoais.Add(entity);
            await _context.SaveChangesAsync();
            return new PhuCapLoaiDto
            {
                Id = entity.Id, TenPhuCap = entity.TenPhuCap, MoTa = entity.MoTa,
                LaCoDinh = entity.LaCoDinh, DonVi = entity.DonVi,
                ThuTu = entity.ThuTu, DangHoatDong = entity.DangHoatDong
            };
        }

        public async Task<bool> UpdateLoaiAsync(int id, PhuCapLoaiCreateDto dto)
        {
            var e = await _context.PhuCapLoais.FindAsync(id);
            if (e == null) return false;
            e.TenPhuCap = dto.TenPhuCap; e.MoTa = dto.MoTa;
            e.LaCoDinh = dto.LaCoDinh; e.DonVi = dto.DonVi; e.ThuTu = dto.ThuTu;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleLoaiAsync(int id)
        {
            var e = await _context.PhuCapLoais.FindAsync(id);
            if (e == null) return false;
            e.DangHoatDong = !e.DangHoatDong;
            await _context.SaveChangesAsync();
            return true;
        }

        // ─── PHỤ CẤP NHÂN VIÊN ──────────────────────────────────────────

        public async Task<List<NvPhuCapDto>> GetByNvAsync(int nvHoSoId)
        {
            return await _context.NvPhuCaps
                .Include(x => x.PhuCapLoai)
                .Include(x => x.NvHoSo)
                .Where(x => x.NvHoSoId == nvHoSoId)
                .OrderByDescending(x => x.DangApDung).ThenByDescending(x => x.NgayBatDau)
                .Select(x => new NvPhuCapDto
                {
                    Id = x.Id, NvHoSoId = x.NvHoSoId, HoTen = x.NvHoSo.HoTen,
                    PhuCapLoaiId = x.PhuCapLoaiId, TenPhuCap = x.PhuCapLoai.TenPhuCap,
                    SoTien = x.SoTien, NgayBatDau = x.NgayBatDau,
                    NgayKetThuc = x.NgayKetThuc, DangApDung = x.DangApDung,
                    GhiChu = x.GhiChu, CreatedAt = x.CreatedAt
                }).ToListAsync();
        }

        public async Task<List<NvPhuCapDto>> GetAllAsync()
        {
            return await _context.NvPhuCaps
                .Include(x => x.PhuCapLoai)
                .Include(x => x.NvHoSo)
                .OrderBy(x => x.NvHoSo.HoTen)
                .ThenBy(x => x.PhuCapLoai.ThuTu)
                .Select(x => new NvPhuCapDto
                {
                    Id = x.Id, NvHoSoId = x.NvHoSoId, HoTen = x.NvHoSo.HoTen,
                    PhuCapLoaiId = x.PhuCapLoaiId, TenPhuCap = x.PhuCapLoai.TenPhuCap,
                    SoTien = x.SoTien, NgayBatDau = x.NgayBatDau,
                    NgayKetThuc = x.NgayKetThuc, DangApDung = x.DangApDung,
                    GhiChu = x.GhiChu, CreatedAt = x.CreatedAt
                }).ToListAsync();
        }

        public async Task<NvPhuCapDto> CreateAsync(NvPhuCapCreateDto dto, int taiKhoanTaoId)
        {
            var entity = new NvPhuCap
            {
                NvHoSoId = dto.NvHoSoId, PhuCapLoaiId = dto.PhuCapLoaiId,
                SoTien = dto.SoTien, NgayBatDau = dto.NgayBatDau,
                NgayKetThuc = dto.NgayKetThuc, DangApDung = true,
                GhiChu = dto.GhiChu, TaiKhoanTaoId = taiKhoanTaoId
            };
            _context.NvPhuCaps.Add(entity);
            await _context.SaveChangesAsync();

            var saved = await _context.NvPhuCaps
                .Include(x => x.PhuCapLoai).Include(x => x.NvHoSo)
                .FirstAsync(x => x.Id == entity.Id);
            return new NvPhuCapDto
            {
                Id = saved.Id, NvHoSoId = saved.NvHoSoId, HoTen = saved.NvHoSo.HoTen,
                PhuCapLoaiId = saved.PhuCapLoaiId, TenPhuCap = saved.PhuCapLoai.TenPhuCap,
                SoTien = saved.SoTien, NgayBatDau = saved.NgayBatDau,
                NgayKetThuc = saved.NgayKetThuc, DangApDung = saved.DangApDung,
                GhiChu = saved.GhiChu, CreatedAt = saved.CreatedAt
            };
        }

        public async Task<bool> UpdateAsync(int id, NvPhuCapUpdateDto dto)
        {
            var e = await _context.NvPhuCaps.FindAsync(id);
            if (e == null) return false;
            e.SoTien = dto.SoTien; e.NgayBatDau = dto.NgayBatDau;
            e.NgayKetThuc = dto.NgayKetThuc; e.DangApDung = dto.DangApDung;
            e.GhiChu = dto.GhiChu;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var e = await _context.NvPhuCaps.FindAsync(id);
            if (e == null) return false;
            _context.NvPhuCaps.Remove(e);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
