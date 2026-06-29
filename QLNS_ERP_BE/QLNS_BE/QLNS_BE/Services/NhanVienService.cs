using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;
using QLNS_BE.Models.Dtos.Common;
using QLNS_BE.Models.Dtos.NhanVien;
using QLNS_BE.Models.Entities;

namespace QLNS_BE.Services
{
    public class NhanVienService
    {
        private readonly AppDbContext _context;
        public NhanVienService(AppDbContext context) 
        {
            _context = context;
        }


        /// <summary>
        /// Danh sách nhân viên (có phân trang + keyword).
        /// Keyword: MaNhanVien, HoTen.
        /// Công việc hiện tại = NV_CONG_VIEC.TrangThaiLamViec = 1 (nếu có).
        /// </summary>

        public async Task<PagedResultDto<NhanVienListItemDto>> GetPagedAsync(PagingRequestDto request)
        {
            if (request.PageIndex <= 0) request.PageIndex = 1;
            if (request.PageSize <= 0) request.PageSize = 20;

            var query =
                from nv in _context.NvHoSos
                join cv in _context.NvCongViecs
                    .Where(x => x.TrangThaiLamViec == 1)
                    on nv.Id equals cv.NvHoSoId into cvJoin
                from cv in cvJoin.DefaultIfEmpty()
                join pb in _context.PhongBans on cv.PhongBanId equals pb.Id into pbJoin
                from pb in pbJoin.DefaultIfEmpty()
                join chv in _context.ChucVus on cv.ChucVuId equals chv.Id into chvJoin
                from chv in chvJoin.DefaultIfEmpty()
                select new NhanVienListItemDto
                {
                    Id = nv.Id,
                    MaNhanVien = nv.MaNhanVien,
                    HoTen = nv.HoTen,
                    PhongBanId = cv != null ? cv.PhongBanId : (int?)null,
                    TenPhongBan = pb != null ? pb.TenPhongBan : null,
                    ChucVuId = cv != null ? cv.ChucVuId : (int?)null,
                    TenChucVu = chv != null ? chv.TenChucVu : null,
                    TrangThaiLamViec = cv != null ? cv.TrangThaiLamViec : (byte)0,
                    NgayVaoLam = cv != null ? cv.NgayVaoLam : (DateTime?)null,
                    NgayNghiViec = cv != null ? cv.NgayNghiViec : (DateTime?)null
                };

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim();
                query = query.Where(x =>
                    x.MaNhanVien.Contains(keyword) ||
                    x.HoTen.Contains(keyword));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(x => x.MaNhanVien)
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResultDto<NhanVienListItemDto>
            {
                Items = items,
                TotalCount = total,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize
            };
        }

        /// <summary>
        /// Lấy chi tiết 1 nhân viên (hồ sơ + công việc hiện tại).
        /// </summary>
        public async Task<NhanVienDetailDto?> GetByIdAsync(int id)
        {
            var query =
                from nv in _context.NvHoSos
                where nv.Id == id
                join cv in _context.NvCongViecs
                    .Where(x => x.TrangThaiLamViec == 1)
                    on nv.Id equals cv.NvHoSoId into cvJoin
                from cv in cvJoin.DefaultIfEmpty()
                join pb in _context.PhongBans on cv.PhongBanId equals pb.Id into pbJoin
                from pb in pbJoin.DefaultIfEmpty()
                join chv in _context.ChucVus on cv.ChucVuId equals chv.Id into chvJoin
                from chv in chvJoin.DefaultIfEmpty()
                select new NhanVienDetailDto
                {
                    Id = nv.Id,
                    MaNhanVien = nv.MaNhanVien,
                    HoTen = nv.HoTen,
                    NgaySinh = nv.NgaySinh,
                    GioiTinh = nv.GioiTinh,
                    DiaChi = nv.DiaChi,
                    SoDienThoai = nv.SoDienThoai,
                    EmailCaNhan = nv.EmailCaNhan,
                    SoTaiKhoanNganHang = nv.SoTaiKhoanNganHang,
                    AnhStkUrl = nv.AnhStkUrl,
                    HopDongUrl = nv.HopDongUrl,
                    // Lấy tên ngân hàng + chi nhánh từ bản lương đang áp dụng
                    TenNganHang = _context.NvLuongHienTais
                        .Where(l => l.NvHoSoId == nv.Id && l.DangApDung)
                        .Select(l => l.TenNganHang)
                        .FirstOrDefault(),
                    ChiNhanhNganHang = _context.NvLuongHienTais
                        .Where(l => l.NvHoSoId == nv.Id && l.DangApDung)
                        .Select(l => l.ChiNhanhNganHang)
                        .FirstOrDefault(),

                    NvCongViecId = cv != null ? cv.Id : (int?)null,
                    PhongBanId = cv != null ? cv.PhongBanId : (int?)null,
                    TenPhongBan = pb != null ? pb.TenPhongBan : null,
                    ChucVuId = cv != null ? cv.ChucVuId : (int?)null,
                    TenChucVu = chv != null ? chv.TenChucVu : null,
                    NgayVaoLam = cv != null ? cv.NgayVaoLam : (DateTime?)null,
                    NgayNghiViec = cv != null ? cv.NgayNghiViec : (DateTime?)null,
                    LoaiHopDong = cv != null ? cv.LoaiHopDong : null,
                    NgayKyHopDong = cv != null ? cv.NgayKyHopDong : (DateTime?)null,
                    NgayHetHanHopDong = cv != null ? cv.NgayHetHanHopDong : (DateTime?)null,
                    TrangThaiLamViec = cv != null ? (byte?)cv.TrangThaiLamViec : null,
                    GhiChu = cv != null ? cv.GhiChu : null
                };

            return await query.FirstOrDefaultAsync();
        }

        /// <summary>
        /// Tạo mới NV_HO_SO + 1 dòng NV_CONG_VIEC.
        /// </summary>
        public async Task<NhanVienDetailDto> CreateAsync(NhanVienCreateDto dto)
        {
            // Check mã nhân viên trùng
            var exists = await _context.NvHoSos
                .AnyAsync(x => x.MaNhanVien == dto.MaNhanVien);
            if (exists)
                throw new InvalidOperationException("Mã nhân viên đã tồn tại");

            var hoSo = new NvHoSo
            {
                MaNhanVien = dto.MaNhanVien,
                HoTen = dto.HoTen,
                NgaySinh = dto.NgaySinh,
                GioiTinh = dto.GioiTinh,
                DiaChi = dto.DiaChi,
                SoDienThoai = dto.SoDienThoai,
                EmailCaNhan = dto.EmailCaNhan,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.NvHoSos.Add(hoSo);
            await _context.SaveChangesAsync();

            var congViec = new NvCongViec
            {
                NvHoSoId = hoSo.Id,
                PhongBanId = dto.PhongBanId,
                ChucVuId = dto.ChucVuId,
                NgayVaoLam = dto.NgayVaoLam,
                NgayNghiViec = null,
                LoaiHopDong = dto.LoaiHopDong,
                NgayKyHopDong = dto.NgayKyHopDong,
                NgayHetHanHopDong = dto.NgayHetHanHopDong,
                TrangThaiLamViec = 1, // đang làm
                GhiChu = dto.GhiChu
            };

            _context.NvCongViecs.Add(congViec);
            await _context.SaveChangesAsync();

            var result = await GetByIdAsync(hoSo.Id);
            return result!;
        }


        /// <summary>
        /// Cập nhật hồ sơ + công việc hiện tại.
        /// </summary>
        public async Task<NhanVienDetailDto> UpdateAsync(int id, NhanVienUpdateDto dto)
        {
            var hoSo = await _context.NvHoSos.FirstOrDefaultAsync(x => x.Id == id);
            if (hoSo == null)
                throw new KeyNotFoundException("Không tìm thấy nhân viên");

            // Cập nhật hồ sơ
            hoSo.HoTen = dto.HoTen;
            hoSo.NgaySinh = dto.NgaySinh;
            hoSo.GioiTinh = dto.GioiTinh;
            hoSo.DiaChi = dto.DiaChi;
            hoSo.SoDienThoai = dto.SoDienThoai;
            hoSo.EmailCaNhan = dto.EmailCaNhan;
            hoSo.UpdatedAt = DateTime.UtcNow;

            // Lấy bản ghi công việc hiện tại (trạng thái đang làm)
            var congViec = await _context.NvCongViecs
                .Where(x => x.NvHoSoId == id && x.TrangThaiLamViec == 1)
                .OrderByDescending(x => x.NgayVaoLam)
                .FirstOrDefaultAsync();

            if (congViec == null)
            {
                congViec = new NvCongViec
                {
                    NvHoSoId = hoSo.Id
                };
                _context.NvCongViecs.Add(congViec);
            }

            congViec.PhongBanId = dto.PhongBanId;
            congViec.ChucVuId = dto.ChucVuId;
            congViec.NgayVaoLam = dto.NgayVaoLam;
            congViec.NgayNghiViec = dto.NgayNghiViec;
            congViec.LoaiHopDong = dto.LoaiHopDong;
            congViec.NgayKyHopDong = dto.NgayKyHopDong;
            congViec.NgayHetHanHopDong = dto.NgayHetHanHopDong;
            congViec.TrangThaiLamViec = dto.TrangThaiLamViec;
            congViec.GhiChu = dto.GhiChu;

            await _context.SaveChangesAsync();

            var result = await GetByIdAsync(hoSo.Id);
            return result!;
        }
        /// <summary>
        /// Cho nhân viên nghỉ việc: set trạng_thai_lam_viec = 0 + ngày nghỉ.
        /// </summary>
        public async Task MarkAsResignedAsync(int id, DateTime? ngayNghiViec = null)
        {
            var congViec = await _context.NvCongViecs
                .Where(x => x.NvHoSoId == id && x.TrangThaiLamViec == 1)
                .OrderByDescending(x => x.NgayVaoLam)
                .FirstOrDefaultAsync();

            if (congViec == null)
                throw new KeyNotFoundException("Không tìm thấy thông tin công việc đang làm của nhân viên");

            congViec.TrangThaiLamViec = 0;
            congViec.NgayNghiViec = ngayNghiViec ?? DateTime.Today;

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Xóa cứng NV_HO_SO (cẩn thận FK). Trong thực tế nên dùng MarkAsResigned.
        /// </summary>
        public async Task DeleteAsync(int id)
        {
            var hoSo = await _context.NvHoSos.FirstOrDefaultAsync(x => x.Id == id);
            if (hoSo == null)
                throw new KeyNotFoundException("Không tìm thấy nhân viên");

            _context.NvHoSos.Remove(hoSo);
            await _context.SaveChangesAsync();
        }
        // ============================================================
        // HỒ SƠ CÁ NHÂN (TAI_KHOAN + VAI_TRO + NV_HO_SO + CÔNG VIỆC HIỆN TẠI)
        // ============================================================
        public async Task<HoSoCaNhanDto?> GetHoSoCaNhanAsync(int nvHoSoId)
        {
            // 1) NV_HO_SO
            var nv = await _context.NvHoSos
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == nvHoSoId);

            if (nv == null) return null;

            // 2) TAI_KHOAN + VAI_TRO
            // NOTE: theo các module bạn đang làm, userId == NvHoSoId
            var tk = await _context.TaiKhoans
                .Include(x => x.VaiTro)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.NvHoSoId == nvHoSoId);

            //var vt = tk == null
            //    ? null
            //    : await _context.VaiTros.AsNoTracking().FirstOrDefaultAsync(x => x.Id == tk.VaiTroId);
            var vt = tk?.VaiTro;

            // 3) CÔNG VIỆC HIỆN TẠI (NV_CONG_VIEC + tên phòng ban + tên chức vụ)
            var cv = await (
                from c in _context.NvCongViecs.AsNoTracking()
                where c.NvHoSoId == nvHoSoId && c.TrangThaiLamViec == 1
                orderby c.NgayVaoLam descending
                join pb in _context.PhongBans.AsNoTracking() on c.PhongBanId equals pb.Id into pbJoin
                from pb in pbJoin.DefaultIfEmpty()
                join chv in _context.ChucVus.AsNoTracking() on c.ChucVuId equals chv.Id into chvJoin
                from chv in chvJoin.DefaultIfEmpty()
                select new
                {
                    CV = c,
                    TenPhongBan = pb != null ? pb.TenPhongBan : null,
                    TenChucVu = chv != null ? chv.TenChucVu : null
                }
            ).FirstOrDefaultAsync();

            // 4) build DTO
            var dto = new HoSoCaNhanDto
            {
                TenDangNhap = tk?.TenDangNhap ?? "",
                VaiTroId = tk?.VaiTroId ?? 0,
                MaVaiTro = vt?.MaVaiTro ?? "",
                TenVaiTro = vt?.TenVaiTro ?? "",

                NvHoSoId = nv.Id,
                MaNhanVien = nv.MaNhanVien,
                HoTen = nv.HoTen,
                NgaySinh = nv.NgaySinh,
                GioiTinh = nv.GioiTinh,
                DiaChi = nv.DiaChi,
                SoDienThoai = nv.SoDienThoai,
                EmailCaNhan = nv.EmailCaNhan,
                // ADD
                AnhCaNhanUrl = nv.AnhCaNhanUrl,
                // ADD
                SoTaiKhoanNganHang = nv.SoTaiKhoanNganHang,
                // ADD - lấy từ NvLuongHienTai đang áp dụng
                TenNganHang = _context.NvLuongHienTais
                    .Where(l => l.NvHoSoId == nvHoSoId && l.DangApDung)
                    .Select(l => l.TenNganHang).FirstOrDefault(),
                ChiNhanhNganHang = _context.NvLuongHienTais
                    .Where(l => l.NvHoSoId == nvHoSoId && l.DangApDung)
                    .Select(l => l.ChiNhanhNganHang).FirstOrDefault(),
                // ADD
                AnhStkUrl = nv.AnhStkUrl,
                // ADD
                HopDongUrl = nv.HopDongUrl,


                CongViecHienTai = cv == null ? null : new NvCongViecDto
                {
                    Id = cv.CV.Id,
                    NvHoSoId = cv.CV.NvHoSoId,

                    // dùng Convert.* để không bị lỗi nếu field trong entity là nullable/non-nullable
                    PhongBanId = Convert.ToInt32(cv.CV.PhongBanId),
                    TenPhongBan = cv.TenPhongBan,

                    ChucVuId = Convert.ToInt32(cv.CV.ChucVuId),
                    TenChucVu = cv.TenChucVu,

                    NgayVaoLam = Convert.ToDateTime(cv.CV.NgayVaoLam),
                    NgayNghiViec = cv.CV.NgayNghiViec,

                    LoaiHopDong = cv.CV.LoaiHopDong,
                    NgayKyHopDong = cv.CV.NgayKyHopDong,
                    NgayHetHanHopDong = cv.CV.NgayHetHanHopDong,

                    TrangThaiLamViec = Convert.ToByte(cv.CV.TrangThaiLamViec),
                    GhiChu = cv.CV.GhiChu
                }
            };
            return dto;
        }
        public async Task<bool> UpdateMyBankAccountAsync(int nvHoSoId,string? soTaiKhoan)
        {
            soTaiKhoan = string.IsNullOrWhiteSpace(soTaiKhoan) ? null : soTaiKhoan.Trim();
            if(soTaiKhoan != null)
            {
                if (soTaiKhoan.Length < 6 || soTaiKhoan.Length > 50)
                    throw new InvalidOperationException("số tài khoản không hợp lệ(6-50 kí tự)");
                if (!soTaiKhoan.All(char.IsDigit))
                    throw new InvalidOperationException("Số tài khoản chỉ được chứa chữ số");
            }
            var nv = await _context.NvHoSos.FirstOrDefaultAsync(x => x.Id == nvHoSoId);
            if (nv == null) return false;

            nv.SoTaiKhoanNganHang = soTaiKhoan;
            nv.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> UpdateMyAvatarUrlAsync(int nvHoSoId,string avatarUrl)
        {
            if (string.IsNullOrWhiteSpace(avatarUrl))
                throw new InvalidOperationException("Url không hợp lệ");
            var nv = await _context.NvHoSos.FirstOrDefaultAsync(x => x.Id == nvHoSoId);
            if (nv == null) return false;
            nv.AnhCaNhanUrl = avatarUrl;
            nv.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAnhStkAsync(int nvHoSoId, string anhStkUrl)
        {
            if (string.IsNullOrWhiteSpace(anhStkUrl))
                throw new InvalidOperationException("URL không hợp lệ");

            var nv = await _context.NvHoSos.FirstOrDefaultAsync(x => x.Id == nvHoSoId);
            if (nv == null) return false;

            nv.AnhStkUrl = anhStkUrl;
            nv.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateHopDongAsync(int nvHoSoId, string hopDongUrl)
        {
            if (string.IsNullOrWhiteSpace(hopDongUrl))
                throw new InvalidOperationException("URL hợp đồng không hợp lệ");

            var nv = await _context.NvHoSos.FirstOrDefaultAsync(x => x.Id == nvHoSoId);
            if (nv == null) return false;

            nv.HopDongUrl = hopDongUrl;
            nv.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
