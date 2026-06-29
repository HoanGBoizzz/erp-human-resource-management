using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;
using QLNS_BE.Models.Dtos.PhongBan;
using QLNS_BE.Models.Entities;

namespace QLNS_BE.Services
{
    public class PhongBanService
    {
        private readonly AppDbContext _context;
        private readonly ThongBaoService _thongBaoService;

        public PhongBanService(AppDbContext context, ThongBaoService thongBaoService)
        {
            _context = context;
            _thongBaoService = thongBaoService;
        }

        /// <summary>
        /// Lấy danh sách tất cả phòng ban với thống kê số nhân viên
        /// </summary>
        public async Task<List<PhongBanListDto>> GetAllAsync()
        {
            var result = await (
                from pb in _context.PhongBans.AsNoTracking()
                join pbCha in _context.PhongBans.AsNoTracking()
                    on pb.PhongBanChaId equals pbCha.Id into pbChaJoin
                from pbCha in pbChaJoin.DefaultIfEmpty()
                select new PhongBanListDto
                {
                    Id = pb.Id,
                    MaPhongBan = pb.MaPhongBan,
                    TenPhongBan = pb.TenPhongBan,
                    PhongBanChaId = pb.PhongBanChaId,
                    TenPhongBanCha = pbCha != null ? pbCha.TenPhongBan : null,
                    TrangThai = pb.TrangThai,
                    GhiChu = pb.GhiChu,
                    // Đếm số NV đang làm (TrangThaiLamViec = 1)
                    SoNhanVienDangLam = _context.NvCongViecs
                        .Count(cv => cv.PhongBanId == pb.Id && cv.TrangThaiLamViec == 1),
                    // Đếm tổng số NV (bao gồm cả nghỉ việc)
                    TongNhanVien = _context.NvCongViecs
                        .Count(cv => cv.PhongBanId == pb.Id)
                }
            ).OrderBy(x => x.TenPhongBan).ToListAsync();

            return result;
        }

        /// <summary>
        /// Lấy chi tiết phòng ban kèm danh sách nhân viên
        /// </summary>
        public async Task<PhongBanDetailDto?> GetByIdAsync(int id)
        {
            var pb = await _context.PhongBans
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (pb == null) return null;

            // Lấy tên phòng ban cha nếu có
            string? tenPbCha = null;
            if (pb.PhongBanChaId.HasValue)
            {
                tenPbCha = await _context.PhongBans
                    .Where(x => x.Id == pb.PhongBanChaId)
                    .Select(x => x.TenPhongBan)
                    .FirstOrDefaultAsync();
            }

            // Lấy danh sách nhân viên trong phòng ban
            var danhSachNv = await (
                from cv in _context.NvCongViecs.AsNoTracking()
                join nv in _context.NvHoSos.AsNoTracking() on cv.NvHoSoId equals nv.Id
                join chucVu in _context.ChucVus.AsNoTracking() on cv.ChucVuId equals chucVu.Id into chucVuJoin
                from chucVu in chucVuJoin.DefaultIfEmpty()
                where cv.PhongBanId == id
                orderby cv.TrangThaiLamViec descending, nv.HoTen
                select new NhanVienTrongPhongBanDto
                {
                    NvHoSoId = nv.Id,
                    NvCongViecId = cv.Id,
                    MaNhanVien = nv.MaNhanVien,
                    HoTen = nv.HoTen,
                    TenChucVu = chucVu != null ? chucVu.TenChucVu : null,
                    TrangThaiLamViec = cv.TrangThaiLamViec,
                    NgayVaoLam = cv.NgayVaoLam
                }
            ).ToListAsync();

            return new PhongBanDetailDto
            {
                Id = pb.Id,
                MaPhongBan = pb.MaPhongBan,
                TenPhongBan = pb.TenPhongBan,
                PhongBanChaId = pb.PhongBanChaId,
                TenPhongBanCha = tenPbCha,
                TrangThai = pb.TrangThai,
                GhiChu = pb.GhiChu,
                DanhSachNhanVien = danhSachNv
            };
        }

        /// <summary>
        /// Tạo mới phòng ban
        /// </summary>
        public async Task<PhongBanListDto> CreateAsync(PhongBanCreateDto dto)
        {
            // Kiểm tra mã phòng ban đã tồn tại chưa
            var exists = await _context.PhongBans.AnyAsync(x => x.MaPhongBan == dto.MaPhongBan);
            if (exists)
                throw new InvalidOperationException($"Mã phòng ban '{dto.MaPhongBan}' đã tồn tại");

            // Kiểm tra phòng ban cha có tồn tại không
            if (dto.PhongBanChaId.HasValue)
            {
                var parentExists = await _context.PhongBans.AnyAsync(x => x.Id == dto.PhongBanChaId);
                if (!parentExists)
                    throw new InvalidOperationException("Phòng ban cha không tồn tại");
            }

            var entity = new PhongBan
            {
                MaPhongBan = dto.MaPhongBan.Trim(),
                TenPhongBan = dto.TenPhongBan.Trim(),
                PhongBanChaId = dto.PhongBanChaId,
                TrangThai = true,
                GhiChu = dto.GhiChu?.Trim()
            };

            _context.PhongBans.Add(entity);
            await _context.SaveChangesAsync();

            // Lấy tên phòng ban cha
            string? tenPbCha = null;
            if (entity.PhongBanChaId.HasValue)
            {
                tenPbCha = await _context.PhongBans
                    .Where(x => x.Id == entity.PhongBanChaId)
                    .Select(x => x.TenPhongBan)
                    .FirstOrDefaultAsync();
            }

            return new PhongBanListDto
            {
                Id = entity.Id,
                MaPhongBan = entity.MaPhongBan,
                TenPhongBan = entity.TenPhongBan,
                PhongBanChaId = entity.PhongBanChaId,
                TenPhongBanCha = tenPbCha,
                TrangThai = entity.TrangThai,
                GhiChu = entity.GhiChu,
                SoNhanVienDangLam = 0,
                TongNhanVien = 0
            };
        }

        /// <summary>
        /// Cập nhật phòng ban
        /// </summary>
        public async Task<PhongBanListDto?> UpdateAsync(int id, PhongBanUpdateDto dto)
        {
            var entity = await _context.PhongBans.FindAsync(id);
            if (entity == null) return null;

            // Không cho phép chọn chính mình làm phòng ban cha
            if (dto.PhongBanChaId == id)
                throw new InvalidOperationException("Phòng ban không thể là con của chính nó");

            // Kiểm tra phòng ban cha có tồn tại không
            if (dto.PhongBanChaId.HasValue)
            {
                var parentExists = await _context.PhongBans.AnyAsync(x => x.Id == dto.PhongBanChaId);
                if (!parentExists)
                    throw new InvalidOperationException("Phòng ban cha không tồn tại");
            }

            entity.TenPhongBan = dto.TenPhongBan.Trim();
            entity.PhongBanChaId = dto.PhongBanChaId;
            entity.TrangThai = dto.TrangThai;
            entity.GhiChu = dto.GhiChu?.Trim();

            await _context.SaveChangesAsync();

            // Lấy thông tin để trả về
            string? tenPbCha = null;
            if (entity.PhongBanChaId.HasValue)
            {
                tenPbCha = await _context.PhongBans
                    .Where(x => x.Id == entity.PhongBanChaId)
                    .Select(x => x.TenPhongBan)
                    .FirstOrDefaultAsync();
            }

            var soNvDangLam = await _context.NvCongViecs
                .CountAsync(cv => cv.PhongBanId == id && cv.TrangThaiLamViec == 1);
            var tongNv = await _context.NvCongViecs
                .CountAsync(cv => cv.PhongBanId == id);

            return new PhongBanListDto
            {
                Id = entity.Id,
                MaPhongBan = entity.MaPhongBan,
                TenPhongBan = entity.TenPhongBan,
                PhongBanChaId = entity.PhongBanChaId,
                TenPhongBanCha = tenPbCha,
                TrangThai = entity.TrangThai,
                GhiChu = entity.GhiChu,
                SoNhanVienDangLam = soNvDangLam,
                TongNhanVien = tongNv
            };
        }

        /// <summary>
        /// Xóa phòng ban (không cho xóa nếu còn nhân viên)
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.PhongBans.FindAsync(id);
            if (entity == null) return false;

            // Kiểm tra còn nhân viên không
            var hasEmployees = await _context.NvCongViecs.AnyAsync(cv => cv.PhongBanId == id);
            if (hasEmployees)
                throw new InvalidOperationException("Không thể xóa phòng ban còn nhân viên. Vui lòng chuyển nhân viên sang phòng ban khác trước.");

            // Kiểm tra có phòng ban con không
            var hasChildren = await _context.PhongBans.AnyAsync(pb => pb.PhongBanChaId == id);
            if (hasChildren)
                throw new InvalidOperationException("Không thể xóa phòng ban có phòng ban con.");

            _context.PhongBans.Remove(entity);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Điều chuyển nhân viên sang phòng ban khác
        /// </summary>
        public async Task<bool> ChuyenNhanVienAsync(ChuyenPhongBanDto dto, int taiKhoanDieuChuyenId = 0)
        {
            var congViec = await _context.NvCongViecs
                .Include(cv => cv.NvHoSo!).ThenInclude(h => h.TaiKhoans)
                .FirstOrDefaultAsync(cv => cv.Id == dto.NvCongViecId);
            if (congViec == null)
                throw new InvalidOperationException("Không tìm thấy công việc nhân viên");

            // Kiểm tra phòng ban mới có tồn tại không
            var pbMoi = await _context.PhongBans.FindAsync(dto.PhongBanMoiId);
            if (pbMoi == null)
                throw new InvalidOperationException("Phòng ban mới không tồn tại");

            if (!pbMoi.TrangThai)
                throw new InvalidOperationException("Phòng ban mới đã ngừng hoạt động");

            // Nếu đã ở phòng ban đó rồi thì không làm gì
            if (congViec.PhongBanId == dto.PhongBanMoiId)
                return true;

            congViec.PhongBanId = dto.PhongBanMoiId;
            await _context.SaveChangesAsync();

            // Gửi thông báo cho nhân viên
            if (congViec.NvHoSo?.TaiKhoans != null)
            {
                foreach (var tk in congViec.NvHoSo.TaiKhoans)
                {
                    await _thongBaoService.CreateAndPushAsync(
                        userId: tk.Id,
                        title: "📌 Bạn đã được điều chuyển phòng ban",
                        message: $"Bạn đã được điều chuyển sang phòng ban \u201c{pbMoi.TenPhongBan}\u201d kể từ hôm nay.",
                        type: "DIEU_CHUYEN",
                        relatedEntity: "NvCongViec",
                        relatedId: congViec.Id,
                        senderId: taiKhoanDieuChuyenId > 0 ? taiKhoanDieuChuyenId : null
                    );
                }
            }

            return true;
        }

        /// <summary>
        /// Xóa nhân viên nghỉ việc khỏi phòng ban (xóa bản ghi NvCongViec)
        /// Chỉ áp dụng cho nhân viên đã nghỉ việc (TrangThaiLamViec != 1)
        /// </summary>
        public async Task XoaNhanVienKhoiPhongBanAsync(int nvCongViecId)
        {
            var congViec = await _context.NvCongViecs.FindAsync(nvCongViecId);
            if (congViec == null)
                throw new InvalidOperationException("Không tìm thấy bản ghi công việc nhân viên");

            if (congViec.TrangThaiLamViec == 1)
                throw new InvalidOperationException("Không thể xóa nhân viên đang làm việc khỏi phòng ban");

            // Xóa bản ghi NvCongViec – không thể NULL PhongBanId vì cột NOT NULL có FK constraint
            _context.NvCongViecs.Remove(congViec);
            await _context.SaveChangesAsync();
        }
    }
}
