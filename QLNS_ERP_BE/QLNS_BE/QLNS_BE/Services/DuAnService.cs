using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;
using QLNS_BE.Models.Dtos.DuAn;
using QLNS_BE.Models.Entities;
using System.Security.Claims;

namespace QLNS_BE.Services
{
    public class DuAnService
    {
        private readonly AppDbContext _context;

        public DuAnService(AppDbContext context)
        {
            _context = context;
        }

        // ========================= 1) LẤY DANH SÁCH =========================
        public async Task<List<DuAnListItemDto>> GetListAsync()
        {
            var data =
                from da in _context.DuAns
                join nv in _context.NvHoSos on da.NvPhuTrachId equals nv.Id into nvJoin
                from nv in nvJoin.DefaultIfEmpty()
                orderby da.CreatedAt descending
                select new DuAnListItemDto
                {
                    Id = da.Id,
                    MaDuAn = da.MaDuAn,
                    TenDuAn = da.TenDuAn,
                    TrangThaiDuAn = da.TrangThaiDuAn,
                    TenNhanVienPhuTrach = nv != null ? nv.HoTen : null,
                    NgayBatDau = da.NgayBatDau,
                    NgayKetThuc = da.NgayKetThuc,
                    TepTinDinhKemUrl = da.TepTinDinhKemUrl,
                    TepTinDinhKemTenGoc = da.TepTinDinhKemTenGoc,
                };

            return await data.ToListAsync();
        }

        // ========================= 2) CHI TIẾT =========================
        public async Task<DuAnDetailDto?> GetDetailAsync(int id)
        {
            var duAn = await _context.DuAns
                .Include(x => x.NvPhuTrach)
                .Include(x => x.ThanhViens).ThenInclude(tv => tv.NvHoSo)
                .Include(x => x.NhatKyTrangThais).ThenInclude(nk => nk.TaiKhoanThucHien)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (duAn == null) return null;

            // Load files separately to avoid Include issue
            var files = await GetFilesAsync(id);

            return new DuAnDetailDto
            {
                Id = duAn.Id,
                MaDuAn = duAn.MaDuAn,
                TenDuAn = duAn.TenDuAn,
                MoTa = duAn.MoTa,
                NganSach = duAn.NganSach,
                TrangThaiDuAn = duAn.TrangThaiDuAn,
                NvPhuTrachId = duAn.NvPhuTrachId,
                TenNvPhuTrach = duAn.NvPhuTrach?.HoTen,
                NgayGuiDuyet = duAn.NgayGuiDuyet,
                NgayDuyet = duAn.NgayDuyet,
                LyDoTuChoi = duAn.LyDoTuChoi,
                NgayBatDau = duAn.NgayBatDau,
                NgayKetThuc = duAn.NgayKetThuc,
                // ADD
                TepTinDinhKemUrl = duAn.TepTinDinhKemUrl,
                TepTinDinhKemTenGoc = duAn.TepTinDinhKemTenGoc,
                TepTinDinhKemMime = duAn.TepTinDinhKemMime,
                TepTinDinhKemSize = duAn.TepTinDinhKemSize,
                TaiKhoanTaoId = duAn.TaiKhoanTaoId, // For notification

                ThanhViens = duAn.ThanhViens.Select(tv => new DuAnThanhVienDto
                {
                    Id = tv.Id,
                    NvHoSoId = tv.NvHoSoId,
                    HoTen = tv.NvHoSo.HoTen,
                    VaiTroTrongDuAn = tv.VaiTroTrongDuAn,
                    NgayThamGia = tv.NgayThamGia,
                    NgayRoiDi = tv.NgayRoiDi
                }).ToList(),

                NhatKyTrangThais = duAn.NhatKyTrangThais
                    .OrderByDescending(x => x.ThoiGian)
                    .Select(nk => new DuAnNhatKyTrangThaiDto
                    {
                        TrangThaiCu = nk.TrangThaiCu,
                        TrangThaiMoi = nk.TrangThaiMoi,
                        GhiChu = nk.GhiChu,
                        ThoiGian = nk.ThoiGian,
                        NguoiThucHien = nk.TaiKhoanThucHien.TenDangNhap
                    }).ToList(),

                Files = files
            };
        }

        // ========================= 3) TẠO DỰ ÁN =========================
        public async Task<int> CreateAsync(DuAnCreateDto dto, int taiKhoanId)
        {
            var duAn = new DuAn
            {
                MaDuAn = dto.MaDuAn,
                TenDuAn = dto.TenDuAn,
                MoTa = dto.MoTa,
                NganSach = dto.NganSach,
                NgayBatDau = dto.NgayBatDau,
                NgayKetThuc = dto.NgayKetThuc,
                NvPhuTrachId = dto.NvPhuTrachId,
                TrangThaiDuAn = "DANG_NHAP",
                TaiKhoanTaoId = taiKhoanId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.DuAns.Add(duAn);
            await _context.SaveChangesAsync();

            return duAn.Id;
        }

        // ========================= 4) CẬP NHẬT =========================
        public async Task<bool> UpdateAsync(int id, DuAnUpdateDto dto)
        {
            var duAn = await _context.DuAns.FirstOrDefaultAsync(x => x.Id == id);
            if (duAn == null) return false;

            duAn.TenDuAn = dto.TenDuAn;
            duAn.MoTa = dto.MoTa;
            duAn.NganSach = dto.NganSach;
            duAn.NgayBatDau = dto.NgayBatDau;
            duAn.NgayKetThuc = dto.NgayKetThuc;
            duAn.NvPhuTrachId = dto.NvPhuTrachId;
            duAn.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        // ========================= 5) GỬI DUYỆT =========================
        public async Task<bool> GuiDuyetAsync(int id, DuAnGuiDuyetRequestDto dto, int taiKhoanId)
        {
            var duAn = await _context.DuAns.FirstOrDefaultAsync(x => x.Id == id);
            if (duAn == null) return false;

            if (duAn.TrangThaiDuAn != "DANG_NHAP" && duAn.TrangThaiDuAn != "TU_CHOI")
                throw new Exception("Chỉ dự án ở trạng thái 'Nháp' hoặc 'Từ chối' mới được gửi duyệt!");

            var trangThaiCu = duAn.TrangThaiDuAn;
            duAn.TrangThaiDuAn = "CHO_DUYET_GIAM_DOC";
            duAn.NgayGuiDuyet = DateTime.UtcNow;

            // nhật ký
            _context.DuAnNhatKyTrangThais.Add(new DuAnNhatKyTrangThai
            {
                DuAnId = id,
                TrangThaiCu = trangThaiCu,
                TrangThaiMoi = "CHO_DUYET_GIAM_DOC",
                GhiChu = dto.GhiChu,
                TaiKhoanThucHienId = taiKhoanId,
                ThoiGian = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return true;
        }

        // ========================= 6) GIÁM ĐỐC DUYỆT =========================
        public async Task<bool> ApproveAsync(int id, DuAnApproveRequestDto dto, int taiKhoanId)
        {
            var duAn = await _context.DuAns.FirstOrDefaultAsync(x => x.Id == id);
            if (duAn == null) return false;

            if (duAn.TrangThaiDuAn != "CHO_DUYET_GIAM_DOC")
                throw new Exception("Dự án không ở trạng thái chờ duyệt!");

            string trangThaiCu = duAn.TrangThaiDuAn;

            if (dto.DongY)
            {
                duAn.TrangThaiDuAn = "DA_DUYET";
                duAn.NgayDuyet = DateTime.UtcNow;
            }
            else
            {
                duAn.TrangThaiDuAn = "TU_CHOI";
                duAn.LyDoTuChoi = dto.LyDoTuChoi;
            }

            duAn.TaiKhoanDuyetId = taiKhoanId;
            duAn.UpdatedAt = DateTime.UtcNow;

            // nhật ký
            _context.DuAnNhatKyTrangThais.Add(new DuAnNhatKyTrangThai
            {
                DuAnId = id,
                TrangThaiCu = trangThaiCu,
                TrangThaiMoi = duAn.TrangThaiDuAn,
                GhiChu = dto.LyDoTuChoi,
                TaiKhoanThucHienId = taiKhoanId,
                ThoiGian = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return true;
        }

        // ========================= 7) LIST DỰ ÁN TÔI ĐÃ DUYỆT =========================
        public async Task<List<DuAnMyApprovedListDto>> GetMyApprovedListAsync(int taiKhoanId)
        {
            var list = await _context.DuAns
                .Where(x => x.TaiKhoanDuyetId == taiKhoanId && x.TrangThaiDuAn == "DA_DUYET")
                .Select(x => new DuAnMyApprovedListDto
                {
                    Id = x.Id,
                    MaDuAn = x.MaDuAn,
                    TenDuAn = x.TenDuAn,
                    NgayDuyet = x.NgayDuyet!.Value,
                    TrangThaiDuAn = x.TrangThaiDuAn
                })
                .ToListAsync();

            return list;
        }
        /// <summary>
        /// Thêm mới thành viên cho dự án
        /// </summary>
        /// <param name="duAnId"></param>
        /// <param name="dto"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<bool> AddMemberAsync(int duAnId, DuAnAddMemberDto dto)
        {
            var duAn = await _context.DuAns
                .Include(x => x.ThanhViens)
                .FirstOrDefaultAsync(x => x.Id == duAnId);

            if (duAn == null) return false;

            // Kiểm tra đã tồn tại chưa
            bool exists = duAn.ThanhViens.Any(x => x.NvHoSoId == dto.NvHoSoId);
            if (exists)
                throw new InvalidOperationException("Nhân viên đã có trong dự án");

            var thanhVien = new DuAnThanhVien
            {
                DuAnId = duAnId,
                NvHoSoId = dto.NvHoSoId,
                VaiTroTrongDuAn = dto.VaiTroTrongDuAn,
                NgayThamGia = DateTime.UtcNow,
            };

            _context.DuAnThanhViens.Add(thanhVien);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// thay vai trò của thành viên trong dự án
        /// </summary>
        /// <param name="duAnId"></param>
        /// <param name="nvId"></param>
        /// <param name="newRole"></param>
        /// <returns></returns>
        public async Task<bool> UpdateMemberRoleAsync(int duAnId, int nvId, string newRole)
        {
            var mem = await _context.DuAnThanhViens
                .FirstOrDefaultAsync(x => x.DuAnId == duAnId && x.NvHoSoId == nvId);

            if (mem == null) return false;

            mem.VaiTroTrongDuAn = newRole;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Xóa thành viên trong dự án
        /// </summary>
        /// <param name="duAnId"></param>
        /// <param name="nvId"></param>
        /// <returns></returns>
        public async Task<bool> RemoveMemberAsync(int duAnId, int nvId)
        {
            var mem = await _context.DuAnThanhViens
                .FirstOrDefaultAsync(x => x.DuAnId == duAnId && x.NvHoSoId == nvId);

            if (mem == null) return false;

            _context.DuAnThanhViens.Remove(mem);
            await _context.SaveChangesAsync();
            return true;
        }
        // ========================= 1.1) LẤY DANH SÁCH DỰ ÁN CỦA TÔI (EMPLOYEE) =========================
        public async Task<List<DuAnMyListItemDto>> GetMyListAsync(int nvHoSoId)
        {
            var data =
                from tv in _context.DuAnThanhViens
                join da in _context.DuAns on tv.DuAnId equals da.Id
                join nv in _context.NvHoSos on da.NvPhuTrachId equals nv.Id into nvJoin
                from nv in nvJoin.DefaultIfEmpty()
                where tv.NvHoSoId == nvHoSoId && tv.NgayRoiDi == null
                orderby da.CreatedAt descending
                select new DuAnMyListItemDto
                {
                    Id = da.Id,
                    MaDuAn = da.MaDuAn,
                    TenDuAn = da.TenDuAn,
                    TrangThaiDuAn = da.TrangThaiDuAn,
                    TenNhanVienPhuTrach = nv != null ? nv.HoTen : null,
                    NgayBatDau = da.NgayBatDau,
                    NgayKetThuc = da.NgayKetThuc,
                    VaiTroTrongDuAn = tv.VaiTroTrongDuAn,
                    NgayThamGia = tv.NgayThamGia,
                    TepTinDinhKemUrl = da.TepTinDinhKemUrl,
                    TepTinDinhKemTenGoc = da.TepTinDinhKemTenGoc,
                };

            return await data.ToListAsync();
        }
        public async Task<bool> UpdateAttachmentAsync(int duAnId, string fileUrl, string tenGoc, string mine, long size)
        {
            var duAn = await _context.DuAns.FirstOrDefaultAsync(x => x.Id == duAnId);
            if (duAn == null) return false;
            if (duAn.TrangThaiDuAn != "DANG_NHAP")
                throw new InvalidOperationException("chỉ được uploadfile khi dự án đang nhập");
            duAn.TepTinDinhKemUrl = fileUrl;
            duAn.TepTinDinhKemTenGoc = tenGoc;
            duAn.TepTinDinhKemMime = mine;
            duAn.TepTinDinhKemSize = size;
            duAn.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        // ============================================================
        // MULTI-FILE MANAGEMENT
        // ============================================================
        public async Task<DuAnFile?> AddFileAsync(int duAnId, string tenFile, string duongDan, long? kichThuoc, string? loaiFile, int taiKhoanId)
        {
            var duAn = await _context.DuAns.FindAsync(duAnId);
            if (duAn == null) return null;

            var file = new DuAnFile
            {
                DuAnId = duAnId,
                TenFile = tenFile,
                DuongDanFile = duongDan,
                KichThuoc = kichThuoc,
                LoaiFile = loaiFile,
                NgayTao = DateTime.UtcNow,
                TaiKhoanTaoId = taiKhoanId
            };

            _context.DuAnFiles.Add(file);
            await _context.SaveChangesAsync();
            return file;
        }

        public async Task<bool> DeleteFileAsync(int fileId, int taiKhoanId)
        {
            var file = await _context.DuAnFiles.FindAsync(fileId);
            if (file == null) return false;

            _context.DuAnFiles.Remove(file);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<DuAnFileDto>> GetFilesAsync(int duAnId)
        {
            return await _context.DuAnFiles
                .Where(f => f.DuAnId == duAnId)
                .Include(f => f.TaiKhoanTao)
                .OrderByDescending(f => f.NgayTao)
                .Select(f => new DuAnFileDto
                {
                    Id = f.Id,
                    DuAnId = f.DuAnId,
                    TenFile = f.TenFile,
                    DuongDanFile = f.DuongDanFile,
                    KichThuoc = f.KichThuoc,
                    LoaiFile = f.LoaiFile,
                    NgayTao = f.NgayTao,
                    TaiKhoanTaoId = f.TaiKhoanTaoId,
                    TenNguoiTao = f.TaiKhoanTao != null ? f.TaiKhoanTao.TenDangNhap : null
                })
                .ToListAsync();
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
        public async Task<bool> ThuHoiAsync(int id, int userId)
        {
            var duAn = await _context.DuAns
                .Include(d => d.NhatKyTrangThais) // Fixed: NhatKies -> NhatKyTrangThais
                .FirstOrDefaultAsync(d => d.Id == id);

            if (duAn == null) throw new Exception("Không tìm thấy dự án.");

            // Chỉ cho thu hồi khi đang chờ duyệt
            if (duAn.TrangThaiDuAn != "CHO_DUYET_GIAM_DOC")
            {
                throw new Exception("Dự án không ở trạng thái chờ duyệt, không thể thu hồi.");
            }

            // Update status
            var oldStatus = duAn.TrangThaiDuAn;
            duAn.TrangThaiDuAn = "DANG_NHAP";
            duAn.UpdatedAt = DateTime.UtcNow;

            // Log history
            duAn.NhatKyTrangThais.Add(new DuAnNhatKyTrangThai
            {
                TrangThaiCu = oldStatus,
                TrangThaiMoi = "DANG_NHAP",
                TaiKhoanThucHienId = userId, // Fixed: NguoiThucHienId -> TaiKhoanThucHienId
                ThoiGian = DateTime.UtcNow,
                GhiChu = "Thu hồi yêu cầu duyệt"
            });

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
