using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;
using QLNS_BE.Models.Dtos.NoiLamViec;
using QLNS_BE.Models.Entities;

namespace QLNS_BE.Services
{
    public class NoiLamViecService
    {
        private readonly AppDbContext _context;
        private readonly ThongBaoService _thongBaoService;

        public NoiLamViecService(AppDbContext context, ThongBaoService thongBaoService)
        {
            _context = context;
            _thongBaoService = thongBaoService;
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────
        private async Task<int?> GetNvHoSoIdAsync(int taiKhoanId)
        {
            return await _context.TaiKhoans
                .Where(x => x.Id == taiKhoanId)
                .Select(x => x.NvHoSoId)
                .FirstOrDefaultAsync();
        }

        /// <summary>Lấy danh sách ID tài khoản HR_ACC để gửi thông báo</summary>
        private async Task<List<int>> GetHrAccountIdsAsync()
        {
            return await _context.TaiKhoans
                .Include(x => x.VaiTro)
                .Where(x => x.VaiTro != null && x.VaiTro.MaVaiTro == "HR_ACC")
                .Select(x => x.Id)
                .ToListAsync();
        }

        /// <summary>Lấy taiKhoanId của nhân viên từ NvHoSoId</summary>
        private async Task<int?> GetTaiKhoanIdByNvHoSoIdAsync(int nvHoSoId)
        {
            return await _context.TaiKhoans
                .Where(x => x.NvHoSoId == nvHoSoId)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();
        }

        // ===========================================================
        // PHIẾU ĐỀ XUẤT DỤNG CỤ
        // ===========================================================

        public async Task<List<PhieuDeXuatListItemDto>> GetDeXuatListAsync(int taiKhoanId, bool? isHr = false)
        {
            var query = _context.PhieuDeXuatDungCus.AsQueryable();

            if (!isHr.GetValueOrDefault())
            {
                var nvId = await GetNvHoSoIdAsync(taiKhoanId);
                query = query.Where(x => x.NvHoSoId == nvId);
            }

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new PhieuDeXuatListItemDto
                {
                    Id = x.Id,
                    NvHoSoId = x.NvHoSoId,
                    MaNhanVien = x.NvHoSo.MaNhanVien,
                    HoTenNhanVien = x.NvHoSo.HoTen,
                    TenDungCu = x.TenDungCu,
                    DonViTinh = x.DonViTinh,
                    SoLuong = x.SoLuong,
                    GiaTien = x.GiaTien,
                    TongTien = x.TongTien,
                    LyDo = x.LyDo,
                    TrangThai = x.TrangThai,
                    LyDoTuChoi = x.LyDoTuChoi,
                    HoTenNguoiDuyet = x.NguoiDuyet != null ? x.NguoiDuyet.TenDangNhap : null,
                    NgayDuyet = x.NgayDuyet,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<int> CreateDeXuatAsync(int taiKhoanId, CreatePhieuDeXuatDto dto)
        {
            var nvId = await GetNvHoSoIdAsync(taiKhoanId)
                ?? throw new InvalidOperationException("Không tìm thấy hồ sơ nhân viên.");

            var entity = new PhieuDeXuatDungCu
            {
                NvHoSoId = nvId,
                TenDungCu = dto.TenDungCu,
                DonViTinh = dto.DonViTinh,
                SoLuong = dto.SoLuong,
                GiaTien = dto.GiaTien,
                TongTien = dto.GiaTien * dto.SoLuong,
                LyDo = dto.LyDo,
                TrangThai = "CHO_DUYET",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PhieuDeXuatDungCus.Add(entity);
            await _context.SaveChangesAsync();

            // Notify HR: nhân viên vừa gửi phiếu đề xuất
            try
            {
                var hoTen = await _context.NvHoSos
                    .Where(x => x.Id == nvId).Select(x => x.HoTen).FirstOrDefaultAsync();
                var hrIds = await GetHrAccountIdsAsync();
                foreach (var hrId in hrIds)
                    await _thongBaoService.CreateAndPushAsync(
                        hrId,
                        "Phiếu đề xuất mới",
                        $"{hoTen} vừa gửi phiếu đề xuất dụng cụ '{dto.TenDungCu}' cần duyệt.",
                        "YEU_CAU",
                        "PhieuDeXuat",
                        entity.Id,
                        "/hr/yeu-cau-noi-lam-viec",
                        taiKhoanId);
            }
            catch { /* log later */ }

            // Realtime: cập nhật danh sách HR
            await _thongBaoService.BroadcastEntityUpdateAsync("PhieuDeXuat", entity.Id, "CREATED");

            return entity.Id;
        }

        public async Task DuyetDeXuatAsync(DuyetPhieuDeXuatDto dto, int taiKhoanId)
        {
            var entity = await _context.PhieuDeXuatDungCus
                .Include(x => x.NvHoSo)
                .FirstOrDefaultAsync(x => x.Id == dto.PhieuId)
                ?? throw new KeyNotFoundException("Không tìm thấy phiếu.");

            entity.TrangThai = dto.ChapNhan ? "DA_DUYET" : "TU_CHOI";
            entity.NguoiDuyetId = taiKhoanId;
            entity.NgayDuyet = DateTime.UtcNow;
            entity.LyDoTuChoi = dto.ChapNhan ? null : dto.LyDoTuChoi;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Notify employee
            try
            {
                var nvTkId = await GetTaiKhoanIdByNvHoSoIdAsync(entity.NvHoSoId);
                if (nvTkId.HasValue)
                {
                    var title = dto.ChapNhan ? "Đơn đề xuất được duyệt" : "Đơn đề xuất bị từ chối";
                    var msg = dto.ChapNhan
                        ? $"Phiếu đề xuất dụng cụ '{entity.TenDungCu}' của bạn đã được phê duyệt."
                        : $"Phiếu đề xuất dụng cụ '{entity.TenDungCu}' bị từ chối. Lý do: {dto.LyDoTuChoi}";
                    await _thongBaoService.CreateAndPushAsync(
                        nvTkId.Value, title, msg,
                        dto.ChapNhan ? "SUCCESS" : "WARNING",
                        "PhieuDeXuat", entity.Id,
                        "/employee/phieu-de-xuat",
                        taiKhoanId);
                }
            }
            catch { /* log later */ }

            // Realtime: cập nhật danh sách employee
            await _thongBaoService.BroadcastEntityUpdateAsync("PhieuDeXuat", entity.Id,
                dto.ChapNhan ? "APPROVED" : "REJECTED");
        }

        public async Task DeleteDeXuatAsync(int id, int taiKhoanId)
        {
            var nvId = await GetNvHoSoIdAsync(taiKhoanId);
            var entity = await _context.PhieuDeXuatDungCus
                .FirstOrDefaultAsync(x => x.Id == id && x.NvHoSoId == nvId)
                ?? throw new KeyNotFoundException("Không tìm thấy phiếu.");

            if (entity.TrangThai != "CHO_DUYET")
                throw new InvalidOperationException("Chỉ được xóa phiếu đang chờ duyệt.");

            _context.PhieuDeXuatDungCus.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateDeXuatAsync(int id, int taiKhoanId, UpdatePhieuDeXuatDto dto)
        {
            var nvId = await GetNvHoSoIdAsync(taiKhoanId);
            var entity = await _context.PhieuDeXuatDungCus
                .FirstOrDefaultAsync(x => x.Id == id && x.NvHoSoId == nvId)
                ?? throw new KeyNotFoundException("Không tìm thấy phiếu.");

            if (entity.TrangThai != "CHO_DUYET")
                throw new InvalidOperationException("Chỉ được sửa phiếu đang chờ duyệt.");

            entity.TenDungCu = dto.TenDungCu;
            entity.DonViTinh = dto.DonViTinh;
            entity.SoLuong = dto.SoLuong;
            entity.GiaTien = dto.GiaTien;
            entity.TongTien = dto.GiaTien * dto.SoLuong;
            entity.LyDo = dto.LyDo;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        // ===========================================================
        // PHIẾU TẠM ỨNG
        // ===========================================================

        public async Task<List<PhieuTamUngListItemDto>> GetTamUngListAsync(int taiKhoanId, bool? isHr = false)
        {
            var query = _context.PhieuTamUngs.AsQueryable();

            if (!isHr.GetValueOrDefault())
            {
                var nvId = await GetNvHoSoIdAsync(taiKhoanId);
                query = query.Where(x => x.NvHoSoId == nvId);
            }

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new PhieuTamUngListItemDto
                {
                    Id = x.Id,
                    NvHoSoId = x.NvHoSoId,
                    MaNhanVien = x.NvHoSo.MaNhanVien,
                    HoTenNhanVien = x.NvHoSo.HoTen,
                    MucDich = x.MucDich,
                    SoTien = x.SoTien,
                    NgayCanTamUng = x.NgayCanTamUng,
                    LyDo = x.LyDo,
                    TrangThai = x.TrangThai,
                    LyDoTuChoi = x.LyDoTuChoi,
                    HoTenNguoiDuyet = x.NguoiDuyet != null ? x.NguoiDuyet.TenDangNhap : null,
                    NgayDuyet = x.NgayDuyet,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<int> CreateTamUngAsync(int taiKhoanId, CreatePhieuTamUngDto dto)
        {
            var nvId = await GetNvHoSoIdAsync(taiKhoanId)
                ?? throw new InvalidOperationException("Không tìm thấy hồ sơ nhân viên.");

            var entity = new PhieuTamUng
            {
                NvHoSoId = nvId,
                MucDich = dto.MucDich,
                SoTien = dto.SoTien,
                NgayCanTamUng = dto.NgayCanTamUng,
                LyDo = dto.LyDo,
                TrangThai = "CHO_DUYET",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PhieuTamUngs.Add(entity);
            await _context.SaveChangesAsync();

            // Notify HR
            try
            {
                var hoTen = await _context.NvHoSos
                    .Where(x => x.Id == nvId).Select(x => x.HoTen).FirstOrDefaultAsync();
                var hrIds = await GetHrAccountIdsAsync();
                foreach (var hrId in hrIds)
                    await _thongBaoService.CreateAndPushAsync(
                        hrId,
                        "Phiếu tạm ứng mới",
                        $"{hoTen} vừa gửi phiếu tạm ứng '{dto.MucDich}' cần duyệt.",
                        "YEU_CAU",
                        "PhieuTamUng",
                        entity.Id,
                        "/hr/yeu-cau-noi-lam-viec",
                        taiKhoanId);
            }
            catch { }

            // Realtime: cập nhật danh sách HR
            await _thongBaoService.BroadcastEntityUpdateAsync("PhieuTamUng", entity.Id, "CREATED");

            return entity.Id;
        }

        public async Task DuyetTamUngAsync(DuyetPhieuTamUngDto dto, int taiKhoanId)
        {
            var entity = await _context.PhieuTamUngs
                .Include(x => x.NvHoSo)
                .FirstOrDefaultAsync(x => x.Id == dto.PhieuId)
                ?? throw new KeyNotFoundException("Không tìm thấy phiếu.");

            entity.TrangThai = dto.ChapNhan ? "DA_DUYET" : "TU_CHOI";
            entity.NguoiDuyetId = taiKhoanId;
            entity.NgayDuyet = DateTime.UtcNow;
            entity.LyDoTuChoi = dto.ChapNhan ? null : dto.LyDoTuChoi;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Notify employee
            try
            {
                var nvTkId = await GetTaiKhoanIdByNvHoSoIdAsync(entity.NvHoSoId);
                if (nvTkId.HasValue)
                {
                    var title = dto.ChapNhan ? "Tạm ứng được phê duyệt" : "Tạm ứng bị từ chối";
                    var msg = dto.ChapNhan
                        ? $"Phiếu tạm ứng '{entity.MucDich}' của bạn đã được phê duyệt."
                        : $"Phiếu tạm ứng '{entity.MucDich}' bị từ chối. Lý do: {dto.LyDoTuChoi}";
                    await _thongBaoService.CreateAndPushAsync(
                        nvTkId.Value, title, msg,
                        dto.ChapNhan ? "SUCCESS" : "WARNING",
                        "PhieuTamUng", entity.Id,
                        "/employee/phieu-tam-ung",
                        taiKhoanId);
                }
            }
            catch { }

            // Realtime: cập nhật danh sách employee
            await _thongBaoService.BroadcastEntityUpdateAsync("PhieuTamUng", entity.Id,
                dto.ChapNhan ? "APPROVED" : "REJECTED");
        }

        public async Task DeleteTamUngAsync(int id, int taiKhoanId)
        {
            var nvId = await GetNvHoSoIdAsync(taiKhoanId);
            var entity = await _context.PhieuTamUngs
                .FirstOrDefaultAsync(x => x.Id == id && x.NvHoSoId == nvId)
                ?? throw new KeyNotFoundException("Không tìm thấy phiếu.");

            if (entity.TrangThai != "CHO_DUYET")
                throw new InvalidOperationException("Chỉ được xóa phiếu đang chờ duyệt.");

            _context.PhieuTamUngs.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateTamUngAsync(int id, int taiKhoanId, UpdatePhieuTamUngDto dto)
        {
            var nvId = await GetNvHoSoIdAsync(taiKhoanId);
            var entity = await _context.PhieuTamUngs
                .FirstOrDefaultAsync(x => x.Id == id && x.NvHoSoId == nvId)
                ?? throw new KeyNotFoundException("Không tìm thấy phiếu.");

            if (entity.TrangThai != "CHO_DUYET")
                throw new InvalidOperationException("Chỉ được sửa phiếu đang chờ duyệt.");

            entity.MucDich = dto.MucDich;
            entity.SoTien = dto.SoTien;
            entity.NgayCanTamUng = dto.NgayCanTamUng;
            entity.LyDo = dto.LyDo;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        // ===========================================================
        // ĐƠN ĐI MUỘN / VỀ SỚM
        // ===========================================================

        private static string GetTenLoai(string loai) => loai switch
        {
            "DI_MUON" => "Đi muộn",
            "VE_SOM" => "Về sớm",
            "CA_HAI" => "Cả hai",
            _ => loai
        };

        public async Task<List<DonDiMuonListItemDto>> GetDiMuonListAsync(int taiKhoanId, bool? isHr = false)
        {
            var query = _context.DonDiMuons.AsQueryable();

            if (!isHr.GetValueOrDefault())
            {
                var nvId = await GetNvHoSoIdAsync(taiKhoanId);
                query = query.Where(x => x.NvHoSoId == nvId);
            }

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new DonDiMuonListItemDto
                {
                    Id = x.Id,
                    NvHoSoId = x.NvHoSoId,
                    MaNhanVien = x.NvHoSo.MaNhanVien,
                    HoTenNhanVien = x.NvHoSo.HoTen,
                    Loai = x.Loai,
                    TenLoai = x.Loai == "DI_MUON" ? "Đi muộn" : x.Loai == "VE_SOM" ? "Về sớm" : "Cả hai",
                    NgayApDung = x.NgayApDung,
                    ThoiGianBatDau = x.ThoiGianBatDau.ToString(@"hh\:mm"),
                    ThoiGianKetThuc = x.ThoiGianKetThuc.ToString(@"hh\:mm"),
                    LyDo = x.LyDo,
                    TrangThai = x.TrangThai,
                    LyDoTuChoi = x.LyDoTuChoi,
                    HoTenNguoiDuyet = x.NguoiDuyet != null ? x.NguoiDuyet.TenDangNhap : null,
                    NgayDuyet = x.NgayDuyet,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<int> CreateDiMuonAsync(int taiKhoanId, CreateDonDiMuonDto dto)
        {
            var nvId = await GetNvHoSoIdAsync(taiKhoanId)
                ?? throw new InvalidOperationException("Không tìm thấy hồ sơ nhân viên.");

            var entity = new DonDiMuon
            {
                NvHoSoId = nvId,
                Loai = dto.Loai,
                NgayApDung = dto.NgayApDung,
                ThoiGianBatDau = TimeSpan.Parse(dto.ThoiGianBatDau),
                ThoiGianKetThuc = TimeSpan.Parse(dto.ThoiGianKetThuc),
                LyDo = dto.LyDo,
                TrangThai = "CHO_DUYET",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.DonDiMuons.Add(entity);
            await _context.SaveChangesAsync();

            // Notify HR
            try
            {
                var hoTen = await _context.NvHoSos
                    .Where(x => x.Id == nvId).Select(x => x.HoTen).FirstOrDefaultAsync();
                var tenLoai = GetTenLoai(dto.Loai);
                var hrIds = await GetHrAccountIdsAsync();
                foreach (var hrId in hrIds)
                    await _thongBaoService.CreateAndPushAsync(
                        hrId,
                        "Đơn xin phép mới",
                        $"{hoTen} vừa gửi đơn '{tenLoai}' ngày {dto.NgayApDung:dd/MM/yyyy} cần duyệt.",
                        "YEU_CAU",
                        "DonDiMuon",
                        entity.Id,
                        "/hr/yeu-cau-noi-lam-viec",
                        taiKhoanId);
            }
            catch { }

            // Realtime: cập nhật danh sách HR
            await _thongBaoService.BroadcastEntityUpdateAsync("DonDiMuon", entity.Id, "CREATED");

            return entity.Id;
        }

        public async Task DuyetDiMuonAsync(DuyetDonDiMuonDto dto, int taiKhoanId)
        {
            var entity = await _context.DonDiMuons
                .Include(x => x.NvHoSo)
                .FirstOrDefaultAsync(x => x.Id == dto.DonId)
                ?? throw new KeyNotFoundException("Không tìm thấy đơn.");

            entity.TrangThai = dto.ChapNhan ? "DA_DUYET" : "TU_CHOI";
            entity.NguoiDuyetId = taiKhoanId;
            entity.NgayDuyet = DateTime.UtcNow;
            entity.LyDoTuChoi = dto.ChapNhan ? null : dto.LyDoTuChoi;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Notify employee
            try
            {
                var nvTkId = await GetTaiKhoanIdByNvHoSoIdAsync(entity.NvHoSoId);
                if (nvTkId.HasValue)
                {
                    var tenLoai = GetTenLoai(entity.Loai);
                    var title = dto.ChapNhan ? $"Đơn '{tenLoai}' được duyệt" : $"Đơn '{tenLoai}' bị từ chối";
                    var msg = dto.ChapNhan
                        ? $"Đơn xin phép '{tenLoai}' ngày {entity.NgayApDung:dd/MM/yyyy} đã được phê duyệt."
                        : $"Đơn xin phép '{tenLoai}' bị từ chối. Lý do: {dto.LyDoTuChoi}";
                    await _thongBaoService.CreateAndPushAsync(
                        nvTkId.Value, title, msg,
                        dto.ChapNhan ? "SUCCESS" : "WARNING",
                        "DonDiMuon", entity.Id,
                        "/employee/don-di-muon",
                        taiKhoanId);
                }
            }
            catch { }

            // Realtime: cập nhật danh sách employee
            await _thongBaoService.BroadcastEntityUpdateAsync("DonDiMuon", entity.Id,
                dto.ChapNhan ? "APPROVED" : "REJECTED");
        }

        public async Task DeleteDiMuonAsync(int id, int taiKhoanId)
        {
            var nvId = await GetNvHoSoIdAsync(taiKhoanId);
            var entity = await _context.DonDiMuons
                .FirstOrDefaultAsync(x => x.Id == id && x.NvHoSoId == nvId)
                ?? throw new KeyNotFoundException("Không tìm thấy đơn.");

            if (entity.TrangThai != "CHO_DUYET")
                throw new InvalidOperationException("Chỉ được xóa đơn đang chờ duyệt.");

            _context.DonDiMuons.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateDiMuonAsync(int id, int taiKhoanId, UpdateDonDiMuonDto dto)
        {
            var nvId = await GetNvHoSoIdAsync(taiKhoanId);
            var entity = await _context.DonDiMuons
                .FirstOrDefaultAsync(x => x.Id == id && x.NvHoSoId == nvId)
                ?? throw new KeyNotFoundException("Không tìm thấy đơn.");

            if (entity.TrangThai != "CHO_DUYET")
                throw new InvalidOperationException("Chỉ được sửa đơn đang chờ duyệt.");

            entity.Loai = dto.Loai;
            entity.NgayApDung = dto.NgayApDung;
            entity.ThoiGianBatDau = TimeSpan.Parse(dto.ThoiGianBatDau);
            entity.ThoiGianKetThuc = TimeSpan.Parse(dto.ThoiGianKetThuc);
            entity.LyDo = dto.LyDo;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        // ===========================================================
        // THỐNG KÊ
        // ===========================================================

        public async Task<ThongKeNoiLamViecDto> GetThongKeAsync(int taiKhoanId)
        {
            var nvId = await GetNvHoSoIdAsync(taiKhoanId);

            var deXuats = await _context.PhieuDeXuatDungCus
                .Where(x => x.NvHoSoId == nvId)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Tong = g.Count(),
                    ChoDuyet = g.Count(x => x.TrangThai == "CHO_DUYET"),
                    DaDuyet = g.Count(x => x.TrangThai == "DA_DUYET"),
                    TuChoi = g.Count(x => x.TrangThai == "TU_CHOI")
                })
                .FirstOrDefaultAsync();

            var tamUngs = await _context.PhieuTamUngs
                .Where(x => x.NvHoSoId == nvId)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Tong = g.Count(),
                    ChoDuyet = g.Count(x => x.TrangThai == "CHO_DUYET"),
                    DaDuyet = g.Count(x => x.TrangThai == "DA_DUYET"),
                    TuChoi = g.Count(x => x.TrangThai == "TU_CHOI")
                })
                .FirstOrDefaultAsync();

            var diMuons = await _context.DonDiMuons
                .Where(x => x.NvHoSoId == nvId)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Tong = g.Count(),
                    ChoDuyet = g.Count(x => x.TrangThai == "CHO_DUYET"),
                    DaDuyet = g.Count(x => x.TrangThai == "DA_DUYET"),
                    TuChoi = g.Count(x => x.TrangThai == "TU_CHOI")
                })
                .FirstOrDefaultAsync();

            return new ThongKeNoiLamViecDto
            {
                TongDeXuat = deXuats?.Tong ?? 0,
                DeXuatChoDuyet = deXuats?.ChoDuyet ?? 0,
                DeXuatDaDuyet = deXuats?.DaDuyet ?? 0,
                DeXuatTuChoi = deXuats?.TuChoi ?? 0,

                TongTamUng = tamUngs?.Tong ?? 0,
                TamUngChoDuyet = tamUngs?.ChoDuyet ?? 0,
                TamUngDaDuyet = tamUngs?.DaDuyet ?? 0,
                TamUngTuChoi = tamUngs?.TuChoi ?? 0,

                TongDiMuon = diMuons?.Tong ?? 0,
                DiMuonChoDuyet = diMuons?.ChoDuyet ?? 0,
                DiMuonDaDuyet = diMuons?.DaDuyet ?? 0,
                DiMuonTuChoi = diMuons?.TuChoi ?? 0
            };
        }
    }
}
