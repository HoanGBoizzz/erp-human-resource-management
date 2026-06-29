using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;
using QLNS_BE.Models.Dtos.ChamCong;
using QLNS_BE.Models.Dtos.FaceRecognition;
using QLNS_BE.Models.Dtos.Luong;
using QLNS_BE.Models.Dtos;
using QLNS_BE.Models.Entities;
using QLNS_BE.Services.FaceRecognition;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;

namespace QLNS_BE.Services
{
    public class ChamCongService
    {
        private readonly AppDbContext _context;
        private readonly AuditLogService _auditLogService;
        private readonly IFaceRecognitionService _faceService;
        private readonly IConfiguration _config;
        private readonly ILogger<ChamCongService> _logger;
        private readonly ThongBaoService _thongBaoService;

        public ChamCongService(
            AppDbContext context,
            AuditLogService auditLogService,
            IFaceRecognitionService faceService,
            IConfiguration config,
            ILogger<ChamCongService> logger,
            ThongBaoService thongBaoService)
        {
            _context = context;
            _auditLogService = auditLogService;
            _faceService = faceService;
            _config = config;
            _logger = logger;
            _thongBaoService = thongBaoService;
        }

        public async Task<int?> GetNvHoSoIdByTaiKhoanAsync(int taiKhoanId)
        {
            return await _context.TaiKhoans
                .Where(x => x.Id == taiKhoanId)
                .Select(x => x.NvHoSoId)
                .FirstOrDefaultAsync();
        }

        public async Task<List<int>> GetMyYearsAsync(int nvId)
        {
            return await _context.ChamCongs
                .Where(x => x.NvHoSoId == nvId)
                .Select(x => x.Ngay.Year)
                .Distinct()
                .OrderByDescending(x => x)
                .ToListAsync();
        }

        // ============================================================
        // 1. L?Y DANH S�CH Bảng công tháng (Summary)
        // ============================================================
        public async Task<List<BangCongThangSummaryDto>> GetBangCongThangAsync(int nam)
        {
            // Get existing BangCongThang records
            var existingBangCong = await _context.BangCongThangs
                .Where(x => x.Nam == nam)
                .ToListAsync();

            // Find months that have ChamCong data but no BangCongThang record
            var monthsWithData = await _context.ChamCongs
                .Where(x => x.Ngay.Year == nam)
                .Select(x => x.Ngay.Month)
                .Distinct()
                .ToListAsync();

            // Create missing BangCongThang records AND link existing ChamCong records
            foreach (var month in monthsWithData)
            {
                var bangCong = existingBangCong.FirstOrDefault(b => b.Thang == month);

                if (bangCong == null)
                {
                    // Create new BangCongThang
                    bangCong = new BangCongThang
                    {
                        Thang = month,
                        Nam = nam,
                        TrangThaiCong = "CHUA_CHOT"
                    };
                    _context.BangCongThangs.Add(bangCong);
                    await _context.SaveChangesAsync(); // Save to get ID
                    existingBangCong.Add(bangCong);
                }

                // Link orphan ChamCong records (wrap in try-catch to avoid crashing on constraint issues)
                try
                {
                    var orphanRecords = await _context.ChamCongs
                        .Where(x => x.Ngay.Year == nam && x.Ngay.Month == month
                                 && x.BangCongThangId != bangCong.Id)
                        .ToListAsync();

                    foreach (var record in orphanRecords)
                    {
                        record.BangCongThangId = bangCong.Id;
                    }

                    if (orphanRecords.Any())
                    {
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Linked {Count} orphan ChamCong records for {Month}/{Year}",
                            orphanRecords.Count, month, nam);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to link orphan ChamCong records for {Month}/{Year}. This is non-fatal.", month, nam);
                }
            }

            // Return all BangCongThang records
            return existingBangCong
                .OrderByDescending(x => x.Thang)
                .Select(x => new BangCongThangSummaryDto
                {
                    Id = x.Id,
                    Thang = x.Thang,
                    Nam = x.Nam,
                    TrangThaiCong = x.TrangThaiCong,
                    NgayChotCong = x.NgayChotCong
                })
                .ToList();
        }
        // ============================================================
        // 2. LẤY CHI TIẾT BẢNG CÔNG THÁNG
        // ============================================================
        public async Task<BangCongThangDetailDto?> GetBangCongThangDetailAsync(int bangCongId)
        {
            var bang = await _context.BangCongThangs
                .Include(x => x.TaiKhoanChot)
                .FirstOrDefaultAsync(x => x.Id == bangCongId);

            if (bang == null) return null;

            // FIX: Query bằng tháng/năm thay vì BangCongThangId
            // Đảm bảo lấy được TẤT CẢ bản ghi chấm công cho tháng đó
            // (kể cả những bản ghi chưa được link đúng BangCongThangId)
            var listNgayCong = await _context.ChamCongs
                .Where(c => c.Ngay.Year == bang.Nam && c.Ngay.Month == bang.Thang)
                .Include(c => c.NvHoSo)
                .OrderBy(c => c.Ngay)
                .ThenBy(c => c.NvHoSo.HoTen)
                .Select(c => new ChamCongNgayDto
                {
                    Id = c.Id,
                    NvHoSoId = c.NvHoSoId,
                    HoTen = c.NvHoSo.HoTen,
                    Ngay = c.Ngay,
                    GioVao = c.GioVao,
                    GioRa = c.GioRa,
                    SoGioOt = c.SoGioOt,
                    TrangThai = c.TrangThai,
                    SourceModule = c.SourceModule,
                    IsLockedByModule = c.IsLockedByModule,
                    GhiChu = c.GhiChu
                })
                .ToListAsync();

            // Đồng thời link lại các bản ghi chưa đúng BangCongThangId (tự sửa data)
            try
            {
                var orphans = await _context.ChamCongs
                    .Where(c => c.Ngay.Year == bang.Nam && c.Ngay.Month == bang.Thang
                             && c.BangCongThangId != bang.Id)
                    .ToListAsync();

                if (orphans.Any())
                {
                    foreach (var o in orphans) o.BangCongThangId = bang.Id;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Linked {Count} orphan ChamCong records to BangCongThang #{Id} ({Thang}/{Nam})",
                        orphans.Count, bang.Id, bang.Thang, bang.Nam);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to link orphan ChamCong records for BangCongThang #{Id}", bang.Id);
            }

            return new BangCongThangDetailDto
            {
                Id = bang.Id,
                Thang = bang.Thang,
                Nam = bang.Nam,
                TrangThaiCong = bang.TrangThaiCong,
                NgayChotCong = bang.NgayChotCong,
                TaiKhoanChotId = bang.TaiKhoanChotId,
                TenNguoiChot = bang.TaiKhoanChot?.TenDangNhap,
                NgayCongs = listNgayCong
            };
        }

        // ============================================================
        // 3. L?Y CH?M Công 1 NH�N VI�N TRONG NG�Y
        // ============================================================
        public async Task<ChamCongOfEmployeeDto?> GetChamCongCuaNhanVienAsync(int nvId, DateTime ngay)
        {
            var cc = await _context.ChamCongs
                .Include(x => x.NvHoSo)
                .FirstOrDefaultAsync(x => x.NvHoSoId == nvId && x.Ngay.Date == ngay.Date);

            if (cc == null) return null;

            return new ChamCongOfEmployeeDto
            {
                ChamCongId = cc.Id,
                NvHoSoId = cc.NvHoSoId,
                HoTen = cc.NvHoSo.HoTen,
                Ngay = cc.Ngay,
                GioVao = cc.GioVao,
                GioRa = cc.GioRa,
                GioVaoOt = cc.GioVaoOt,
                GioRaOt = cc.GioRaOt,
                SoGioOt = cc.SoGioOt,
                TrangThai = cc.TrangThai,
                GhiChu = cc.GhiChu,
                IsLockedByModule = cc.IsLockedByModule
            };
        }


        // ============================================================
        // 4. CẬP NHẬT CHẤM CÔNG 1 NGÀY (HR nhập liệu)
        // ============================================================
        public async Task UpdateChamCongNgayAsync(int chamCongId, UpdateChamCongRequestDto dto)
        {
            var cc = await _context.ChamCongs.FirstOrDefaultAsync(x => x.Id == chamCongId);
            if (cc == null) throw new Exception("Kh�ng t�m th?y ch?m Công.");

            if (cc.IsLockedByModule)
                throw new Exception("D? li?u d� b? kho�, kh�ng th? s?a.");

            // Parse time strings "HH:mm" to DateTime
            if (!string.IsNullOrEmpty(dto.GioVao))
            {
                if (TimeSpan.TryParse(dto.GioVao, out var timeVao))
                    cc.GioVao = cc.Ngay.Date.Add(timeVao);
            }
            else
            {
                cc.GioVao = null;
            }

            if (!string.IsNullOrEmpty(dto.GioRa))
            {
                if (TimeSpan.TryParse(dto.GioRa, out var timeRa))
                    cc.GioRa = cc.Ngay.Date.Add(timeRa);
            }
            else
            {
                cc.GioRa = null;
            }

            cc.SoGioOt = dto.SoGioOt;
            cc.TrangThai = dto.TrangThai;
            cc.GhiChu = dto.GhiChu;

            await _context.SaveChangesAsync();

            // Broadcast SignalR update for realtime refresh (don't block on error)
            try
            {
                await _thongBaoService.BroadcastEntityUpdateAsync("ChamCong", chamCongId, "updated");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast SignalR update for ChamCong {ChamCongId}", chamCongId);
            }
        }

        // ============================================================
        // 4B. XO� CH?M Công 1 NG�Y (HR xo� b?n ghi)
        // ============================================================
        public async Task DeleteChamCongAsync(int chamCongId)
        {
            var cc = await _context.ChamCongs.FirstOrDefaultAsync(x => x.Id == chamCongId);
            if (cc == null) throw new Exception("Kh�ng t�m th?y ch?m Công.");

            if (cc.IsLockedByModule)
                throw new Exception("D? li?u d� b? kho�, kh�ng th? xo�.");

            _context.ChamCongs.Remove(cc);
            await _context.SaveChangesAsync();

            // Broadcast SignalR update for realtime refresh (don't block on error)
            try
            {
                await _thongBaoService.BroadcastEntityUpdateAsync("ChamCong", chamCongId, "deleted");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast SignalR update for deleted ChamCong {ChamCongId}", chamCongId);
            }
        }



        // ============================================================
        // 5. KHO� / M? KHO� Bảng công tháng
        // ============================================================
        public async Task LockBangCongAsync(LockBangCongRequestDto request, int taiKhoanId)
        {
            var bang = await _context.BangCongThangs.FirstOrDefaultAsync(x => x.Id == request.BangCongThangId);
            if (bang == null) throw new Exception("Kh�ng t�m th?y b?ng Công.");


            if (!request.Lock)
            {
                // check gửi duyệt lương
                var salariesInApproval = await _context.BangLuongThangs
                    .Where(x => x.BangCongThangId == bang.Id)
                    .Where(x => x.TrangThai == "CHO_DUYET_GIAM_DOC"
                             || x.TrangThai == "DA_DUYET"
                             || x.TrangThai == "DA_KHOA")
                    .Select(x => new { x.Id, x.TrangThai })
                    .ToListAsync();

                if (salariesInApproval.Any())
                {
                    var count = salariesInApproval.Count;
                    var states = string.Join(", ", salariesInApproval.Select(s => s.TrangThai).Distinct());

                    await _auditLogService.LogActionAsync(
                        taiKhoanId: taiKhoanId,
                        bang: "Bảng công tháng",
                        doiTuongId: bang.Id,
                        tenDoiTuong: $"Công T{bang.Thang}/{bang.Nam}",
                        hanhDong: "Th? m? ch?t (th?t b?i)",
                        ghiChu: $"Kh�ng th? m? ch?t v� c� {count} b?ng luong ? tr?ng th�i: {states}"
                    );

                    // Message ng?n g?n v� d? hi?u
                    string message = count == 1
                        ? "Không thể mở chốt công! Đang có bảng lương chờ duyệt. Vui lòng thu hồi bảng lương trước!"
                        : $"Không thể mở chốt công! Có {count} bảng lương chờ duyệt. Vui lòng thu hồi bảng lương trước!";

                    throw new InvalidOperationException(message);
                }

                // N?u validation pass, ghi log m? ch?t th�nh Công
                await _auditLogService.LogActionAsync(
                    taiKhoanId: taiKhoanId,
                    bang: "Bảng công tháng",
                    doiTuongId: bang.Id,
                    tenDoiTuong: $"Công T{bang.Thang}/{bang.Nam}",
                    hanhDong: "Mở chốt công",
                    ghiChu: request.GhiChu ?? $"Mở chốt công tháng {bang.Thang}/{bang.Nam}"
                );
            }
            else
            {
                // Ghi log Chốt công
                await _auditLogService.LogActionAsync(
                    taiKhoanId: taiKhoanId,
                    bang: "Bảng công tháng",
                    doiTuongId: bang.Id,
                    tenDoiTuong: $"Công T{bang.Thang}/{bang.Nam}",
                    hanhDong: "Chốt công",
                    ghiChu: request.GhiChu ?? $"Chốt bảng công tháng {bang.Thang}/{bang.Nam}"
                );
            }

            // Th?c hi?n lock/unlock
            if (request.Lock)
            {
                bang.TrangThaiCong = "DA_CHOT_CONG";
                bang.NgayChotCong = DateTime.UtcNow;
                bang.TaiKhoanChotId = taiKhoanId;
            }
            else
            {
                bang.TrangThaiCong = "DANG_NHAP_LIEU";
                bang.NgayChotCong = null;
                bang.TaiKhoanChotId = null;
            }

            await _context.SaveChangesAsync();
        }
        // =====================================================
        // 6. NH�N VI�N � XEM CHI TI?T 1 B?NG LUONG C?A T�I
        // (ch? tr? v? n?u b?ng luong thu?c nvId)
        // =====================================================
        public async Task<BangLuongThangDetailDto?> GetMyDetailAsync(int id, int nvId)
        {
            var bl = await _context.BangLuongThangs
                .Include(x => x.NvHoSo)
                .Include(x => x.TaiKhoanTinh)
                .Include(x => x.TaiKhoanGuiDuyet)
                .Include(x => x.TaiKhoanDuyet)
                .Include(x => x.TaiKhoanKhoa)
                .FirstOrDefaultAsync(x => x.Id == id && x.NvHoSoId == nvId);

            if (bl == null) return null;

            return new BangLuongThangDetailDto
            {
                Id = bl.Id,
                NvHoSoId = bl.NvHoSoId,
                HoTen = bl.NvHoSo.HoTen,
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
                NguoiDuyet = bl.TaiKhoanDuyet?.TenDangNhap,
                NguoiKhoa = bl.TaiKhoanKhoa?.TenDangNhap
            };
        }
        // ============================================================
        // 6. EMPLOYEE � L?Y DANH S�CH TH�NG C� CH?M Công TRONG NAM
        // ============================================================
        public async Task<List<int>> GetMyMonthsAsync(int nvId, int nam)
        {
            return await _context.ChamCongs
                .Where(x => x.NvHoSoId == nvId && x.Ngay.Year == nam)
                .Select(x => x.Ngay.Month)
                .Distinct()
                .OrderByDescending(x => x)
                .ToListAsync();
        }

        // ============================================================
        // 7. EMPLOYEE � L?Y CH?M Công THEO TH�NG C?A T�I
        // ============================================================
        public async Task<List<ChamCongNgayDto>> GetMyTimesheetMonthAsync(int nvId, int thang, int nam)
        {
            return await _context.ChamCongs
                .Where(x => x.NvHoSoId == nvId && x.Ngay.Year == nam && x.Ngay.Month == thang)
                .Include(x => x.NvHoSo)
                .OrderBy(x => x.Ngay)
                .Select(x => new ChamCongNgayDto
                {
                    Id = x.Id,
                    NvHoSoId = x.NvHoSoId,
                    HoTen = x.NvHoSo.HoTen,
                    Ngay = x.Ngay,
                    GioVao = x.GioVao,
                    GioRa = x.GioRa,
                    GioVaoOt = x.GioVaoOt,
                    GioRaOt = x.GioRaOt,
                    SoGioOt = x.SoGioOt,
                    TrangThai = x.TrangThai,
                    SourceModule = x.SourceModule,
                    IsLockedByModule = x.IsLockedByModule,
                    GhiChu = x.GhiChu
                })
                .ToListAsync();
        }

        /// <summary>
        /// [NEW] L?y chi ti?t Bảng công tháng v?i ph�n trang server-side
        /// </summary>
        public async Task<ChamCongPagedResponseDto> GetBangCongPagedAsync(ChamCongPagedRequestDto request)
        {
            // Validate page index & size
            if (request.PageIndex < 1) request.PageIndex = 1;
            if (request.PageSize < 1) request.PageSize = 20;
            if (request.PageSize > 100) request.PageSize = 100; // Max 100 records/page

            // Base query: Lấy BangCongThang để biết tháng/năm, rồi query ChamCongs theo ngày
            var bangCong = await _context.BangCongThangs.FindAsync(request.BangCongThangId);
            if (bangCong == null)
                throw new Exception("Không tìm thấy bảng công tháng.");

            // FIX: Query bằng tháng/năm thay vì BangCongThangId
            var query = _context.ChamCongs
                .Where(cc => cc.Ngay.Year == bangCong.Nam && cc.Ngay.Month == bangCong.Thang)
                .Join(
                    _context.NvHoSos,
                    cc => cc.NvHoSoId,
                    nv => nv.Id,
                    (cc, nv) => new { ChamCong = cc, NhanVien = nv }
                );

            // Filter: Keyword (t�m theo t�n ho?c m� NV)
            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim().ToLower();
                query = query.Where(x =>
                    x.NhanVien.HoTen.ToLower().Contains(keyword) ||
                    x.NhanVien.MaNhanVien.ToLower().Contains(keyword)
                );
            }


            if (!string.IsNullOrWhiteSpace(request.TrangThai))
            {
                query = query.Where(x => x.ChamCong.TrangThai == request.TrangThai);
            }


            var totalRecords = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)request.PageSize);


            var items = await query
                .OrderByDescending(x => x.ChamCong.Ngay)
                .ThenBy(x => x.NhanVien.HoTen)
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new ChamCongChiTietDto
                {
                    Id = x.ChamCong.Id,
                    BangCongThangId = x.ChamCong.BangCongThangId,
                    NvHoSoId = x.ChamCong.NvHoSoId,
                    MaNhanVien = x.NhanVien.MaNhanVien,
                    HoTen = x.NhanVien.HoTen,
                    Avatar = x.NhanVien.AnhCaNhanUrl,
                    Ngay = x.ChamCong.Ngay,
                    GioVao = x.ChamCong.GioVao != null ? x.ChamCong.GioVao.Value.ToString("HH:mm") : null,
                    GioRa = x.ChamCong.GioRa != null ? x.ChamCong.GioRa.Value.ToString("HH:mm") : null,
                    TrangThai = x.ChamCong.TrangThai ?? "Chua r�",
                    GhiChu = x.ChamCong.GhiChu
                })
                .ToListAsync();
            return new ChamCongPagedResponseDto
            {
                Items = items,
                TotalRecords = totalRecords,
                TotalPages = totalPages,
                CurrentPage = request.PageIndex,
                PageSize = request.PageSize
            };
        }

        // ============================================================
        // FACE RECOGNITION ATTENDANCE
        // ============================================================

        public async Task<FaceRecognitionResultDto> CheckInByFaceAsync(
            IFormFile image, string? ipAddress, string? deviceInfo, int? nvHoSoId = null, bool xacNhanOt = false)
        {
            var result = new FaceRecognitionResultDto();
            var threshold = GetConfidenceThreshold();
            _logger.LogInformation($"🔍 [CHECK-IN] Threshold hiện tại: {threshold:P0}");

            var (buffer, error) = await ReadImageBufferAsync(image);
            if (error != null)
            {
                result.Success = false;
                result.Message = error;
                return result;
            }

            // LOGIC MỚI: Nếu có nvHoSoId (đã đăng nhập) → chỉ verify face của người đó
            // Nếu không có nvHoSoId (kiosk) → tìm trong toàn bộ DB
            int? matchedNvId = null;
            double? similarity = null;

            if (nvHoSoId.HasValue)
            {
                // ✅ Đã đăng nhập: CHỈ verify face của người đang login
                var (isMatch, confidence) = await _faceService.VerifyEmployeeFaceAsync(nvHoSoId.Value, new MemoryStream(buffer!));
                similarity = confidence;
                // FIX: Chỉ dùng isMatch (đã check threshold bên trong VerifyEmployeeFaceAsync)
                matchedNvId = isMatch ? nvHoSoId.Value : null;
                _logger.LogInformation($"🔍 [CHECK-IN] NV #{nvHoSoId}: isMatch={isMatch}, confidence={confidence:P0}, threshold={threshold:P0}");
            }
            else
            {
                // ⚠️ Kiosk không đăng nhập: Tìm trong toàn bộ DB
                var (candidateNvId, confidence) = await _faceService.IdentifyEmployeeAsync(new MemoryStream(buffer!));
                similarity = confidence;

                // FIX #4: Xử lý an toàn khi similarity null
                if (!similarity.HasValue || candidateNvId == null)
                {
                    matchedNvId = null;
                    _logger.LogWarning($"⚠️ [CHECK-IN] Kiosk: Không nhận diện được (similarity={similarity}, candidateNvId={candidateNvId})");
                }
                else if (similarity.Value >= threshold)
                {
                    matchedNvId = candidateNvId;
                    _logger.LogInformation($"🔍 [CHECK-IN] Kiosk: Match NV #{candidateNvId}, confidence={confidence:P0}, threshold={threshold:P0}");
                }
                else
                {
                    matchedNvId = null;
                    _logger.LogInformation($"🔍 [CHECK-IN] Kiosk: Không đủ ngưỡng - confidence={confidence:P0} < threshold={threshold:P0}");
                }
            }

            // FIX #5: Chuẩn hoá Timezone (Vietnam UTC+7)
            var now = DateTime.UtcNow.AddHours(7);

            var log = new ChamCongFaceLog
            {
                NvHoSoId = matchedNvId,
                ThoiGian = now,
                Loai = "VAO",
                TrangThai = matchedNvId != null ? "THANH_CONG" : "THAT_BAI",
                ConfidenceScore = similarity.HasValue ? (decimal?)similarity.Value : null,
                IpAddress = ipAddress,
                DeviceInfo = deviceInfo,
                FaceImageUrl = null,
                CreatedAt = now
            };
            _context.ChamCongFaceLogs.Add(log);
            await _context.SaveChangesAsync();

            if (matchedNvId == null)
            {
                result.Success = false;
                result.Message = similarity.HasValue
                    ? $"Không nhận diện được khuôn mặt (độ khớp {similarity:P0}). Vui lòng chụp rõ hơn hoặc đăng ký lại."
                    : "Không nhận diện được khuôn mặt. Vui lòng thử lại hoặc liên hệ HR.";
                result.ConfidenceScore = log.ConfidenceScore;
                return result;
            }

            var nhanVien = await _context.NvHoSos.FirstOrDefaultAsync(x => x.Id == matchedNvId.Value);
            if (nhanVien == null || nhanVien.TrangThaiLamViec == 2)
            {
                result.Success = false;
                result.Message = "Nhân viên không tồn tại hoặc đã nghỉ việc.";
                return result;
            }

            // Sử dụng 'now' đã được định nghĩa ở trên (UTC+7)
            var (expectedIn, expectedOut, lateGrace, earlyGrace) = GetWorkTimeConfig();
            var scheduledIn = now.Date.Add(expectedIn);
            var lateMinutes = (int)Math.Round((now - scheduledIn).TotalMinutes);
            var inNote = BuildInNote(lateMinutes, lateGrace);

            string warningMsg = "";
            var scheduledOutTemp = now.Date.Add(expectedOut);

            // === Tải bản ghi chấm công hôm nay sớm để phát hiện tình huống OT ===
            var bangCong = await _context.BangCongThangs
                .FirstOrDefaultAsync(x => x.Thang == now.Month && x.Nam == now.Year);
            if (bangCong == null)
            {
                bangCong = new BangCongThang { Thang = now.Month, Nam = now.Year, TrangThaiCong = "CHUA_CHOT" };
                _context.BangCongThangs.Add(bangCong);
                await _context.SaveChangesAsync();
            }

            var ngayHomNay = now.Date;
            var chamCongExist = await _context.ChamCongs
                .FirstOrDefaultAsync(x => x.NvHoSoId == matchedNvId.Value && x.Ngay.Date == ngayHomNay);

            // === OT FLOW: Ca làm thường đã hoàn tất (GioVao + GioRa đều có) ===
            if (chamCongExist?.GioVao != null && chamCongExist.GioRa != null)
            {
                if (chamCongExist.GioVaoOt != null)
                {
                    // OT đã bắt đầu (đang chạy hoặc đã kết thúc)
                    result.Success = false;
                    result.Message = chamCongExist.GioRaOt != null
                        ? $"Bạn đã hoàn tất ca tăng ca hôm nay ({chamCongExist.GioVaoOt?.ToString("HH:mm")} - {chamCongExist.GioRaOt?.ToString("HH:mm")})"
                        : $"Ca tăng ca đang diễn ra (từ {chamCongExist.GioVaoOt?.ToString("HH:mm")}). Vui lòng chấm công ra khi kết thúc";
                    result.NvHoSoId = matchedNvId.Value;
                    result.TenNhanVien = nhanVien.HoTen;
                    result.ConfidenceScore = log.ConfidenceScore;
                    return result;
                }

                // Ca thường xong, chưa có OT → yêu cầu xác nhận từ nhân viên
                if (!xacNhanOt)
                {
                    result.Success = false;
                    result.RequireOtConfirmation = true;
                    result.Message = "Bạn đang chuẩn bị vào tăng ca?";
                    result.NvHoSoId = matchedNvId.Value;
                    result.TenNhanVien = nhanVien.HoTen;
                    result.ConfidenceScore = log.ConfidenceScore;
                    return result;
                }

                // Nhân viên đã xác nhận OT → bắt đầu ca tăng ca
                chamCongExist.GioVaoOt = now;
                await _context.SaveChangesAsync();

                try
                {
                    await _thongBaoService.BroadcastEntityUpdateAsync("ChamCong", chamCongExist.Id, "updated");
                    var taiKhoanOt = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.NvHoSoId == matchedNvId.Value);
                    if (taiKhoanOt != null)
                    {
                        await _thongBaoService.CreateAndPushAsync(
                            taiKhoanOt.Id,
                            "Bắt đầu tăng ca",
                            $"Bạn đã bắt đầu tăng ca lúc {now:HH:mm}. Nhớ chấm công ra khi kết thúc!",
                            "OT",
                            "ChamCong",
                            chamCongExist.Id
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to broadcast SignalR update for OT check-in ChamCong {ChamCongId}", chamCongExist.Id);
                }

                result.Success = true;
                result.Message = $"Bắt đầu tăng ca lúc {now:HH:mm}. Nhớ chấm công ra khi kết thúc!";
                result.NvHoSoId = matchedNvId.Value;
                result.TenNhanVien = nhanVien.HoTen;
                result.ConfidenceScore = log.ConfidenceScore;
                result.ThoiGianChamCong = now;
                result.LoaiChamCong = "VAO_OT";
                result.ChamCongId = chamCongExist.Id;
                result.LogId = log.Id;
                return result;
            }

            // === DIRECT OT CHECK-IN: Nhân viên bấm nút "Tăng Ca" trực tiếp ===
            if (xacNhanOt)
            {
                // Nếu trong giờ làm bình thường: kiểm tra OT đêm hôm qua còn mở
                if (now <= scheduledOutTemp)
                {
                    var yesterday2 = now.AddDays(-2).Date;
                    var openOtYesterday = await _context.ChamCongs
                        .FirstOrDefaultAsync(x => x.NvHoSoId == matchedNvId.Value
                            && x.GioVaoOt != null
                            && x.GioRaOt == null
                            && x.Ngay.Date >= yesterday2
                            && x.Ngay.Date < ngayHomNay);
                    if (openOtYesterday != null)
                    {
                        result.Success = false;
                        result.Message = $"Bạn có ca tăng ca chưa kết thúc từ {openOtYesterday.Ngay:dd/MM} (từ {openOtYesterday.GioVaoOt?.ToString("HH:mm")}). Vui lòng bấm 'Ra tăng ca' trước.";
                        result.NvHoSoId = matchedNvId.Value;
                        result.TenNhanVien = nhanVien.HoTen;
                        result.ConfidenceScore = log.ConfidenceScore;
                        return result;
                    }
                    // Không có OT đêm hôm qua → bỏ qua cờ OT, thực hiện check-in thường
                }
            }

            if (xacNhanOt && now > scheduledOutTemp)
            {
                if (chamCongExist?.GioVaoOt != null)
                {
                    result.Success = false;
                    result.Message = chamCongExist.GioRaOt != null
                        ? $"Bạn đã hoàn tất ca tăng ca hôm nay ({chamCongExist.GioVaoOt?.ToString("HH:mm")} - {chamCongExist.GioRaOt?.ToString("HH:mm")})"
                        : $"Ca tăng ca đang diễn ra (từ {chamCongExist.GioVaoOt?.ToString("HH:mm")}). Vui lòng chấm công ra khi kết thúc";
                    result.NvHoSoId = matchedNvId.Value;
                    result.TenNhanVien = nhanVien.HoTen;
                    result.ConfidenceScore = log.ConfidenceScore;
                    return result;
                }

                var taiKhoanDirect = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.NvHoSoId == matchedNvId.Value);
                if (chamCongExist == null)
                {
                    chamCongExist = new ChamCong
                    {
                        NvHoSoId = matchedNvId.Value,
                        BangCongThangId = bangCong.Id,
                        Ngay = now,
                        GioVaoOt = now,
                        PhuongThuc = "FACE_RECOGNITION",
                        CreatedBy = taiKhoanDirect?.Id,
                        FaceLogVaoId = log.Id,
                        TrangThai = "DI_LAM",
                        SoGioOt = 0
                    };
                    _context.ChamCongs.Add(chamCongExist);
                }
                else
                {
                    chamCongExist.GioVaoOt = now;
                }
                await _context.SaveChangesAsync();

                try
                {
                    var eventType = chamCongExist.GioVao == null ? "created" : "updated";
                    await _thongBaoService.BroadcastEntityUpdateAsync("ChamCong", chamCongExist.Id, eventType);
                    if (taiKhoanDirect != null)
                    {
                        await _thongBaoService.CreateAndPushAsync(
                            taiKhoanDirect.Id,
                            "Bắt đầu tăng ca",
                            $"Bạn đã bắt đầu tăng ca lúc {now:HH:mm}. Nhớ chấm công ra khi kết thúc!",
                            "OT",
                            "ChamCong",
                            chamCongExist.Id
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to broadcast SignalR update for direct OT check-in ChamCong {ChamCongId}", chamCongExist.Id);
                }

                result.Success = true;
                result.Message = $"Bắt đầu tăng ca lúc {now:HH:mm}. Nhớ chấm công ra khi kết thúc!";
                result.NvHoSoId = matchedNvId.Value;
                result.TenNhanVien = nhanVien.HoTen;
                result.ConfidenceScore = log.ConfidenceScore;
                result.ThoiGianChamCong = now;
                result.LoaiChamCong = "VAO_OT";
                result.ChamCongId = chamCongExist.Id;
                result.LogId = log.Id;
                return result;
            }

            // === REGULAR CHECK-IN FLOW ===
            // Chặn check-in thường sau khi giờ làm kết thúc
            if (now > scheduledOutTemp)
            {
                result.Success = false;
                result.Message = "Ngày làm việc đã kết thúc, không thể chấm công Check-in lúc này.";
                result.NvHoSoId = matchedNvId.Value;
                result.TenNhanVien = nhanVien.HoTen;
                result.ConfidenceScore = log.ConfidenceScore;
                return result;
            }

            if ((scheduledIn - now).TotalMinutes > 120) // Sớm hơn 2 tiếng
            {
                warningMsg = " (Lưu ý: Bạn đang check-in quá sớm so với giờ quy định)";
            }

            if (chamCongExist != null && chamCongExist.GioVao != null)
            {
                result.Success = false;
                result.Message = $"Bạn đã chấm công vào lúc {chamCongExist.GioVao?.ToString("HH:mm")}";
                result.NvHoSoId = matchedNvId.Value;
                result.TenNhanVien = nhanVien.HoTen;
                result.ConfidenceScore = log.ConfidenceScore;
                return result;
            }

            var taiKhoan = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.NvHoSoId == matchedNvId.Value);

            if (chamCongExist == null)
            {
                chamCongExist = new ChamCong
                {
                    NvHoSoId = matchedNvId.Value,
                    BangCongThangId = bangCong.Id,
                    Ngay = now,
                    GioVao = now,
                    PhuongThuc = "FACE_RECOGNITION",
                    CreatedBy = taiKhoan?.Id,
                    FaceLogVaoId = log.Id,
                    TrangThai = "DI_LAM",
                    SoGioOt = 0
                };
                if (!string.IsNullOrEmpty(inNote))
                {
                    chamCongExist.GhiChu = AppendNote(chamCongExist.GhiChu, inNote);
                    if (lateMinutes > lateGrace)
                    {
                        chamCongExist.TrangThai = "TRE";
                    }
                }
                _context.ChamCongs.Add(chamCongExist);
            }
            else
            {
                // Update existing record and ensure BangCongThangId is correct
                chamCongExist.BangCongThangId = bangCong.Id;
                chamCongExist.GioVao = now;
                chamCongExist.PhuongThuc = "FACE_RECOGNITION";
                chamCongExist.FaceLogVaoId = log.Id;
                chamCongExist.TrangThai = "DI_LAM";
                if (!string.IsNullOrEmpty(inNote))
                {
                    chamCongExist.GhiChu = AppendNote(chamCongExist.GhiChu, inNote);
                    if (lateMinutes > lateGrace)
                    {
                        chamCongExist.TrangThai = "TRE";
                    }
                }
            }
            await _context.SaveChangesAsync();

            // Broadcast SignalR update for realtime refresh (check-in)
            try
            {
                await _thongBaoService.BroadcastEntityUpdateAsync("ChamCong", chamCongExist.Id, "created");

                // Gửi thông báo cá nhân nếu có cảnh báo giờ giấc
                if (!string.IsNullOrEmpty(warningMsg) && taiKhoan != null)
                {
                    await _thongBaoService.CreateAndPushAsync(
                        taiKhoan.Id,
                        "Cảnh báo chấm công",
                        $"Bạn vừa chấm công VÀO lúc {now:HH:mm}. {warningMsg.Replace("(", "").Replace(")", "")}",
                        "WARNING",
                        "ChamCong",
                        chamCongExist.Id
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast SignalR update for check-in ChamCong {ChamCongId}", chamCongExist.Id);
            }

            result.Success = true;
            result.Message = $"Chấm công VÀO thành công lúc {now:HH:mm}" + (string.IsNullOrEmpty(inNote) ? string.Empty : $" ({inNote})") + warningMsg;
            result.NvHoSoId = matchedNvId.Value;
            result.TenNhanVien = nhanVien.HoTen;
            result.ConfidenceScore = log.ConfidenceScore;
            result.ThoiGianChamCong = now;
            result.LoaiChamCong = "VAO";
            result.ChamCongId = chamCongExist.Id;
            result.LogId = log.Id;
            return result;
        }

        public async Task<FaceRecognitionResultDto> CheckOutByFaceAsync(
            IFormFile image, string? ipAddress, string? deviceInfo, int? nvHoSoId = null, bool xacNhanOtOut = false)
        {
            var result = new FaceRecognitionResultDto();
            var threshold = GetConfidenceThreshold();
            _logger.LogInformation($"🔍 [CHECK-OUT] Threshold hiện tại: {threshold:P0}");

            var (buffer, error) = await ReadImageBufferAsync(image);
            if (error != null)
            {
                result.Success = false;
                result.Message = error;
                return result;
            }

            // LOGIC MỚI: Nếu có nvHoSoId (đã đăng nhập) → chỉ verify face của người đó
            // Nếu không có nvHoSoId (kiosk) → tìm trong toàn bộ DB
            int? matchedNvId = null;
            double? similarity = null;

            if (nvHoSoId.HasValue)
            {
                // ✅ Đã đăng nhập: CHỈ verify face của người đang login
                var (isMatch, confidence) = await _faceService.VerifyEmployeeFaceAsync(nvHoSoId.Value, new MemoryStream(buffer!));
                similarity = confidence;
                // FIX: Chỉ dùng isMatch (đã check threshold bên trong VerifyEmployeeFaceAsync)
                matchedNvId = isMatch ? nvHoSoId.Value : null;
                _logger.LogInformation($"🔍 [CHECK-OUT] NV #{nvHoSoId}: isMatch={isMatch}, confidence={confidence:P0}, threshold={threshold:P0}");
            }
            else
            {
                // ⚠️ Kiosk không đăng nhập: Tìm trong toàn bộ DB
                var (candidateNvId, confidence) = await _faceService.IdentifyEmployeeAsync(new MemoryStream(buffer!));
                similarity = confidence;

                // FIX #4: Xử lý an toàn khi similarity null
                if (!similarity.HasValue || candidateNvId == null)
                {
                    matchedNvId = null;
                    _logger.LogWarning($"⚠️ [CHECK-OUT] Kiosk: Không nhận diện được (similarity={similarity}, candidateNvId={candidateNvId})");
                }
                else if (similarity.Value >= threshold)
                {
                    matchedNvId = candidateNvId;
                    _logger.LogInformation($"🔍 [CHECK-OUT] Kiosk: Match NV #{candidateNvId}, confidence={confidence:P0}, threshold={threshold:P0}");
                }
                else
                {
                    matchedNvId = null;
                    _logger.LogInformation($"🔍 [CHECK-OUT] Kiosk: Không đủ ngưỡng - confidence={confidence:P0} < threshold={threshold:P0}");
                }
            }

            // FIX #5: Chuẩn hoá Timezone (Vietnam UTC+7)
            var now = DateTime.UtcNow.AddHours(7);

            var log = new ChamCongFaceLog
            {
                NvHoSoId = matchedNvId,
                ThoiGian = now,
                Loai = "RA",
                TrangThai = matchedNvId != null ? "THANH_CONG" : "THAT_BAI",
                ConfidenceScore = similarity.HasValue ? (decimal?)similarity.Value : null,
                IpAddress = ipAddress,
                DeviceInfo = deviceInfo,
                FaceImageUrl = null,
                CreatedAt = now
            };
            _context.ChamCongFaceLogs.Add(log);
            await _context.SaveChangesAsync();

            if (matchedNvId == null)
            {
                result.Success = false;
                result.Message = similarity.HasValue
                    ? $"Không nhận diện được khuôn mặt (độ khớp {similarity:P0}). Vui lòng chụp rõ hơn hoặc đăng ký lại."
                    : "Không nhận diện được khuôn mặt. Vui lòng thử lại hoặc liên hệ HR.";
                result.ConfidenceScore = log.ConfidenceScore;
                return result;
            }

            var nhanVien = await _context.NvHoSos.FirstOrDefaultAsync(x => x.Id == matchedNvId.Value);
            if (nhanVien == null)
            {
                result.Success = false;
                result.Message = "Nhân viên không tồn tại.";
                return result;
            }

            // Sử dụng 'now' đã được định nghĩa ở trên (UTC+7)
            var ngayHomNay = now.Date;
            var chamCong = await _context.ChamCongs
                .FirstOrDefaultAsync(x => x.NvHoSoId == matchedNvId.Value && x.Ngay.Date == ngayHomNay);

            // === OT-OUT PRIORITY: Khi nhân viên bấm nút "Ra tăng ca" ===
            // Ưu tiên tìm ca OT đang chạy (hôm nay hoặc hôm qua) trước khi xét check-out thường
            if (xacNhanOtOut)
            {
                // Kiểm tra ca OT hôm nay trước
                if (chamCong?.GioVaoOt != null && chamCong.GioRaOt == null)
                {
                    chamCong.GioRaOt = now;
                    chamCong.SoGioOt = (decimal)Math.Round((chamCong.GioRaOt.Value - chamCong.GioVaoOt.Value).TotalHours, 2);
                    await _context.SaveChangesAsync();
                    try { await _thongBaoService.BroadcastEntityUpdateAsync("ChamCong", chamCong.Id, "updated"); } catch { }
                    result.Success = true;
                    result.Message = $"Kết thúc tăng ca lúc {now:HH:mm}. Tổng OT: {chamCong.SoGioOt:F1}h";
                    result.NvHoSoId = matchedNvId.Value;
                    result.TenNhanVien = nhanVien.HoTen;
                    result.ConfidenceScore = log.ConfidenceScore;
                    result.ThoiGianChamCong = now;
                    result.LoaiChamCong = "RA_OT";
                    result.ChamCongId = chamCong.Id;
                    result.LogId = log.Id;
                    return result;
                }

                // Kiểm tra ca OT chưa đóng từ những ngày trước (OT qua đêm)
                var yesterday = now.AddDays(-2).Date;
                var openOtPrev = await _context.ChamCongs
                    .OrderByDescending(x => x.Ngay)
                    .FirstOrDefaultAsync(x => x.NvHoSoId == matchedNvId.Value
                        && x.GioVaoOt != null
                        && x.GioRaOt == null
                        && x.Ngay.Date >= yesterday
                        && x.Ngay.Date < ngayHomNay);

                if (openOtPrev != null)
                {
                    openOtPrev.GioRaOt = now;
                    openOtPrev.SoGioOt = (decimal)Math.Round((openOtPrev.GioRaOt.Value - openOtPrev.GioVaoOt!.Value).TotalHours, 2);
                    await _context.SaveChangesAsync();
                    try
                    {
                        await _thongBaoService.BroadcastEntityUpdateAsync("ChamCong", openOtPrev.Id, "updated");
                        var tkOt2 = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.NvHoSoId == matchedNvId.Value);
                        if (tkOt2 != null)
                            await _thongBaoService.CreateAndPushAsync(tkOt2.Id, "Kết thúc tăng ca",
                                $"Tổng OT: {openOtPrev.SoGioOt:F1}h", "OT", "ChamCong", openOtPrev.Id);
                    }
                    catch (Exception ex) { _logger.LogWarning(ex, "Broadcast OT-out failed"); }

                    result.Success = true;
                    result.Message = $"Kết thúc tăng ca lúc {now:HH:mm}. Tổng OT: {openOtPrev.SoGioOt:F1}h";
                    result.NvHoSoId = matchedNvId.Value;
                    result.TenNhanVien = nhanVien.HoTen;
                    result.ConfidenceScore = log.ConfidenceScore;
                    result.ThoiGianChamCong = now;
                    result.LoaiChamCong = "RA_OT";
                    result.ChamCongId = openOtPrev.Id;
                    result.LogId = log.Id;
                    return result;
                }

                // Không tìm thấy OT nào đang mở
                result.Success = false;
                result.Message = "Không tìm thấy ca tăng ca nào đang diễn ra để kết thúc.";
                result.NvHoSoId = matchedNvId.Value;
                result.TenNhanVien = nhanVien.HoTen;
                result.ConfidenceScore = log.ConfidenceScore;
                return result;
            }

            // Fallback: tìm ca OT đang chạy từ ngày hôm qua (OT đêm, qua đêm)
            if (chamCong == null || chamCong.GioVao == null)
            {
                var yesterday = now.AddDays(-2).Date;
                var ongoingOt = await _context.ChamCongs
                    .FirstOrDefaultAsync(x => x.NvHoSoId == matchedNvId.Value
                        && x.GioVaoOt != null
                        && x.GioRaOt == null
                        && x.Ngay.Date >= yesterday
                        && x.Ngay.Date < ngayHomNay);

                if (ongoingOt != null)
                {
                    ongoingOt.GioRaOt = now;
                    ongoingOt.SoGioOt = (decimal)Math.Round((ongoingOt.GioRaOt.Value - ongoingOt.GioVaoOt!.Value).TotalHours, 2);
                    await _context.SaveChangesAsync();

                    try
                    {
                        await _thongBaoService.BroadcastEntityUpdateAsync("ChamCong", ongoingOt.Id, "updated");
                        var tkOt = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.NvHoSoId == matchedNvId.Value);
                        if (tkOt != null)
                        {
                            await _thongBaoService.CreateAndPushAsync(
                                tkOt.Id,
                                "Kết thúc tăng ca",
                                $"Bạn đã kết thúc tăng ca lúc {now:HH:mm}. Tổng OT: {ongoingOt.SoGioOt:F1}h",
                                "OT", "ChamCong", ongoingOt.Id
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to broadcast SignalR update for overnight OT check-out ChamCong {ChamCongId}", ongoingOt.Id);
                    }

                    result.Success = true;
                    result.Message = $"Kết thúc tăng ca lúc {now:HH:mm}. Tổng OT: {ongoingOt.SoGioOt:F1}h";
                    result.NvHoSoId = matchedNvId.Value;
                    result.TenNhanVien = nhanVien.HoTen;
                    result.ConfidenceScore = log.ConfidenceScore;
                    result.ThoiGianChamCong = now;
                    result.LoaiChamCong = "RA_OT";
                    result.ChamCongId = ongoingOt.Id;
                    result.LogId = log.Id;
                    return result;
                }
            }

            if (chamCong == null || chamCong.GioVao == null)
            {
                result.Success = false;
                result.Message = "Bạn chưa chấm công VÀO hôm nay!";
                result.NvHoSoId = matchedNvId.Value;
                result.TenNhanVien = nhanVien.HoTen;
                result.ConfidenceScore = log.ConfidenceScore;
                return result;
            }

            // Ensure chamCong has correct BangCongThangId
            var bangCongRa = await _context.BangCongThangs
                .FirstOrDefaultAsync(x => x.Thang == now.Month && x.Nam == now.Year);
            if (bangCongRa == null)
            {
                bangCongRa = new BangCongThang { Thang = now.Month, Nam = now.Year, TrangThaiCong = "CHUA_CHOT" };
                _context.BangCongThangs.Add(bangCongRa);
                await _context.SaveChangesAsync();
            }
            chamCong.BangCongThangId = bangCongRa.Id;

            if (chamCong.GioRa != null)
            {
                // Ca làm thường đã kết thúc → kiểm tra xem có ca OT không
                if (chamCong.GioVaoOt != null && chamCong.GioRaOt == null)
                {
                    // OT check-out: kết thúc ca tăng ca
                    chamCong.GioRaOt = now;
                    chamCong.SoGioOt = (decimal)Math.Round((chamCong.GioRaOt.Value - chamCong.GioVaoOt.Value).TotalHours, 2);
                    await _context.SaveChangesAsync();

                    try
                    {
                        await _thongBaoService.BroadcastEntityUpdateAsync("ChamCong", chamCong.Id, "updated");
                        var taiKhoanOtOut = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.NvHoSoId == matchedNvId.Value);
                        if (taiKhoanOtOut != null)
                        {
                            await _thongBaoService.CreateAndPushAsync(
                                taiKhoanOtOut.Id,
                                "Kết thúc tăng ca",
                                $"Bạn đã kết thúc tăng ca lúc {now:HH:mm}. Tổng OT: {chamCong.SoGioOt:F1}h",
                                "OT",
                                "ChamCong",
                                chamCong.Id
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to broadcast SignalR update for OT check-out ChamCong {ChamCongId}", chamCong.Id);
                    }

                    result.Success = true;
                    result.Message = $"Kết thúc tăng ca lúc {now:HH:mm}. Tổng OT: {chamCong.SoGioOt:F1}h";
                    result.NvHoSoId = matchedNvId.Value;
                    result.TenNhanVien = nhanVien.HoTen;
                    result.ConfidenceScore = log.ConfidenceScore;
                    result.ThoiGianChamCong = now;
                    result.LoaiChamCong = "RA_OT";
                    result.ChamCongId = chamCong.Id;
                    result.LogId = log.Id;
                    return result;
                }

                result.Success = false;
                result.Message = chamCong.GioVaoOt != null && chamCong.GioRaOt != null
                    ? $"Bạn đã hoàn tất ca làm và tăng ca hôm nay"
                    : $"Bạn đã chấm công ra lúc {chamCong.GioRa?.ToString("HH:mm")}";
                result.NvHoSoId = matchedNvId.Value;
                result.TenNhanVien = nhanVien.HoTen;
                result.ConfidenceScore = log.ConfidenceScore;
                return result;
            }

            chamCong.GioRa = now;
            chamCong.FaceLogRaId = log.Id;

            var (expectedIn, expectedOut, lateGrace, earlyGrace) = GetWorkTimeConfig();
            var scheduledOut = now.Date.Add(expectedOut);
            var deltaMinutes = (int)Math.Round((scheduledOut - now).TotalMinutes);
            var outNote = BuildOutNote(deltaMinutes, earlyGrace);

            // Cảnh báo nếu check-out bất thường
            string warningMsg = "";
            var scheduledInTemp = now.Date.Add(expectedIn);
            if (now < scheduledInTemp) // Check-out trước giờ vào làm
            {
                warningMsg = " (Cảnh báo: Bạn đang check-out trước giờ làm việc)";
            }

            if (!string.IsNullOrEmpty(outNote))
            {
                chamCong.GhiChu = AppendNote(chamCong.GhiChu, outNote);
            }

            var soGioLam = (chamCong.GioRa.Value - chamCong.GioVao.Value).TotalHours;
            var soCong = Math.Round(soGioLam / 8.0, 2);

            await _context.SaveChangesAsync();

            // Broadcast SignalR update for realtime refresh (check-out)
            try
            {
                await _thongBaoService.BroadcastEntityUpdateAsync("ChamCong", chamCong.Id, "updated");

                // Gửi thông báo cá nhân nếu có cảnh báo giờ giấc
                var taiKhoanOut = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.NvHoSoId == matchedNvId.Value);
                if (!string.IsNullOrEmpty(warningMsg) && taiKhoanOut != null)
                {
                    await _thongBaoService.CreateAndPushAsync(
                       taiKhoanOut.Id,
                       "Cảnh báo chấm công",
                       $"Bạn vừa chấm công RA lúc {now:HH:mm}. {warningMsg.Replace("(", "").Replace(")", "")}",
                       "WARNING",
                       "ChamCong",
                       chamCong.Id
                   );
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast SignalR update for check-out ChamCong {ChamCongId}", chamCong.Id);
            }

            result.Success = true;
            var outSuffix = string.IsNullOrEmpty(outNote) ? string.Empty : $" ({outNote})";
            result.Message = $"Chấm công RA thành công lúc {now:HH:mm}. Tổng {soGioLam:F1}h ({soCong} công){outSuffix}{warningMsg}";
            result.NvHoSoId = matchedNvId.Value;
            result.TenNhanVien = nhanVien.HoTen;
            result.ConfidenceScore = log.ConfidenceScore;
            result.ThoiGianChamCong = now;
            result.LoaiChamCong = "RA";
            result.ChamCongId = chamCong.Id;
            result.LogId = log.Id;
            return result;
        }

        public async Task<PageResultDto<ChamCongFaceLogDto>> GetFaceLogsAsync(FaceLogFilterDto filter)
        {
            var query = _context.ChamCongFaceLogs.Include(x => x.NhanVien).AsQueryable();

            if (filter.NvId.HasValue)
                query = query.Where(x => x.NvHoSoId == filter.NvId.Value);
            if (filter.TuNgay.HasValue)
                query = query.Where(x => x.ThoiGian >= filter.TuNgay.Value);
            if (filter.DenNgay.HasValue)
                query = query.Where(x => x.ThoiGian <= filter.DenNgay.Value.AddDays(1));
            if (!string.IsNullOrEmpty(filter.Loai))
                query = query.Where(x => x.Loai == filter.Loai);
            if (!string.IsNullOrEmpty(filter.TrangThai))
                query = query.Where(x => x.TrangThai == filter.TrangThai);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.ThoiGian)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(x => new ChamCongFaceLogDto
                {
                    Id = x.Id,
                    NvHoSoId = x.NvHoSoId,
                    HoTen = x.NhanVien != null ? x.NhanVien.HoTen : "Unknown",
                    ThoiGian = x.ThoiGian,
                    Loai = x.Loai,
                    TrangThai = x.TrangThai,
                    ConfidenceScore = x.ConfidenceScore.HasValue ? (double)x.ConfidenceScore.Value : null,
                    IpAddress = x.IpAddress,
                    DeviceInfo = x.DeviceInfo
                })
                .ToListAsync();

            return new PageResultDto<ChamCongFaceLogDto>
            {
                Items = items,
                TotalRecords = total,
                TotalPages = (int)Math.Ceiling(total / (double)filter.PageSize),
                CurrentPage = filter.Page,
                PageSize = filter.PageSize
            };
        }

        private double GetConfidenceThreshold()
        {
            if (double.TryParse(_config["FaceRecognition:ConfidenceThreshold"], out var configured))
            {
                // FIX: Cap threshold to 0.75 to match Python Service fix
                if (configured > 0.75) return 0.75;

                _logger.LogDebug($"📊 [THRESHOLD] Loaded from config: {configured:P0}");
                return configured; // Sử dụng giá trị từ config
            }

            _logger.LogWarning($"⚠️ [THRESHOLD] Config not found, using default: 60%");
            return 0.6; // Mặc định 60% để giảm tỷ lệ từ chối
        }

        private async Task<(byte[]? buffer, string? error)> ReadImageBufferAsync(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                return (null, "Vui lòng chọn ảnh");
            }

            await using var stream = image.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return (ms.ToArray(), null);
        }

        private async Task<(bool ok, string? error, double? quality)> ValidateFaceImageAsync(byte[] buffer)
        {
            var hasFace = await _faceService.HasFaceAsync(new MemoryStream(buffer));
            if (!hasFace)
            {
                return (false, "Không thấy khuôn mặt trong khung. Vui lòng đưa khuôn mặt vào khung và chụp lại.", null);
            }

            var quality = await _faceService.EvaluateImageQualityAsync(new MemoryStream(buffer));
            var minQuality = GetMinQualityThreshold();
            if (quality < minQuality)
            {
                return (false, $"Ảnh quá mờ/thiếu sáng (điểm chất lượng {quality:P0}). Vui lòng chụp rõ hơn.", quality);
            }

            return (true, null, quality);
        }

        private double GetMinQualityThreshold()
        {
            if (double.TryParse(_config["FaceRecognition:MinQualityScore"], out var configured))
            {
                return configured;
            }

            return 0.5;
        }

        // ============================================================
        // 8. CẤU HÌNH GIỜ LÀM VIỆC (Dùng ThamSoHeThong)
        // ============================================================
        public async Task<ChamCongConfigDto> GetWorkHoursConfigAsync()
        {
            var config = await _context.ThamSoHeThongs
                .Where(x => x.MaThamSo.StartsWith("CHAM_CONG_"))
                .ToListAsync();

            var dto = new ChamCongConfigDto();

            var start = config.FirstOrDefault(x => x.MaThamSo == "CHAM_CONG_GIO_VAO")?.GiaTri;
            if (!string.IsNullOrEmpty(start)) dto.GioVao = start;

            var end = config.FirstOrDefault(x => x.MaThamSo == "CHAM_CONG_GIO_RA")?.GiaTri;
            if (!string.IsNullOrEmpty(end)) dto.GioRa = end;

            var late = config.FirstOrDefault(x => x.MaThamSo == "CHAM_CONG_TRE_PHUT")?.GiaTri;
            if (int.TryParse(late, out int l)) dto.LateGraceMinutes = l;

            var early = config.FirstOrDefault(x => x.MaThamSo == "CHAM_CONG_SOM_PHUT")?.GiaTri;
            if (int.TryParse(early, out int e)) dto.EarlyLeaveGraceMinutes = e;

            return dto;
        }

        public async Task UpdateWorkHoursConfigAsync(ChamCongConfigDto dto, int taiKhoanId)
        {
            await SaveThamSoAsync("CHAM_CONG_GIO_VAO", dto.GioVao, "Giờ vào làm tiêu chuẩn");
            await SaveThamSoAsync("CHAM_CONG_GIO_RA", dto.GioRa, "Giờ tan làm tiêu chuẩn");
            await SaveThamSoAsync("CHAM_CONG_TRE_PHUT", dto.LateGraceMinutes.ToString(), "Số phút cho phép đi muộn");
            await SaveThamSoAsync("CHAM_CONG_SOM_PHUT", dto.EarlyLeaveGraceMinutes.ToString(), "Số phút cho phép về sớm");

            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync(taiKhoanId, "Cấu hình hệ thống", 0, "Cấu hình chấm công", "Cập nhật",
                $"Giờ vào: {dto.GioVao}, Giờ ra: {dto.GioRa}, Cho phép trễ: {dto.LateGraceMinutes}p, Về sớm: {dto.EarlyLeaveGraceMinutes}p");
        }

        private async Task SaveThamSoAsync(string ma, string giatri, string mota)
        {
            var ts = await _context.ThamSoHeThongs.FirstOrDefaultAsync(x => x.MaThamSo == ma);
            if (ts == null)
            {
                ts = new ThamSoHeThong
                {
                    MaThamSo = ma,
                    GiaTri = giatri,
                    MoTa = mota,
                    NgayBatDauHieuLuc = DateTime.UtcNow
                };
                _context.ThamSoHeThongs.Add(ts);
            }
            else
            {
                ts.GiaTri = giatri;
                ts.MoTa = mota;
            }
        }

        // ============================================================
        // 9. THÔNG BÁO NHẮC NHỞ CHECK IN/OUT (Background Job Endpoint)
        // ============================================================
        public async Task NotifyUpcomingCheckInOutAsync()
        {
            var (start, end, _, _) = GetWorkTimeConfig();
            var now = DateTime.UtcNow.AddHours(7); // VN Time
            var nowTime = now.TimeOfDay;

            // Kiểm tra sắp đến giờ vào (trước 5 phút)
            if (IsAboutTime(nowTime, start, 5))
            {
                await _thongBaoService.
                    BroadcastToAllAsync("Sắp đến giờ vào làm!",
                    $"Còn 5 phút nữa là đến giờ vào làm ({start:hh\\:mm}). Chấm công ngay nhé!", "info");
            }

            // Kiểm tra sắp đến giờ về (trước 5 phút)
            if (IsAboutTime(nowTime, end, 5))
            {
                await _thongBaoService.BroadcastToAllAsync("Sắp đến giờ tan làm!",
                    $"Còn 5 phút nữa là đến giờ về ({end:hh\\:mm}). Đừng quên checkout!", "info");
            }
        }

        private bool IsAboutTime(TimeSpan current, TimeSpan target, int minutesBefore)
        {
            var diff = target - current;
            return diff.TotalMinutes > 0 && diff.TotalMinutes <= minutesBefore && diff.TotalMinutes > minutesBefore - 1;
            // Trigger 1 lần trong khoảng checking (ví dụ cron chạy mỗi phút)
        }

        private (TimeSpan expectedIn, TimeSpan expectedOut, int lateGraceMinutes, int earlyLeaveGraceMinutes) GetWorkTimeConfig()
        {
            // Ưu tiên đọc từ DB
            try
            {
                var workTimes = _context.ThamSoHeThongs
                    .Where(x => x.MaThamSo.StartsWith("CHAM_CONG_"))
                    .ToDictionary(x => x.MaThamSo, x => x.GiaTri);

                TimeSpan start = new TimeSpan(8, 0, 0);
                if (workTimes.ContainsKey("CHAM_CONG_GIO_VAO") && TimeSpan.TryParse(workTimes["CHAM_CONG_GIO_VAO"], out var s))
                    start = s;
                else if (TimeSpan.TryParse(_config["WorkTime:Start"], out var sc))
                    start = sc;

                TimeSpan end = new TimeSpan(17, 0, 0);
                if (workTimes.ContainsKey("CHAM_CONG_GIO_RA") && TimeSpan.TryParse(workTimes["CHAM_CONG_GIO_RA"], out var e))
                    end = e;
                else if (TimeSpan.TryParse(_config["WorkTime:End"], out var ec))
                    end = ec;

                int late = 15;
                if (workTimes.ContainsKey("CHAM_CONG_TRE_PHUT") && int.TryParse(workTimes["CHAM_CONG_TRE_PHUT"], out var l))
                    late = l;
                else if (int.TryParse(_config["WorkTime:LateGraceMinutes"], out var lc))
                    late = lc;

                int early = 15;
                if (workTimes.ContainsKey("CHAM_CONG_SOM_PHUT") && int.TryParse(workTimes["CHAM_CONG_SOM_PHUT"], out var ea))
                    early = ea;
                else if (int.TryParse(_config["WorkTime:EarlyLeaveGraceMinutes"], out var eac))
                    early = eac;

                return (start, end, late, early);
            }
            catch
            {
                // Fallback config file
                var start = TimeSpan.TryParse(_config["WorkTime:Start"], out var s) ? s : new TimeSpan(8, 0, 0);
                var end = TimeSpan.TryParse(_config["WorkTime:End"], out var e) ? e : new TimeSpan(17, 0, 0);
                var lateGrace = int.TryParse(_config["WorkTime:LateGraceMinutes"], out var lg) ? lg : 15;
                var earlyGrace = int.TryParse(_config["WorkTime:EarlyLeaveGraceMinutes"], out var eg) ? eg : 15;
                return (start, end, lateGrace, earlyGrace);
            }
        }

        private string? BuildInNote(int lateMinutes, int graceMinutes)
        {
            if (lateMinutes > graceMinutes)
            {
                return $"Vào muộn {lateMinutes} phút";
            }

            if (lateMinutes < -graceMinutes)
            {
                return $"Vào sớm {Math.Abs(lateMinutes)} phút";
            }

            return null;
        }

        private string? BuildOutNote(int deltaMinutes, int graceMinutes)
        {
            // deltaMinutes > 0: ra trước giờ chuẩn; deltaMinutes < 0: ra muộn / OT
            if (deltaMinutes > graceMinutes)
            {
                return $"Ra sớm {deltaMinutes} phút";
            }

            if (deltaMinutes < -graceMinutes)
            {
                return $"Ra muộn {Math.Abs(deltaMinutes)} phút";
            }

            return null;
        }

        private string AppendNote(string? current, string note)
        {
            if (string.IsNullOrWhiteSpace(current))
            {
                return note;
            }

            if (current.Contains(note, StringComparison.OrdinalIgnoreCase))
            {
                return current;
            }

            return $"{current}; {note}";
        }
    }
}
