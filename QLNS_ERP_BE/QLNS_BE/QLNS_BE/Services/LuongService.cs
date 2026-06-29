using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;
using QLNS_BE.Models.Dtos.Luong;
using QLNS_BE.Models.Entities;

namespace QLNS_BE.Services
{
    public class LuongService
    {
        private readonly AppDbContext _context;

        public LuongService(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // 1. HR – TÍNH LƯƠNG THÁNG CHO 1 NHÂN VIÊN
        // =====================================================
        public async Task<BangLuongThangDto> TinhLuongAsync(TinhLuongRequestDto dto, int taiKhoanTinhId)
        {
            var nvLuong = await _context.NvLuongHienTais
                .FirstOrDefaultAsync(x => x.NvHoSoId == dto.NvHoSoId && x.DangApDung);

            if (nvLuong == null)
                throw new Exception("Nhân viên chưa có thông tin lương hiện tại");

            var bangCong = await _context.BangCongThangs
                .FirstOrDefaultAsync(x => x.Thang == dto.Thang && x.Nam == dto.Nam);

            if (bangCong == null)
                throw new Exception("Bảng công tháng chưa tồn tại");

            if (bangCong.TrangThaiCong != "DA_CHOT_CONG")
                throw new Exception($"Vui lòng chốt công tháng {dto.Thang}/{dto.Nam} trước khi tính lương");

            // Tính tổng công: bao gồm cả "DI_LAM" (đúng giờ) và "TRE" (đi muộn) vì nhân viên đi muộn vẫn được tính công
            var tongCong = await _context.ChamCongs
                .Where(x => x.NvHoSoId == dto.NvHoSoId
                         && x.BangCongThangId == bangCong.Id
                         && (x.TrangThai == "DI_LAM" || x.TrangThai == "TRE"))
                .CountAsync();

            var tongOt = await _context.ChamCongs
                .Where(x => x.NvHoSoId == dto.NvHoSoId && x.BangCongThangId == bangCong.Id)
                .SumAsync(x => x.SoGioOt);

            // ── Tính phụ cấp: ưu tiên dùng itemized NvPhuCap (với tên loại), fallback về PhuCapCoDinh
            var today = DateTime.Today;
            var phuCapItems = await _context.NvPhuCaps
                .Include(x => x.PhuCapLoai)
                .Where(x => x.NvHoSoId == dto.NvHoSoId
                         && x.DangApDung
                         && x.NgayBatDau <= today
                         && (x.NgayKetThuc == null || x.NgayKetThuc >= today))
                .ToListAsync();

            decimal tongPhuCap = phuCapItems.Sum(x => x.SoTien);
            bool phuCapFallback = tongPhuCap == 0m;
            if (phuCapFallback) tongPhuCap = nvLuong.PhuCapCoDinh;

            // ── Lấy tham số tính lương từ DB (fallback về giá trị mặc định)
            var thamSos = await _context.ThamSoHeThongs.ToListAsync();
            T GetThamSo<T>(string ma, T defaultVal)
            {
                var raw = thamSos.FirstOrDefault(x => x.MaThamSo == ma)?.GiaTri;
                if (raw == null) return defaultVal;
                try { return (T)Convert.ChangeType(raw, typeof(T)); } catch { return defaultVal; }
            }

            decimal ngayCongChuan = GetThamSo("LUONG_NGAY_CONG_CHUAN", 26m);
            decimal gioLamChuan = GetThamSo("LUONG_GIO_LAM_CHUAN", 8m);
            decimal heSoOT = GetThamSo("LUONG_HE_SO_OT", 1.5m);
            decimal phatDiMuon = GetThamSo("LUONG_PHAT_DI_MUON", 30_000m);

            // ── Cấu hình bật/tắt các thành phần trong công thức lương
            bool coTinhPhuCap = GetThamSo("LUONG_CO_TINH_PHU_CAP", 1) != 0;
            bool coTinhOt = GetThamSo("LUONG_CO_TINH_OT", 1) != 0;
            bool coTinhThuong = GetThamSo("LUONG_CO_TINH_THUONG", 1) != 0; bool coTinhKhauTru = GetThamSo("LUONG_CO_TINH_KHAU_TRU", 1) != 0;
            var gioChuanVao = TimeSpan.FromHours(8);
            var cfgGioVao = thamSos.FirstOrDefault(x => x.MaThamSo == "CHAM_CONG_GIO_VAO")?.GiaTri;
            if (cfgGioVao != null && TimeSpan.TryParse(cfgGioVao, out var parsedGio))
                gioChuanVao = parsedGio;
            int giaCuPhut = GetThamSo("CHAM_CONG_GIO_GIA_CU", 1);
            var gioChuanCoGiaCu = gioChuanVao.Add(TimeSpan.FromMinutes(giaCuPhut));

            // ── Tính lương
            decimal luongNgay = nvLuong.LuongCoBan / ngayCongChuan;
            decimal luongCong = luongNgay * tongCong;
            decimal luongOt = tongOt * (luongNgay / gioLamChuan) * heSoOT;

            // ── Upsert BangLuongThang để lấy Id (cần trước khi tổng hợp items)
            var entity = await _context.BangLuongThangs
                .FirstOrDefaultAsync(x => x.NvHoSoId == dto.NvHoSoId
                                       && x.Thang == dto.Thang
                                       && x.Nam == dto.Nam);

            // ── Chặn tính lại nếu đã được Giám đốc duyệt / khóa
            if (entity != null)
            {
                if (entity.TrangThai == "DA_DUYET" || entity.TrangThai == "DA_KHOA")
                    throw new Exception("Bảng lương này đã được Giám đốc duyệt, không thể tính lại");
                if (entity.TrangThai == "CHO_DUYET_GIAM_DOC")
                    throw new Exception("Bảng lương này đang chờ Giám đốc duyệt, không thể tính lại");
            }

            if (entity == null)
            {
                entity = new BangLuongThang
                {
                    NvHoSoId = dto.NvHoSoId,
                    BangCongThangId = bangCong.Id,
                    Thang = dto.Thang,
                    Nam = dto.Nam,
                    TrangThai = "TAM_TINH",
                    TaiKhoanTinhId = taiKhoanTinhId,
                };
                _context.BangLuongThangs.Add(entity);
                await _context.SaveChangesAsync(); // lấy Id
            }

            // ── Tổng hợp thưởng / khấu trừ từ BangLuongItems
            var items = await _context.BangLuongItems
                .Where(x => x.BangLuongThangId == entity.Id)
                .ToListAsync();
            decimal tongThuong = items.Where(x => x.Loai == "THUONG").Sum(x => x.SoTien);
            decimal tongKhauTruItems = items.Where(x => x.Loai == "KHAU_TRU").Sum(x => x.SoTien);

            // ── Tính phạt đi muộn:
            // - Bản ghi TrangThai=="TRE": đã bị face-recognition xác định muộn → tính phạt trực tiếp
            // - Bản ghi TrangThai=="DI_LAM" + GioVao > giờ chuẩn+gia cú: nhập tay muộn
            int soLanDiMuon = await _context.ChamCongs
                .Where(x => x.NvHoSoId == dto.NvHoSoId
                         && x.BangCongThangId == bangCong.Id
                         && (x.TrangThai == "TRE"
                             || (x.TrangThai == "DI_LAM"
                                  && x.GioVao != null
                                  && x.GioVao.Value.TimeOfDay > gioChuanCoGiaCu)))
                .CountAsync();

            decimal khauTruMuon = soLanDiMuon * phatDiMuon;
            decimal tongKhauTru = tongKhauTruItems + khauTruMuon;

            // ── Áp dụng cờ công thức
            decimal tongLuong = luongCong
                + (coTinhPhuCap ? tongPhuCap : 0m)
                + (coTinhOt ? luongOt : 0m)
                + (coTinhThuong ? tongThuong : 0m)
                - (coTinhKhauTru ? tongKhauTru : 0m);
            if (tongLuong < 0) tongLuong = 0;

            entity.TongCong = tongCong;
            entity.TongOt = tongOt;
            entity.LuongCoBanTinh = nvLuong.LuongCoBan;
            entity.PhuCapTinh = tongPhuCap;
            entity.Thuong = tongThuong;
            entity.KhauTru = tongKhauTru;
            entity.TongLuong = tongLuong;
            entity.NgayTinhLuong = DateTime.UtcNow;
            entity.TaiKhoanTinhId = taiKhoanTinhId;

            await _context.SaveChangesAsync();

            var nv = await _context.NvHoSos.FindAsync(dto.NvHoSoId);

            // ── Xây dựng chi tiết phụ cấp
            var chiTietPhuCap = phuCapFallback
                ? (tongPhuCap > 0
                    ? new List<ChiTietMucLuongItem> { new() { Ten = "Phụ cấp cố định", SoTien = tongPhuCap } }
                    : new List<ChiTietMucLuongItem>())
                : phuCapItems
                    .Select(x => new ChiTietMucLuongItem
                    {
                        Ten = x.PhuCapLoai?.TenPhuCap ?? "Phụ cấp",
                        SoTien = x.SoTien
                    })
                    .ToList();

            // ── Xây dựng chi tiết khấu trừ từ hệ thống thưởng & phạt
            var chiTietKhauTruItems = items
                .Where(x => x.Loai == "KHAU_TRU")
                .Select(x => new ChiTietMucLuongItem { Ten = x.LyDo, SoTien = x.SoTien })
                .ToList();

            return new BangLuongThangDto
            {
                Id = entity.Id,
                NvHoSoId = dto.NvHoSoId,
                HoTen = nv!.HoTen,
                Thang = dto.Thang,
                Nam = dto.Nam,
                TongCong = entity.TongCong,
                TongOt = entity.TongOt,
                LuongCoBanTinh = entity.LuongCoBanTinh,
                PhuCapTinh = entity.PhuCapTinh,
                Thuong = entity.Thuong,
                KhauTru = entity.KhauTru,
                TongLuong = entity.TongLuong,
                TrangThai = entity.TrangThai,
                CoTinhPhuCap = coTinhPhuCap,
                CoTinhOT = coTinhOt,
                CoTinhThuong = coTinhThuong,
                CoTinhKhauTru = coTinhKhauTru,
                ChiTietPhuCap = chiTietPhuCap,
                KhauTruDiMuon = khauTruMuon,
                KhauTruThuongPhat = tongKhauTruItems,
                SoLanDiMuon = soLanDiMuon,
                ChiTietKhauTruItems = chiTietKhauTruItems
            };
        }

        // =====================================================
        // 2. HR – GỬI DUYỆT GIÁM ĐỐC
        // =====================================================
        public async Task<bool> GuiDuyetLuongAsync(int bangLuongId, GuiDuyetLuongRequestDto dto, int taiKhoanGuiId)
        {
            var bl = await _context.BangLuongThangs.FindAsync(bangLuongId);
            if (bl == null) return false;

            if (bl.TrangThai != "TAM_TINH")
                throw new Exception("Chỉ bảng lương tạm tính mới được gửi duyệt");

            bl.TrangThai = "CHO_DUYET_GIAM_DOC";
            bl.TaiKhoanGuiDuyetId = taiKhoanGuiId;
            bl.NgayGuiDuyet = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        // =====================================================
        // 3. GIÁM ĐỐC – DUYỆT HOẶC TỪ CHỐI
        // =====================================================
        public async Task<bool> DuyetLuongAsync(int id, DuyetLuongRequestDto dto, int taiKhoanDuyetId)
        {
            var bl = await _context.BangLuongThangs.FindAsync(id);
            if (bl == null) return false;

            if (bl.TrangThai != "CHO_DUYET_GIAM_DOC")
                throw new Exception("Bảng lương không ở trạng thái chờ duyệt");

            if (dto.DongY)
            {
                bl.TrangThai = "DA_DUYET";
                bl.NgayDuyetGiamDoc = DateTime.UtcNow;
            }
            else
            {
                bl.TrangThai = "TU_CHOI";
            }

            bl.TaiKhoanDuyetId = taiKhoanDuyetId;
            await _context.SaveChangesAsync();

            return true;
        }

        // =====================================================
        // 3.5. HR – THU HỒI LƯƠNG ĐÃ GỬI DUYỆT
        // =====================================================
        public async Task<bool> ThuHoiLuongAsync(int bangLuongId, int taiKhoanId)
        {
            var bl = await _context.BangLuongThangs.FindAsync(bangLuongId);
            if (bl == null) return false;

            // Chỉ cho phép thu hồi bảng lương đang chờ duyệt
            if (bl.TrangThai != "CHO_DUYET_GIAM_DOC")
                throw new Exception("Chỉ có thể thu hồi bảng lương đang chờ duyệt");

            // Trả về trạng thái tạm tính
            bl.TrangThai = "TAM_TINH";
            bl.TaiKhoanGuiDuyetId = null;
            bl.NgayGuiDuyet = null;

            await _context.SaveChangesAsync();
            return true;
        }

        // =====================================================
        // 4. NHÂN VIÊN – XEM LƯƠNG CỦA TÔI
        // =====================================================
        public async Task<List<LuongCuaToiDto>> GetLuongCuaToiAsync(int nvId)
        {
            var data = await _context.BangLuongThangs
                .Where(x => x.NvHoSoId == nvId)
                .OrderByDescending(x => x.Nam)
                .ThenByDescending(x => x.Thang)
                .Select(x => new LuongCuaToiDto
                {
                    Thang = x.Thang,
                    Nam = x.Nam,
                    LuongCoBan = x.LuongCoBanTinh,
                    PhuCapCoDinh = x.PhuCapTinh,
                    TongCong = x.TongCong,
                    TongOt = x.TongOt,
                    Thuong = x.Thuong,
                    KhauTru = x.KhauTru,
                    TongLuong = x.TongLuong,
                    TrangThai = x.TrangThai
                })
                .ToListAsync();

            return data;
        }
        // =====================================================
        // 4.5) HR & GIÁM ĐỐC – DANH SÁCH BẢNG LƯƠNG
        // =====================================================
        public async Task<List<BangLuongThangListItemDto>> GetListAsync()
        {
            var data = await _context.BangLuongThangs
                .AsNoTracking()
                .Include(x => x.NvHoSo)
                .OrderByDescending(x => x.Nam)
                .ThenByDescending(x => x.Thang)
                .ThenByDescending(x => x.Id)
                .Select(x => new BangLuongThangListItemDto
                {
                    Id = x.Id,
                    NvHoSoId = x.NvHoSoId,
                    HoTen = x.NvHoSo.HoTen,
                    MaNhanVien = x.NvHoSo.MaNhanVien,
                    TenPhongBan = _context.NvCongViecs
                        .Where(cv => cv.NvHoSoId == x.NvHoSoId && cv.NgayNghiViec == null)
                        .OrderByDescending(cv => cv.NgayVaoLam)
                        .Select(cv => cv.PhongBan.TenPhongBan)
                        .FirstOrDefault(),
                    Thang = x.Thang,
                    Nam = x.Nam,
                    TongLuong = x.TongLuong,

                    TongCong = x.TongCong,
                    TongOt = x.TongOt,
                    LuongCoBanTinh = x.LuongCoBanTinh,
                    PhuCapTinh = x.PhuCapTinh,
                    Thuong = x.Thuong,
                    KhauTru = x.KhauTru,
                    TrangThai = TrangThaiLuongMapper.FromDb(x.TrangThai),

                    NgayTinhLuong = x.NgayTinhLuong,
                    NgayGuiDuyet = x.NgayGuiDuyet,
                    NgayDuyetGiamDoc = x.NgayDuyetGiamDoc,
                    NgayKhoaLuong = x.NgayKhoaLuong,
                    KhauTruThuongPhat = 0m,  // populated below
                    KhauTruDiMuon = 0m        // populated below
                })
                .ToListAsync();

            // ── Bổ sung breakdown khấu trừ (T&P vs đi muộn) bằng 1 query gộp
            var bangLuongIds = data.Select(d => d.Id).ToList();
            var itemKhauTruSums = await _context.BangLuongItems
                .Where(i => bangLuongIds.Contains(i.BangLuongThangId) && i.Loai == "KHAU_TRU")
                .GroupBy(i => i.BangLuongThangId)
                .Select(g => new { BangLuongThangId = g.Key, Total = g.Sum(i => i.SoTien) })
                .ToListAsync();

            // Đọc cờ công thức trực tiếp từ DB (GetThamSo là local fn của TinhLuongAsync)
            var flagMas = new[] { "LUONG_CO_TINH_PHU_CAP", "LUONG_CO_TINH_OT", "LUONG_CO_TINH_THUONG", "LUONG_CO_TINH_KHAU_TRU" };
            var flagRows = await _context.ThamSoHeThongs
                .Where(x => flagMas.Contains(x.MaThamSo))
                .Select(x => new { x.MaThamSo, x.GiaTri })
                .ToListAsync();
            bool GetFlag(string ma) => flagRows.FirstOrDefault(x => x.MaThamSo == ma)?.GiaTri != "0";
            bool flagPhuCap = GetFlag("LUONG_CO_TINH_PHU_CAP");
            bool flagOT = GetFlag("LUONG_CO_TINH_OT");
            bool flagThuong = GetFlag("LUONG_CO_TINH_THUONG");
            bool flagKhauTru = GetFlag("LUONG_CO_TINH_KHAU_TRU");

            foreach (var item in data)
            {
                var kt = itemKhauTruSums.FirstOrDefault(s => s.BangLuongThangId == item.Id);
                item.KhauTruThuongPhat = kt?.Total ?? 0m;
                item.KhauTruDiMuon = item.KhauTru - item.KhauTruThuongPhat;
                item.CoTinhPhuCap = flagPhuCap;
                item.CoTinhOT = flagOT;
                item.CoTinhThuong = flagThuong;
                item.CoTinhKhauTru = flagKhauTru;
            }

            return data;
        }


        // =====================================================
        // 5. HR / GD – LẤY CHI TIẾT 1 BẢNG LƯƠNG
        // =====================================================
        public async Task<BangLuongThangDetailDto?> GetDetailAsync(int id)
        {
            var bl = await _context.BangLuongThangs
                .Include(x => x.NvHoSo)
                .Include(x => x.TaiKhoanTinh)
                .Include(x => x.TaiKhoanGuiDuyet)
                .Include(x => x.TaiKhoanDuyet)
                .Include(x => x.TaiKhoanKhoa)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (bl == null) return null;

            return new BangLuongThangDetailDto
            {
                Id = bl.Id,
                NvHoSoId = bl.NvHoSoId,
                HoTen = bl.NvHoSo.HoTen,
                MaNhanVien = bl.NvHoSo.MaNhanVien,
                TenPhongBan = _context.NvCongViecs
                    .Where(cv => cv.NvHoSoId == bl.NvHoSoId && cv.NgayNghiViec == null)
                    .OrderByDescending(cv => cv.NgayVaoLam)
                    .Select(cv => cv.PhongBan.TenPhongBan)
                    .FirstOrDefault(),
                Thang = bl.Thang,
                Nam = bl.Nam,
                TongCong = bl.TongCong,
                TongOt = bl.TongOt,
                LuongCoBanTinh = bl.LuongCoBanTinh,
                PhuCapTinh = bl.PhuCapTinh,
                Thuong = bl.Thuong,
                KhauTru = bl.KhauTru,
                TongLuong = bl.TongLuong,
                TrangThai = bl.TrangThai,
                NgayTinhLuong = bl.NgayTinhLuong,
                NgayGuiDuyet = bl.NgayGuiDuyet,
                NgayDuyetGiamDoc = bl.NgayDuyetGiamDoc,
                NgayKhoaLuong = bl.NgayKhoaLuong,
                NguoiTinh = bl.TaiKhoanTinh?.TenDangNhap,
                NguoiGuiDuyet = bl.TaiKhoanGuiDuyet?.TenDangNhap,
                TaiKhoanGuiDuyetId = bl.TaiKhoanGuiDuyetId, // For notification
                NguoiDuyet = bl.TaiKhoanDuyet?.TenDangNhap,
                NguoiKhoa = bl.TaiKhoanKhoa?.TenDangNhap
            };
        }
        private static TrangThaiLuong ParseTrangThai(string? dbValue)
        {
            if (string.IsNullOrWhiteSpace(dbValue)) return TrangThaiLuong.KHAC;
            return Enum.TryParse<TrangThaiLuong>(dbValue, out var st) ? st : TrangThaiLuong.KHAC;
        }
        public async Task<LuongTongLuongThangDto> GetTongLuongTheoThangAsync(int thang, int nam)
        {
            var q = _context.BangLuongThangs
                .AsNoTracking()
                .Where(x => x.Thang == thang && x.Nam == nam);

            // gom nhóm theo trạng thái để vừa lấy count vừa lấy tổng lương
            var grouped = await q
                .GroupBy(x => x.TrangThai)
                .Select(g => new
                {
                    TrangThai = g.Key,
                    Count = g.Count(),
                    TongLuong = g.Select(x => (decimal?)x.TongLuong).Sum() ?? 0m
                })
                .ToListAsync();

            var result = new LuongTongLuongThangDto
            {
                Thang = thang,
                Nam = nam,
                SoBangLuong = grouped.Sum(x => x.Count),
                TongLuongTatCa = grouped.Sum(x => x.TongLuong)
            };

            foreach (var item in grouped)
            {
                switch (ParseTrangThai(item.TrangThai))
                {
                    case TrangThaiLuong.TAM_TINH:
                        result.TongLuongTamTinh += item.TongLuong; break;
                    case TrangThaiLuong.CHO_DUYET_GIAM_DOC:
                        result.TongLuongChoDuyet += item.TongLuong; break;
                    case TrangThaiLuong.DA_DUYET:
                        result.TongLuongDaDuyet += item.TongLuong; break;
                    case TrangThaiLuong.TU_CHOI:
                        result.TongLuongTuChoi += item.TongLuong; break;
                    case TrangThaiLuong.DA_KHOA:
                        result.TongLuongDaKhoa += item.TongLuong; break;
                    default:
                        result.TongLuongKhac += item.TongLuong; break;
                }
            }

            return result;
        }
        public async Task<LuongThongKeTrangThaiDto> GetThongKeTrangThaiAsync(int thang, int nam)
        {
            var grouped = await _context.BangLuongThangs
                .AsNoTracking()
                .Where(x => x.Thang == thang && x.Nam == nam)
                .GroupBy(x => x.TrangThai)
                .Select(g => new { TrangThai = g.Key, Count = g.Count() })
                .ToListAsync();

            var result = new LuongThongKeTrangThaiDto
            {
                Thang = thang,
                Nam = nam
            };

            foreach (var item in grouped)
            {
                switch (ParseTrangThai(item.TrangThai))
                {
                    case TrangThaiLuong.TAM_TINH:
                        result.TamTinh += item.Count; break;
                    case TrangThaiLuong.CHO_DUYET_GIAM_DOC:
                        result.ChoDuyet += item.Count; break;
                    case TrangThaiLuong.DA_DUYET:
                        result.DaDuyet += item.Count; break;
                    case TrangThaiLuong.TU_CHOI:
                        result.TuChoi += item.Count; break;
                    case TrangThaiLuong.DA_KHOA:
                        result.DaKhoa += item.Count; break;
                    default:
                        result.Khac += item.Count; break;
                }
            }

            result.Tong = result.TamTinh + result.ChoDuyet + result.DaDuyet + result.TuChoi + result.DaKhoa + result.Khac;
            return result;
        }

        // ============================================================
        // NOTIFICATION HELPERS
        // ============================================================
        /// <summary>
        /// Lấy danh sách tài khoản Giám đốc để gửi thông báo
        /// </summary>
        public async Task<List<int>> GetDirectorAccountIdsAsync()
        {
            return await _context.TaiKhoans
                .Include(x => x.VaiTro)
                .Where(x => x.VaiTro != null && x.VaiTro.MaVaiTro == "GIAM_DOC")
                .Select(x => x.Id)
                .ToListAsync();
        }

    }
}
