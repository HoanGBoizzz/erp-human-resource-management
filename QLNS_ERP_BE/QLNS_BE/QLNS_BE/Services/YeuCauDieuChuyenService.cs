using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;
using QLNS_BE.Models.Dtos.PhongBan;
using QLNS_BE.Models.Entities;

namespace QLNS_BE.Services
{
    /// <summary>
    /// Service xử lý yêu cầu điều chuyển phòng ban
    /// </summary>
    public class YeuCauDieuChuyenService
    {
        private readonly AppDbContext _context;
        private readonly ThongBaoService _thongBaoService;

        public YeuCauDieuChuyenService(AppDbContext context, ThongBaoService thongBaoService)
        {
            _context = context;
            _thongBaoService = thongBaoService;
        }

        /// <summary>
        /// Lấy danh sách yêu cầu điều chuyển (cho cả HR và Giám đốc)
        /// </summary>
        public async Task<List<YeuCauDieuChuyenListDto>> GetAllAsync(int? trangThai = null)
        {
            var query = _context.YeuCauDieuChuyens
                .Include(y => y.NvCongViec).ThenInclude(c => c!.NvHoSo)
                .Include(y => y.PhongBanCu)
                .Include(y => y.PhongBanMoi)
                .Include(y => y.TaiKhoanTao).ThenInclude(t => t!.NvHoSo)
                .Include(y => y.TaiKhoanDuyet).ThenInclude(t => t!.NvHoSo)
                .AsNoTracking();

            if (trangThai.HasValue)
            {
                query = query.Where(y => y.TrangThai == trangThai.Value);
            }

            var result = await query
                .OrderByDescending(y => y.NgayTao)
                .Select(y => new YeuCauDieuChuyenListDto
                {
                    Id = y.Id,
                    MaNhanVien = y.NvCongViec!.NvHoSo!.MaNhanVien ?? "",
                    HoTenNhanVien = y.NvCongViec.NvHoSo.HoTen ?? "",
                    TenPhongBanCu = y.PhongBanCu!.TenPhongBan ?? "",
                    TenPhongBanMoi = y.PhongBanMoi!.TenPhongBan ?? "",
                    LyDo = y.LyDo,
                    TrangThai = y.TrangThai,
                    TenNguoiTao = y.TaiKhoanTao!.NvHoSo != null ? y.TaiKhoanTao.NvHoSo.HoTen ?? "" : y.TaiKhoanTao.TenDangNhap ?? "",
                    NgayTao = y.NgayTao,
                    TenNguoiDuyet = y.TaiKhoanDuyet != null ? (y.TaiKhoanDuyet.NvHoSo != null ? y.TaiKhoanDuyet.NvHoSo.HoTen : y.TaiKhoanDuyet.TenDangNhap) : null,
                    NgayDuyet = y.NgayDuyet,
                    GhiChuDuyet = y.GhiChuDuyet
                })
                .ToListAsync();

            return result;
        }

        /// <summary>
        /// Lấy các yêu cầu chờ duyệt (cho Giám đốc)
        /// </summary>
        public async Task<List<YeuCauDieuChuyenListDto>> GetPendingAsync()
        {
            return await GetAllAsync(0);
        }

        /// <summary>
        /// HR tạo yêu cầu điều chuyển
        /// </summary>
        public async Task<YeuCauDieuChuyen> CreateAsync(TaoYeuCauDieuChuyenDto dto, int taiKhoanTaoId)
        {
            // Validate NvCongViec tồn tại và đang làm việc
            var nvCongViec = await _context.NvCongViecs
                .Include(c => c.NvHoSo)
                .Include(c => c.PhongBan)
                .FirstOrDefaultAsync(c => c.Id == dto.NvCongViecId);

            if (nvCongViec == null)
                throw new InvalidOperationException("Không tìm thấy thông tin công việc của nhân viên");

            if (nvCongViec.TrangThaiLamViec != 1)
                throw new InvalidOperationException("Nhân viên này đã nghỉ việc, không thể điều chuyển");

            // Validate phòng ban mới tồn tại và hoạt động
            var phongBanMoi = await _context.PhongBans.FirstOrDefaultAsync(p => p.Id == dto.PhongBanMoiId);
            if (phongBanMoi == null || !phongBanMoi.TrangThai)
                throw new InvalidOperationException("Phòng ban mới không tồn tại hoặc đã ngừng hoạt động");

            if (nvCongViec.PhongBanId == dto.PhongBanMoiId)
                throw new InvalidOperationException("Phòng ban mới phải khác phòng ban hiện tại");

            // Check xem có yêu cầu pending cho NV này chưa
            var existingPending = await _context.YeuCauDieuChuyens
                .AnyAsync(y => y.NvCongViecId == dto.NvCongViecId && y.TrangThai == 0);
            if (existingPending)
                throw new InvalidOperationException("Nhân viên này đang có yêu cầu điều chuyển chờ duyệt");

            var yeuCau = new YeuCauDieuChuyen
            {
                NvCongViecId = dto.NvCongViecId,
                PhongBanCuId = nvCongViec.PhongBanId,
                PhongBanMoiId = dto.PhongBanMoiId,
                LyDo = dto.LyDo,
                TrangThai = 0, // Chờ duyệt
                TaiKhoanTaoId = taiKhoanTaoId,
                NgayTao = DateTime.Now
            };

            _context.YeuCauDieuChuyens.Add(yeuCau);
            await _context.SaveChangesAsync();

            return yeuCau;
        }

        /// <summary>
        /// Giám đốc duyệt/từ chối yêu cầu
        /// </summary>
        public async Task<bool> DuyetAsync(DuyetYeuCauDieuChuyenDto dto, int taiKhoanDuyetId)
        {
            var yeuCau = await _context.YeuCauDieuChuyens
                .Include(y => y.NvCongViec)
                    .ThenInclude(c => c!.NvHoSo)
                        .ThenInclude(h => h!.TaiKhoans)
                .Include(y => y.PhongBanMoi)
                .FirstOrDefaultAsync(y => y.Id == dto.YeuCauId);

            if (yeuCau == null)
                throw new InvalidOperationException("Không tìm thấy yêu cầu điều chuyển");

            if (yeuCau.TrangThai != 0)
                throw new InvalidOperationException("Yêu cầu này đã được xử lý trước đó");

            yeuCau.TaiKhoanDuyetId = taiKhoanDuyetId;
            yeuCau.NgayDuyet = DateTime.Now;
            yeuCau.GhiChuDuyet = dto.GhiChu;

            if (dto.Duyet)
            {
                // Duyệt - thực hiện điều chuyển
                yeuCau.TrangThai = 1;

                // Cập nhật phòng ban trong NvCongViec
                if (yeuCau.NvCongViec != null)
                {
                    yeuCau.NvCongViec.PhongBanId = yeuCau.PhongBanMoiId;
                }
            }
            else
            {
                // Từ chối
                yeuCau.TrangThai = 2;
            }

            await _context.SaveChangesAsync();

            // Gửi thông báo cho nhân viên sau khi duyệt
            if (dto.Duyet && yeuCau.NvCongViec?.NvHoSo != null)
            {
                var hoTen = yeuCau.NvCongViec.NvHoSo.HoTen ?? "Nhân viên";
                var tenPBMoi = yeuCau.PhongBanMoi?.TenPhongBan ?? "phòng ban mới";
                var ghiChu = string.IsNullOrWhiteSpace(dto.GhiChu) ? "" : $" Ghi chú: {dto.GhiChu}.";

                foreach (var taiKhoan in yeuCau.NvCongViec.NvHoSo.TaiKhoans)
                {
                    await _thongBaoService.CreateAndPushAsync(
                        userId: taiKhoan.Id,
                        title: "📌 Bạn đã được điều chuyển phòng ban",
                        message: $"Bạn đã được điều chuyển sang phòng ban “{tenPBMoi}” kể từ hôm nay.{ghiChu}",
                        type: "DIEU_CHUYEN",
                        relatedEntity: "YeuCauDieuChuyen",
                        relatedId: yeuCau.Id,
                        senderId: taiKhoanDuyetId
                    );
                }
            }

            return dto.Duyet;
        }

        /// <summary>
        /// Hủy yêu cầu điều chuyển (cho HR)
        /// </summary>
        public async Task<bool> CancelAsync(int yeuCauId, int taiKhoanHuyId)
        {
            var yeuCau = await _context.YeuCauDieuChuyens
                .FirstOrDefaultAsync(y => y.Id == yeuCauId);

            if (yeuCau == null)
                throw new InvalidOperationException("Không tìm thấy yêu cầu điều chuyển");

            if (yeuCau.TrangThai != 0)
                throw new InvalidOperationException("Chỉ có thể hủy yêu cầu đang chờ duyệt");

            // Chỉ cho phép người tạo hủy
            if (yeuCau.TaiKhoanTaoId != taiKhoanHuyId)
                throw new InvalidOperationException("Bạn không có quyền hủy yêu cầu này");

            _context.YeuCauDieuChuyens.Remove(yeuCau);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
