//using Microsoft.EntityFrameworkCore;
//using QLNS_BE.Models.Entities;

//namespace QLNS.ERP.Data
//{
//    public class AppDbContext : DbContext
//    {
//        public AppDbContext(DbContextOptions<AppDbContext> options)
//            : base(options)
//        {
//        }

//        // DbSet cho từng entity
//        public DbSet<NvHoSo> NvHoSos { get; set; }
//        public DbSet<PhongBan> PhongBans { get; set; }
//        public DbSet<ChucVu> ChucVus { get; set; }
//        public DbSet<NvCongViec> NvCongViecs { get; set; }

//        public DbSet<VaiTro> VaiTros { get; set; }
//        public DbSet<ChucNang> ChucNangs { get; set; }
//        public DbSet<VaiTroChucNang> VaiTroChucNangs { get; set; }
//        public DbSet<TaiKhoan> TaiKhoans { get; set; }

//        public DbSet<BangCongThang> BangCongThangs { get; set; }
//        public DbSet<ChamCong> ChamCongs { get; set; }

//        public DbSet<LoaiPhep> LoaiPheps { get; set; }
//        public DbSet<DonXinPhep> DonXinPheps { get; set; }

//        public DbSet<NvLuongHienTai> NvLuongHienTais { get; set; }
//        public DbSet<NvLuongDeXuat> NvLuongDeXuats { get; set; }
//        public DbSet<BangLuongThang> BangLuongThangs { get; set; }

//        public DbSet<ThamSoHeThong> ThamSoHeThongs { get; set; }
//        public DbSet<AuditLog> AuditLogs { get; set; }
//        public DbSet<DuAn> DuAns { get; set; }
//        public DbSet<DuAnThanhVien> DuAnThanhViens { get; set; }
//        public DbSet<DuAnNhatKyTrangThai> DuAnNhatKyTrangThais { get; set; }

//        // ==========================================
//        // [OPTIONAL] Alias DbSets (cho tiện query)
//        // ==========================================
//        public DbSet<TaiKhoan> TAI_KHOAN { get; set; }
//        public DbSet<VaiTro> VAI_TRO { get; set; }
//        public DbSet<BangCongThang> BANG_CONG_THANG { get; set; }
//        public DbSet<NvHoSo> NV_HO_SO { get; set; }
//        public DbSet<ChamCongChiTiet> ChamCongChiTiets { get; set; }

//        protected override void OnModelCreating(ModelBuilder modelBuilder)
//        {
//            base.OnModelCreating(modelBuilder);

//            // ============= TÊN BẢNG =============
//            modelBuilder.Entity<NvHoSo>().ToTable("NV_HO_SO");
//            modelBuilder.Entity<PhongBan>().ToTable("PHONG_BAN");
//            modelBuilder.Entity<ChucVu>().ToTable("CHUC_VU");
//            modelBuilder.Entity<NvCongViec>().ToTable("NV_CONG_VIEC");

//            modelBuilder.Entity<VaiTro>().ToTable("VAI_TRO");
//            modelBuilder.Entity<ChucNang>().ToTable("CHUC_NANG");
//            modelBuilder.Entity<VaiTroChucNang>().ToTable("VAI_TRO_CHUC_NANG");
//            modelBuilder.Entity<TaiKhoan>().ToTable("TAI_KHOAN");

//            modelBuilder.Entity<BangCongThang>().ToTable("BANG_CONG_THANG");
//            modelBuilder.Entity<ChamCong>().ToTable("CHAM_CONG");
//            modelBuilder.Entity<ChamCongChiTiet>().ToTable("CHAM_CONG_CHI_TIET");

//            modelBuilder.Entity<LoaiPhep>().ToTable("LOAI_PHEP");
//            modelBuilder.Entity<DonXinPhep>().ToTable("DON_XIN_PHEP");

//            modelBuilder.Entity<NvLuongHienTai>().ToTable("NV_LUONG_HIEN_TAI");
//            modelBuilder.Entity<NvLuongDeXuat>().ToTable("NV_LUONG_DE_XUAT");
//            modelBuilder.Entity<BangLuongThang>().ToTable("BANG_LUONG_THANG");

//            modelBuilder.Entity<ThamSoHeThong>().ToTable("THAM_SO_HE_THONG");
//            modelBuilder.Entity<AuditLog>().ToTable("AUDIT_LOG");

//            modelBuilder.Entity<DuAn>().ToTable("DU_AN");
//            modelBuilder.Entity<DuAnThanhVien>().ToTable("DU_AN_THANH_VIEN");
//            modelBuilder.Entity<DuAnNhatKyTrangThai>().ToTable("DU_AN_NHAT_KY_TRANG_THAI");

//            // ============= KHÓA ĐỘC NHẤT =============
//            modelBuilder.Entity<NvHoSo>()
//                .HasIndex(x => x.MaNhanVien)
//                .IsUnique();

//            modelBuilder.Entity<PhongBan>()
//                .HasIndex(x => x.MaPhongBan)
//                .IsUnique();

//            modelBuilder.Entity<ChucVu>()
//                .HasIndex(x => x.MaChucVu)
//                .IsUnique();

//            modelBuilder.Entity<VaiTro>()
//                .HasIndex(x => x.MaVaiTro)
//                .IsUnique();

//            modelBuilder.Entity<ChucNang>()
//                .HasIndex(x => x.MaChucNang)
//                .IsUnique();

//            modelBuilder.Entity<LoaiPhep>()
//                .HasIndex(x => x.MaLoaiPhep)
//                .IsUnique();

//            modelBuilder.Entity<TaiKhoan>()
//                .HasIndex(x => x.TenDangNhap)
//                .IsUnique();

//            modelBuilder.Entity<DuAn>()
//                .HasIndex(x => x.MaDuAn)
//                .IsUnique();

//            // ============= QUAN HỆ PHÒNG BAN TỰ THAM CHIẾU =============
//            modelBuilder.Entity<PhongBan>()
//                .HasOne(x => x.PhongBanCha)
//                .WithMany(x => x.PhongBanCon)
//                .HasForeignKey(x => x.PhongBanChaId)
//                .OnDelete(DeleteBehavior.SetNull);

//            // ============= NV_CONG_VIEC =============
//            modelBuilder.Entity<NvCongViec>()
//                .HasOne(x => x.NvHoSo)
//                .WithMany(x => x.CongViecs)
//                .HasForeignKey(x => x.NvHoSoId)
//                .OnDelete(DeleteBehavior.Restrict);

//            modelBuilder.Entity<NvCongViec>()
//                .HasOne(x => x.PhongBan)
//                .WithMany(x => x.NhanVienCongViecs)
//                .HasForeignKey(x => x.PhongBanId)
//                .OnDelete(DeleteBehavior.Restrict);

//            modelBuilder.Entity<NvCongViec>()
//                .HasOne(x => x.ChucVu)
//                .WithMany(x => x.NhanVienCongViecs)
//                .HasForeignKey(x => x.ChucVuId)
//                .OnDelete(DeleteBehavior.Restrict);

//            // ============= TÀI KHOẢN =============
//            modelBuilder.Entity<TaiKhoan>()
//                .HasOne(x => x.NvHoSo)
//                .WithMany(x => x.TaiKhoans)
//                .HasForeignKey(x => x.NvHoSoId)
//                .OnDelete(DeleteBehavior.SetNull);

//            modelBuilder.Entity<TaiKhoan>()
//                .HasOne(x => x.VaiTro)
//                .WithMany(x => x.TaiKhoans)
//                .HasForeignKey(x => x.VaiTroId)
//                .OnDelete(DeleteBehavior.Restrict);

//            // ============= VAI_TRO_CHUC_NANG =============
//            modelBuilder.Entity<VaiTroChucNang>()
//                .HasOne(x => x.VaiTro)
//                .WithMany(x => x.VaiTroChucNangs)
//                .HasForeignKey(x => x.VaiTroId)
//                .OnDelete(DeleteBehavior.Cascade);

//            modelBuilder.Entity<VaiTroChucNang>()
//                .HasOne(x => x.ChucNang)
//                .WithMany(x => x.VaiTroChucNangs)
//                .HasForeignKey(x => x.ChucNangId)
//                .OnDelete(DeleteBehavior.Cascade);

//            // ============= BẢNG CÔNG THÁNG =============
//            modelBuilder.Entity<BangCongThang>()
//                .HasOne(x => x.TaiKhoanChot)
//                .WithMany(x => x.BangCongDaChot)
//                .HasForeignKey(x => x.TaiKhoanChotId)
//                .OnDelete(DeleteBehavior.SetNull);

//            // ==========================================
//            // [NEW] CHẤM CÔNG - Cấu hình chi tiết
//            // ==========================================
//            modelBuilder.Entity<ChamCong>()
//                .HasOne(x => x.NvHoSo)
//                .WithMany(x => x.ChamCongs)
//                .HasForeignKey(x => x.NvHoSoId)
//                .OnDelete(DeleteBehavior.Restrict);

//            modelBuilder.Entity<ChamCong>()
//                .HasOne(x => x.BangCongThang)
//                .WithMany(x => x.ChamCongs)
//                .HasForeignKey(x => x.BangCongThangId)
//                .OnDelete(DeleteBehavior.Restrict);

//            // Index để tăng performance cho query phân trang
//            modelBuilder.Entity<ChamCong>()
//                .HasIndex(x => x.BangCongThangId)
//                .HasDatabaseName("IDX_CHAM_CONG_BANG_CONG_THANG");

//            modelBuilder.Entity<ChamCong>()
//                .HasIndex(x => x.NvHoSoId)
//                .HasDatabaseName("IDX_CHAM_CONG_NV_HO_SO");

//            modelBuilder.Entity<ChamCong>()
//                .HasIndex(x => x.Ngay)
//                .HasDatabaseName("IDX_CHAM_CONG_NGAY");

//            // Index phức hợp: Đảm bảo không có duplicate (1 nhân viên chỉ có 1 record/ngày/tháng)
//            modelBuilder.Entity<ChamCong>()
//                .HasIndex(x => new { x.BangCongThangId, x.NvHoSoId, x.Ngay })
//                .IsUnique()
//                .HasDatabaseName("UQ_CHAM_CONG_THANG_NV_NGAY");

//            // ============= LOẠI PHÉP & ĐƠN PHÉP =============
//            modelBuilder.Entity<DonXinPhep>()
//                .HasOne(x => x.NvHoSo)
//                .WithMany(x => x.DonXinPheps)
//                .HasForeignKey(x => x.NvHoSoId)
//                .OnDelete(DeleteBehavior.Restrict);

//            modelBuilder.Entity<DonXinPhep>()
//                .HasOne(x => x.LoaiPhep)
//                .WithMany(x => x.DonXinPheps)
//                .HasForeignKey(x => x.LoaiPhepId)
//                .OnDelete(DeleteBehavior.Restrict);

//            modelBuilder.Entity<DonXinPhep>()
//                .HasOne(x => x.NguoiDuyet)
//                .WithMany()
//                .HasForeignKey(x => x.NguoiDuyetId)
//                .OnDelete(DeleteBehavior.SetNull);

//            // ============= LƯƠNG HIỆN TẠI =============
//            modelBuilder.Entity<NvLuongHienTai>()
//                .HasOne(x => x.NvHoSo)
//                .WithMany(x => x.LuongHienTais)
//                .HasForeignKey(x => x.NvHoSoId)
//                .OnDelete(DeleteBehavior.Restrict);

//            // ============= ĐỀ XUẤT LƯƠNG / TÀI KHOẢN =============
//            modelBuilder.Entity<NvLuongDeXuat>()
//                .HasOne(x => x.NvHoSo)
//                .WithMany(x => x.LuongDeXuats)
//                .HasForeignKey(x => x.NvHoSoId)
//                .OnDelete(DeleteBehavior.Restrict);

//            modelBuilder.Entity<NvLuongDeXuat>()
//                .HasOne(x => x.TaiKhoanTao)
//                .WithMany(x => x.LuongDeXuatsTao)
//                .HasForeignKey(x => x.TaiKhoanTaoId)
//                .OnDelete(DeleteBehavior.Restrict);

//            modelBuilder.Entity<NvLuongDeXuat>()
//                .HasOne(x => x.TaiKhoanDuyet)
//                .WithMany(x => x.LuongDeXuatsDuyet)
//                .HasForeignKey(x => x.TaiKhoanDuyetId)
//                .OnDelete(DeleteBehavior.SetNull);

//            // ============= BẢNG LƯƠNG THÁNG =============
//            modelBuilder.Entity<BangLuongThang>()
//                .HasOne(x => x.NvHoSo)
//                .WithMany(x => x.BangLuongThangs)
//                .HasForeignKey(x => x.NvHoSoId)
//                .OnDelete(DeleteBehavior.Restrict);

//            modelBuilder.Entity<BangLuongThang>()
//                .HasOne(x => x.BangCongThang)
//                .WithMany(x => x.BangLuongThangs)
//                .HasForeignKey(x => x.BangCongThangId)
//                .OnDelete(DeleteBehavior.Restrict);

//            modelBuilder.Entity<BangLuongThang>()
//                .HasOne(x => x.TaiKhoanTinh)
//                .WithMany(x => x.BangLuongDaTinh)
//                .HasForeignKey(x => x.TaiKhoanTinhId)
//                .OnDelete(DeleteBehavior.SetNull);

//            modelBuilder.Entity<BangLuongThang>()
//                .HasOne(x => x.TaiKhoanGuiDuyet)
//                .WithMany(x => x.BangLuongGuiDuyet)
//                .HasForeignKey(x => x.TaiKhoanGuiDuyetId)
//                .OnDelete(DeleteBehavior.SetNull);

//            modelBuilder.Entity<BangLuongThang>()
//                .HasOne(x => x.TaiKhoanDuyet)
//                .WithMany(x => x.BangLuongDaDuyet)
//                .HasForeignKey(x => x.TaiKhoanDuyetId)
//                .OnDelete(DeleteBehavior.SetNull);

//            modelBuilder.Entity<BangLuongThang>()
//                .HasOne(x => x.TaiKhoanKhoa)
//                .WithMany(x => x.BangLuongDaKhoa)
//                .HasForeignKey(x => x.TaiKhoanKhoaId)
//                .OnDelete(DeleteBehavior.SetNull);

//            // ============= AUDIT LOG =============
//            modelBuilder.Entity<AuditLog>()
//                .HasOne(x => x.TaiKhoan)
//                .WithMany(x => x.AuditLogs)
//                .HasForeignKey(x => x.TaiKhoanId)
//                .OnDelete(DeleteBehavior.Restrict);

//            // ============= DỰ ÁN =============
//            modelBuilder.Entity<DuAn>()
//                .HasOne(d => d.NvPhuTrach)
//                .WithMany(nv => nv.DuAnPhuTrachs)
//                .HasForeignKey(d => d.NvPhuTrachId)
//                .OnDelete(DeleteBehavior.SetNull);

//            modelBuilder.Entity<DuAn>()
//                .HasOne(d => d.TaiKhoanTao)
//                .WithMany(tk => tk.DuAnTao)
//                .HasForeignKey(d => d.TaiKhoanTaoId)
//                .OnDelete(DeleteBehavior.Restrict);

//            modelBuilder.Entity<DuAn>()
//                .HasOne(d => d.TaiKhoanDuyet)
//                .WithMany(tk => tk.DuAnDuyet)
//                .HasForeignKey(d => d.TaiKhoanDuyetId)
//                .OnDelete(DeleteBehavior.SetNull);

//            modelBuilder.Entity<DuAnThanhVien>()
//                .HasOne(tv => tv.DuAn)
//                .WithMany(d => d.ThanhViens)
//                .HasForeignKey(tv => tv.DuAnId)
//                .OnDelete(DeleteBehavior.Cascade);

//            modelBuilder.Entity<DuAnThanhVien>()
//                .HasOne(tv => tv.NvHoSo)
//                .WithMany(nv => nv.DuAnThanhViens)
//                .HasForeignKey(tv => tv.NvHoSoId)
//                .OnDelete(DeleteBehavior.Restrict);

//            modelBuilder.Entity<DuAnNhatKyTrangThai>()
//                .HasOne(nk => nk.DuAn)
//                .WithMany(d => d.NhatKyTrangThais)
//                .HasForeignKey(nk => nk.DuAnId)
//                .OnDelete(DeleteBehavior.Cascade);

//            modelBuilder.Entity<DuAnNhatKyTrangThai>()
//                .HasOne(nk => nk.TaiKhoanThucHien)
//                .WithMany(tk => tk.DuAnNhatKyTrangThais)
//                .HasForeignKey(nk => nk.TaiKhoanThucHienId)
//                .OnDelete(DeleteBehavior.Restrict);
//        }
//    }
//}
using Microsoft.EntityFrameworkCore;
using QLNS_BE.Models.Entities;

namespace QLNS.ERP.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // ==========================================
        // 1. DbSets (Danh sách các bảng)
        // ==========================================
        public DbSet<NvHoSo> NvHoSos { get; set; }
        public DbSet<PhongBan> PhongBans { get; set; }
        public DbSet<ChucVu> ChucVus { get; set; }
        public DbSet<NvCongViec> NvCongViecs { get; set; }

        public DbSet<VaiTro> VaiTros { get; set; }
        public DbSet<ChucNang> ChucNangs { get; set; }
        public DbSet<VaiTroChucNang> VaiTroChucNangs { get; set; }
        public DbSet<TaiKhoan> TaiKhoans { get; set; }

        public DbSet<BangCongThang> BangCongThangs { get; set; }
        public DbSet<ChamCong> ChamCongs { get; set; }
        public DbSet<ChamCongChiTiet> ChamCongChiTiets { get; set; } // Đưa lên nhóm chính cho dễ quản lý

        public DbSet<LoaiPhep> LoaiPheps { get; set; }
        public DbSet<DonXinPhep> DonXinPheps { get; set; }

        public DbSet<NvLuongHienTai> NvLuongHienTais { get; set; }
        public DbSet<NvLuongDeXuat> NvLuongDeXuats { get; set; }
        public DbSet<BangLuongThang> BangLuongThangs { get; set; }
        public DbSet<PhuCapLoai> PhuCapLoais { get; set; }
        public DbSet<NvPhuCap> NvPhuCaps { get; set; }
        public DbSet<BangLuongItem> BangLuongItems { get; set; }

        public DbSet<ThamSoHeThong> ThamSoHeThongs { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<DuAn> DuAns { get; set; }
        public DbSet<DuAnThanhVien> DuAnThanhViens { get; set; }
        public DbSet<DuAnNhatKyTrangThai> DuAnNhatKyTrangThais { get; set; }
        public DbSet<DuAnTask> DuAnTasks { get; set; }
        public DbSet<DuAnFile> DuAnFiles { get; set; } = null!;
        public DbSet<YeuCauDieuChuyen> YeuCauDieuChuyens { get; set; }
        public DbSet<ThongBao> ThongBaos { get; set; }

        // ==========================================
        // NƠI LÀM VIỆC (Phiếu đề xuất, Tạm ứng, Đơn đi muộn)
        // ==========================================
        public DbSet<PhieuDeXuatDungCu> PhieuDeXuatDungCus { get; set; }
        public DbSet<PhieuTamUng> PhieuTamUngs { get; set; }
        public DbSet<DonDiMuon> DonDiMuons { get; set; }

        // ==========================================
        // ĐỀ XUẤT GIÁM ĐỐC
        // ==========================================
        public DbSet<DeXuatGiamDoc> DeXuatGiamDocs { get; set; }

        // ==========================================
        // FACE RECOGNITION (Nhận diện khuôn mặt)
        // ==========================================
        public DbSet<NvFaceData> NvFaceDatas { get; set; }
        public DbSet<ChamCongFaceLog> ChamCongFaceLogs { get; set; }
        public DbSet<FaceRecognitionConfig> FaceRecognitionConfigs { get; set; }

        // ==========================================
        // [OPTIONAL] Alias DbSets (Giữ nguyên theo code cũ)
        // ==========================================
        public DbSet<TaiKhoan> TAI_KHOAN { get; set; }
        public DbSet<VaiTro> VAI_TRO { get; set; }
        public DbSet<BangCongThang> BANG_CONG_THANG { get; set; }
        public DbSet<NvHoSo> NV_HO_SO { get; set; }
        // ChamCongChiTiets đã được khai báo ở trên

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==========================================
            // A. CẤU HÌNH TÊN BẢNG (Table Names)
            // ==========================================
            modelBuilder.Entity<NvHoSo>().ToTable("NV_HO_SO");
            modelBuilder.Entity<PhongBan>().ToTable("PHONG_BAN");
            modelBuilder.Entity<ChucVu>().ToTable("CHUC_VU");
            modelBuilder.Entity<NvCongViec>().ToTable("NV_CONG_VIEC");

            modelBuilder.Entity<VaiTro>().ToTable("VAI_TRO");
            modelBuilder.Entity<ChucNang>().ToTable("CHUC_NANG");
            modelBuilder.Entity<VaiTroChucNang>().ToTable("VAI_TRO_CHUC_NANG");
            modelBuilder.Entity<TaiKhoan>().ToTable("TAI_KHOAN");

            modelBuilder.Entity<BangCongThang>().ToTable("BANG_CONG_THANG");
            modelBuilder.Entity<ChamCong>().ToTable("CHAM_CONG");
            modelBuilder.Entity<ChamCongChiTiet>().ToTable("CHAM_CONG_CHI_TIET"); // [NEW]

            modelBuilder.Entity<LoaiPhep>().ToTable("loai_phep");

            // Column mappings cho LoaiPhep (snake_case)
            modelBuilder.Entity<LoaiPhep>()
                .Property(x => x.Id).HasColumnName("id");
            modelBuilder.Entity<LoaiPhep>()
                .Property(x => x.MaLoaiPhep).HasColumnName("ma_loai_phep");
            modelBuilder.Entity<LoaiPhep>()
                .Property(x => x.TenLoaiPhep).HasColumnName("ten_loai_phep");
            modelBuilder.Entity<LoaiPhep>()
                .Property(x => x.SoNgayMacDinh).HasColumnName("so_ngay_mac_dinh");
            modelBuilder.Entity<LoaiPhep>()
                .Property(x => x.TinhLuong).HasColumnName("tinh_luong");
            modelBuilder.Entity<LoaiPhep>()
                .Property(x => x.TrangThai).HasColumnName("trang_thai");

            modelBuilder.Entity<DonXinPhep>().ToTable("don_xin_phep");

            modelBuilder.Entity<NvLuongHienTai>().ToTable("NV_LUONG_HIEN_TAI");
            modelBuilder.Entity<NvLuongDeXuat>().ToTable("NV_LUONG_DE_XUAT");
            modelBuilder.Entity<BangLuongThang>().ToTable("BANG_LUONG_THANG");
            modelBuilder.Entity<PhuCapLoai>().ToTable("PHU_CAP_LOAI");
            modelBuilder.Entity<NvPhuCap>().ToTable("NV_PHU_CAP");
            modelBuilder.Entity<BangLuongItem>().ToTable("BANG_LUONG_ITEM");

            modelBuilder.Entity<ThamSoHeThong>().ToTable("THAM_SO_HE_THONG");
            modelBuilder.Entity<AuditLog>().ToTable("AUDIT_LOG");

            modelBuilder.Entity<DuAn>().ToTable("DU_AN");
            modelBuilder.Entity<DuAnThanhVien>().ToTable("DU_AN_THANH_VIEN");
            modelBuilder.Entity<DuAnNhatKyTrangThai>().ToTable("DU_AN_NHAT_KY_TRANG_THAI");
            modelBuilder.Entity<DuAnTask>().ToTable("du_an_task");
            modelBuilder.Entity<ThongBao>().ToTable("THONG_BAO");

            // ─── Đề xuất giám đốc ──────────────────────────────────
            modelBuilder.Entity<DeXuatGiamDoc>().ToTable("DE_XUAT_GIAM_DOC");

            // ─── Nơi làm việc ──────────────────────────────────────
            modelBuilder.Entity<PhieuDeXuatDungCu>().ToTable("PHIEU_DE_XUAT_DUNG_CU");
            modelBuilder.Entity<PhieuTamUng>().ToTable("PHIEU_TAM_UNG");
            modelBuilder.Entity<DonDiMuon>().ToTable("DON_DI_MUON");


            // ==========================================
            // B. CẤU HÌNH KHÓA & INDEX (Keys & Indexes)
            // ==========================================

            // Unique Constraints
            modelBuilder.Entity<NvHoSo>().HasIndex(x => x.MaNhanVien).IsUnique();
            modelBuilder.Entity<PhongBan>().HasIndex(x => x.MaPhongBan).IsUnique();
            modelBuilder.Entity<ChucVu>().HasIndex(x => x.MaChucVu).IsUnique();
            modelBuilder.Entity<VaiTro>().HasIndex(x => x.MaVaiTro).IsUnique();
            modelBuilder.Entity<ChucNang>().HasIndex(x => x.MaChucNang).IsUnique();
            modelBuilder.Entity<LoaiPhep>().HasIndex(x => x.MaLoaiPhep).IsUnique();
            modelBuilder.Entity<TaiKhoan>().HasIndex(x => x.TenDangNhap).IsUnique();
            modelBuilder.Entity<DuAn>().HasIndex(x => x.MaDuAn).IsUnique();

            // Index Performance & Unique Logic cho Chấm Công
            modelBuilder.Entity<ChamCong>()
                .HasIndex(x => x.BangCongThangId)
                .HasDatabaseName("IDX_CHAM_CONG_BANG_CONG_THANG");
            modelBuilder.Entity<ChamCong>()
                .HasIndex(x => x.NvHoSoId)
                .HasDatabaseName("IDX_CHAM_CONG_NV_HO_SO");
            modelBuilder.Entity<ChamCong>()
                .HasIndex(x => x.Ngay)
                .HasDatabaseName("IDX_CHAM_CONG_NGAY");
            modelBuilder.Entity<ChamCong>()
                .HasIndex(x => new { x.BangCongThangId, x.NvHoSoId, x.Ngay })
                .IsUnique()
                .HasDatabaseName("UQ_CHAM_CONG_THANG_NV_NGAY");

            // [NEW] Index cho ChamCongChiTiet
            modelBuilder.Entity<ChamCongChiTiet>()
                .HasIndex(x => x.BangCongThangId)
                .HasDatabaseName("IDX_CC_CHI_TIET_BANG_CONG");
            modelBuilder.Entity<ChamCongChiTiet>()
                .HasIndex(x => x.NvHoSoId)
                .HasDatabaseName("IDX_CC_CHI_TIET_NV_HO_SO");
            // Đảm bảo 1 nhân viên chỉ có 1 dòng chi tiết trong 1 ngày
            modelBuilder.Entity<ChamCongChiTiet>()
                .HasIndex(x => new { x.BangCongThangId, x.NvHoSoId, x.Ngay })
                .IsUnique()
                .HasDatabaseName("UQ_CC_CHI_TIET_THANG_NV_NGAY");

            // ==========================================
            // C. CẤU HÌNH QUAN HỆ (Relationships)
            // ==========================================

            // 1. PhongBan
            modelBuilder.Entity<PhongBan>()
                .HasOne(x => x.PhongBanCha)
                .WithMany(x => x.PhongBanCon)
                .HasForeignKey(x => x.PhongBanChaId)
                .OnDelete(DeleteBehavior.SetNull);

            // 2. NvCongViec
            modelBuilder.Entity<NvCongViec>()
                .HasOne(x => x.NvHoSo)
                .WithMany(x => x.CongViecs)
                .HasForeignKey(x => x.NvHoSoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<NvCongViec>()
                .HasOne(x => x.PhongBan)
                .WithMany(x => x.NhanVienCongViecs)
                .HasForeignKey(x => x.PhongBanId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<NvCongViec>()
                .HasOne(x => x.ChucVu)
                .WithMany(x => x.NhanVienCongViecs)
                .HasForeignKey(x => x.ChucVuId)
                .OnDelete(DeleteBehavior.Restrict);

            // 3. TaiKhoan & VaiTro
            modelBuilder.Entity<TaiKhoan>()
                .HasOne(x => x.NvHoSo)
                .WithMany(x => x.TaiKhoans)
                .HasForeignKey(x => x.NvHoSoId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TaiKhoan>()
                .HasOne(x => x.VaiTro)
                .WithMany(x => x.TaiKhoans)
                .HasForeignKey(x => x.VaiTroId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VaiTroChucNang>()
                .HasOne(x => x.VaiTro)
                .WithMany(x => x.VaiTroChucNangs)
                .HasForeignKey(x => x.VaiTroId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VaiTroChucNang>()
                .HasOne(x => x.ChucNang)
                .WithMany(x => x.VaiTroChucNangs)
                .HasForeignKey(x => x.ChucNangId)
                .OnDelete(DeleteBehavior.Cascade);

            // 4. BangCongThang & ChamCong
            modelBuilder.Entity<BangCongThang>()
                .HasOne(x => x.TaiKhoanChot)
                .WithMany(x => x.BangCongDaChot)
                .HasForeignKey(x => x.TaiKhoanChotId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ChamCong>()
                .HasOne(x => x.NvHoSo)
                .WithMany(x => x.ChamCongs)
                .HasForeignKey(x => x.NvHoSoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChamCong>()
                .HasOne(x => x.BangCongThang)
                .WithMany(x => x.ChamCongs)
                .HasForeignKey(x => x.BangCongThangId)
                .OnDelete(DeleteBehavior.Restrict);

            // [NEW] Cấu hình quan hệ cho ChamCongChiTiet
            modelBuilder.Entity<ChamCongChiTiet>()
                .HasOne(x => x.BangCongThang)
                .WithMany(x => x.ChiTietChamCong) // Map với collection trong BangCongThang
                .HasForeignKey(x => x.BangCongThangId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChamCongChiTiet>()
                .HasOne(x => x.NhanVien)          // Map với property NhanVien trong ChamCongChiTiet.cs
                .WithMany(x => x.ChamCongChiTiets) // Map với collection trong NvHoSo
                .HasForeignKey(x => x.NvHoSoId)
                .OnDelete(DeleteBehavior.Restrict);

            // 5. LoaiPhep & DonXinPhep
            modelBuilder.Entity<DonXinPhep>()
                .HasOne(x => x.NvHoSo)
                .WithMany(x => x.DonXinPheps)
                .HasForeignKey(x => x.NvHoSoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DonXinPhep>()
                .HasOne(x => x.LoaiPhep)
                .WithMany(x => x.DonXinPheps)
                .HasForeignKey(x => x.LoaiPhepId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DonXinPhep>()
                .HasOne(x => x.NguoiDuyet)
                .WithMany()
                .HasForeignKey(x => x.NguoiDuyetId)
                .OnDelete(DeleteBehavior.SetNull);

            // Column mappings cho DonXinPhep (snake_case)
            modelBuilder.Entity<DonXinPhep>()
                .Property(x => x.Id).HasColumnName("id");
            modelBuilder.Entity<DonXinPhep>()
                .Property(x => x.NvHoSoId).HasColumnName("nv_ho_so_id");
            modelBuilder.Entity<DonXinPhep>()
                .Property(x => x.LoaiPhepId).HasColumnName("loai_phep_id");
            modelBuilder.Entity<DonXinPhep>()
                .Property(x => x.TuNgay).HasColumnName("tu_ngay");
            modelBuilder.Entity<DonXinPhep>()
                .Property(x => x.DenNgay).HasColumnName("den_ngay");
            modelBuilder.Entity<DonXinPhep>()
                .Property(x => x.SoNgay).HasColumnName("so_ngay");
            modelBuilder.Entity<DonXinPhep>()
                .Property(x => x.LyDo).HasColumnName("ly_do");
            modelBuilder.Entity<DonXinPhep>()
                .Property(x => x.TrangThai).HasColumnName("trang_thai");
            modelBuilder.Entity<DonXinPhep>()
                .Property(x => x.NguoiDuyetId).HasColumnName("nguoi_duyet_id");
            modelBuilder.Entity<DonXinPhep>()
                .Property(x => x.NgayDuyet).HasColumnName("ngay_duyet");
            modelBuilder.Entity<DonXinPhep>()
                .Property(x => x.LyDoTuChoi).HasColumnName("ly_do_tu_choi");
            modelBuilder.Entity<DonXinPhep>()
                .Property(x => x.CreatedAt).HasColumnName("created_at");
            modelBuilder.Entity<DonXinPhep>()
                .Property(x => x.UpdatedAt).HasColumnName("updated_at");

            // 6. Luong & DeXuat
            modelBuilder.Entity<NvLuongHienTai>()
                .HasOne(x => x.NvHoSo)
                .WithMany(x => x.LuongHienTais)
                .HasForeignKey(x => x.NvHoSoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<NvLuongDeXuat>()
                .HasOne(x => x.NvHoSo)
                .WithMany(x => x.LuongDeXuats)
                .HasForeignKey(x => x.NvHoSoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<NvLuongDeXuat>()
                .HasOne(x => x.TaiKhoanTao)
                .WithMany(x => x.LuongDeXuatsTao)
                .HasForeignKey(x => x.TaiKhoanTaoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<NvLuongDeXuat>()
                .HasOne(x => x.TaiKhoanDuyet)
                .WithMany(x => x.LuongDeXuatsDuyet)
                .HasForeignKey(x => x.TaiKhoanDuyetId)
                .OnDelete(DeleteBehavior.SetNull);

            // 7. BangLuongThang
            modelBuilder.Entity<BangLuongThang>()
                .HasOne(x => x.NvHoSo)
                .WithMany(x => x.BangLuongThangs)
                .HasForeignKey(x => x.NvHoSoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BangLuongThang>()
                .HasOne(x => x.BangCongThang)
                .WithMany(x => x.BangLuongThangs)
                .HasForeignKey(x => x.BangCongThangId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BangLuongThang>()
                .HasOne(x => x.TaiKhoanTinh)
                .WithMany(x => x.BangLuongDaTinh)
                .HasForeignKey(x => x.TaiKhoanTinhId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<BangLuongThang>()
                .HasOne(x => x.TaiKhoanGuiDuyet)
                .WithMany(x => x.BangLuongGuiDuyet)
                .HasForeignKey(x => x.TaiKhoanGuiDuyetId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<BangLuongThang>()
                .HasOne(x => x.TaiKhoanDuyet)
                .WithMany(x => x.BangLuongDaDuyet)
                .HasForeignKey(x => x.TaiKhoanDuyetId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<BangLuongThang>()
                .HasOne(x => x.TaiKhoanKhoa)
                .WithMany(x => x.BangLuongDaKhoa)
                .HasForeignKey(x => x.TaiKhoanKhoaId)
                .OnDelete(DeleteBehavior.SetNull);

            // 8. AuditLog & DuAn
            modelBuilder.Entity<AuditLog>()
                .HasOne(x => x.TaiKhoan)
                .WithMany(x => x.AuditLogs)
                .HasForeignKey(x => x.TaiKhoanId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình tên cột cho AuditLog (snake_case)
            modelBuilder.Entity<AuditLog>()
                .Property(x => x.Id)
                .HasColumnName("id");

            modelBuilder.Entity<AuditLog>()
                .Property(x => x.TaiKhoanId)
                .HasColumnName("tai_khoan_id");

            modelBuilder.Entity<AuditLog>()
                .Property(x => x.ThoiGian)
                .HasColumnName("thoi_gian");

            modelBuilder.Entity<AuditLog>()
                .Property(x => x.Bang)
                .HasColumnName("bang");

            modelBuilder.Entity<AuditLog>()
                .Property(x => x.DoiTuongId)
                .HasColumnName("doi_tuong_id");

            modelBuilder.Entity<AuditLog>()
                .Property(x => x.TenDoiTuong)
                .HasColumnName("TenDoiTuong");

            modelBuilder.Entity<AuditLog>()
                .Property(x => x.Truong)
                .HasColumnName("truong");

            modelBuilder.Entity<AuditLog>()
                .Property(x => x.GiaTriCu)
                .HasColumnName("gia_tri_cu");

            modelBuilder.Entity<AuditLog>()
                .Property(x => x.GiaTriMoi)
                .HasColumnName("gia_tri_moi");

            modelBuilder.Entity<AuditLog>()
                .Property(x => x.HanhDong)
                .HasColumnName("hanh_dong");

            modelBuilder.Entity<AuditLog>()
                .Property(x => x.GhiChu)
                .HasColumnName("ghi_chu");


            modelBuilder.Entity<DuAn>()
                .HasOne(d => d.NvPhuTrach)
                .WithMany(nv => nv.DuAnPhuTrachs)
                .HasForeignKey(d => d.NvPhuTrachId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DuAn>()
                .HasOne(d => d.TaiKhoanTao)
                .WithMany(tk => tk.DuAnTao)
                .HasForeignKey(d => d.TaiKhoanTaoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DuAn>()
                .HasOne(d => d.TaiKhoanDuyet)
                .WithMany(tk => tk.DuAnDuyet)
                .HasForeignKey(d => d.TaiKhoanDuyetId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DuAnThanhVien>()
                .HasOne(tv => tv.DuAn)
                .WithMany(d => d.ThanhViens)
                .HasForeignKey(tv => tv.DuAnId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DuAnThanhVien>()
                .HasOne(tv => tv.NvHoSo)
                .WithMany(nv => nv.DuAnThanhViens)
                .HasForeignKey(tv => tv.NvHoSoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DuAnNhatKyTrangThai>()
                .HasOne(nk => nk.DuAn)
                .WithMany(d => d.NhatKyTrangThais)
                .HasForeignKey(nk => nk.DuAnId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DuAnNhatKyTrangThai>()
                .HasOne(nk => nk.TaiKhoanThucHien)
                .WithMany(tk => tk.DuAnNhatKyTrangThais)
                .HasForeignKey(nk => nk.TaiKhoanThucHienId)
                .OnDelete(DeleteBehavior.Restrict);

            // ==========================================
            // D. CẤU HÌNH DATA TYPES (Decimal Precision)
            // ==========================================
            // [NEW] Cấu hình này rất quan trọng để tránh warning EF Core 
            // và lỗi làm tròn tiền tệ trong SQL Server (mặc định (18,2))

            // 1. BangLuongThang (Tiền)
            var luongProps = new[] { "TongCong", "TongOt", "LuongCoBanTinh", "PhuCapTinh", "Thuong", "KhauTru", "TongLuong" };
            foreach (var prop in luongProps)
            {
                modelBuilder.Entity<BangLuongThang>()
                    .Property(prop)
                    .HasColumnType("decimal(18, 2)");
            }

            // 2. ChamCong (Giờ OT)
            modelBuilder.Entity<ChamCong>()
                .Property(x => x.SoGioOt)
                .HasColumnType("decimal(18, 2)");

            // 3. ChamCongChiTiet (Số công)
            modelBuilder.Entity<ChamCongChiTiet>()
                .Property(x => x.SoCong)
                .HasColumnType("decimal(18, 2)");

            // ==========================================
            // E. CẤU HÌNH RELATIONSHIPS MỚI 
            // (Task Management & Account Warning)
            // ==========================================

            // 1. DuAnTask - Relationships
            modelBuilder.Entity<DuAnTask>()
                .HasOne(t => t.DuAn)
                .WithMany(d => d.Tasks)
                .HasForeignKey(t => t.DuAnId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DuAnTask>()
                .HasOne(t => t.NhanVien)
                .WithMany(nv => nv.TasksNhan)
                .HasForeignKey(t => t.NhanVienId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DuAnTask>()
                .HasOne(t => t.NguoiGiao)
                .WithMany(nv => nv.TasksGiao)
                .HasForeignKey(t => t.NguoiGiaoId)
                .OnDelete(DeleteBehavior.Restrict);

            // 2. TaiKhoan - Account Warning Self-Reference
            modelBuilder.Entity<TaiKhoan>()
                .HasOne(tk => tk.TaiKhoanCanhBaoBoi_Nav)
                .WithMany(tk => tk.TaiKhoanDaCanhBao)
                .HasForeignKey(tk => tk.TaiKhoanCanhBaoBoiId)  // Changed: Added "Id" suffix
                .OnDelete(DeleteBehavior.SetNull);

            // 3. Column mappings cho các trường mới
            modelBuilder.Entity<TaiKhoan>()
                .Property(tk => tk.TrangThaiCanhBao)
                .HasDefaultValue("BINH_THUONG");

            modelBuilder.Entity<TaiKhoan>()
                .Property(tk => tk.SoLanDangNhapSai)
                .HasDefaultValue(0);

            modelBuilder.Entity<NvHoSo>()
                .Property(nv => nv.AnhStkUrl)
                .IsRequired(false);

            // ==========================================
            // THONG_BAO - Real-time Notifications
            // ==========================================
            modelBuilder.Entity<ThongBao>()
                .HasIndex(x => new { x.UserId, x.IsRead })
                .HasDatabaseName("IDX_THONG_BAO_USER_UNREAD");

            modelBuilder.Entity<ThongBao>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ThongBao>()
                .HasOne(x => x.Sender)
                .WithMany()
                .HasForeignKey(x => x.SenderId)
                .OnDelete(DeleteBehavior.SetNull);

            // ==========================================
            // DU_AN_FILE - Multi-file attachments
            // ==========================================
            modelBuilder.Entity<DuAnFile>(entity =>
            {
                entity.ToTable("du_an_file");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.DuAnId).HasColumnName("du_an_id");
                entity.Property(e => e.TenFile).HasColumnName("ten_file");
                entity.Property(e => e.DuongDanFile).HasColumnName("duong_dan_file");
                entity.Property(e => e.KichThuoc).HasColumnName("kich_thuoc");
                entity.Property(e => e.LoaiFile).HasColumnName("loai_file");
                entity.Property(e => e.NgayTao).HasColumnName("ngay_tao");
                entity.Property(e => e.TaiKhoanTaoId).HasColumnName("tai_khoan_tao_id");

                entity.HasOne(f => f.DuAn)
                    .WithMany(d => d.Files)
                    .HasForeignKey(f => f.DuAnId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.TaiKhoanTao)
                    .WithMany()
                    .HasForeignKey(f => f.TaiKhoanTaoId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(f => f.DuAnId)
                    .HasDatabaseName("idx_du_an_file_du_an_id");
            });

            // ==========================================
            // FACE RECOGNITION - Relationships Configuration
            // ==========================================

            // 1. NvFaceData -> NhanVien
            modelBuilder.Entity<NvFaceData>()
                .HasOne(f => f.NhanVien)
                .WithMany()
                .HasForeignKey(f => f.NvHoSoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NvFaceData>()
                .HasOne(f => f.NguoiTao)
                .WithMany()
                .HasForeignKey(f => f.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // 2. ChamCongFaceLog -> NhanVien (chỉ cấu hình 1 chiều)
            modelBuilder.Entity<ChamCongFaceLog>()
                .HasOne(f => f.NhanVien)
                .WithMany()
                .HasForeignKey(f => f.NvHoSoId)
                .OnDelete(DeleteBehavior.SetNull);

            // 3. ChamCong -> FaceLogVao & FaceLogRa (quan hệ phức tạp)
            // ChamCong có 2 FK về ChamCongFaceLog: FaceLogVaoId và FaceLogRaId
            // IMPORTANT: Ignore inverse navigation từ ChamCongFaceLog.ChamCong
            // vì nó conflict với 2 navigation từ ChamCong
            modelBuilder.Entity<ChamCongFaceLog>()
                .Ignore(f => f.ChamCong);

            // Cấu hình navigation cho check-in log
            modelBuilder.Entity<ChamCong>()
                .HasOne(c => c.FaceLogVao)
                .WithMany()
                .HasForeignKey(c => c.FaceLogVaoId)
                .OnDelete(DeleteBehavior.SetNull);

            // Cấu hình navigation cho check-out log
            modelBuilder.Entity<ChamCong>()
                .HasOne(c => c.FaceLogRa)
                .WithMany()
                .HasForeignKey(c => c.FaceLogRaId)
                .OnDelete(DeleteBehavior.SetNull);

            // 4. ChamCong -> Creator (TaiKhoan)
            modelBuilder.Entity<ChamCong>()
                .HasOne(c => c.Creator)
                .WithMany()
                .HasForeignKey(c => c.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // ==========================================
            // LƯƠNG CHUYÊN NGHIỆP (PhuCapLoai, NvPhuCap, BangLuongItem)
            // ==========================================
            modelBuilder.Entity<NvPhuCap>()
                .HasOne(x => x.NvHoSo)
                .WithMany()
                .HasForeignKey(x => x.NvHoSoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<NvPhuCap>()
                .HasOne(x => x.PhuCapLoai)
                .WithMany(x => x.NvPhuCaps)
                .HasForeignKey(x => x.PhuCapLoaiId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BangLuongItem>()
                .HasOne(x => x.BangLuongThang)
                .WithMany(x => x.BangLuongItems)
                .HasForeignKey(x => x.BangLuongThangId)
                .OnDelete(DeleteBehavior.Cascade);

            // ==========================================
            // NƠI LÀM VIỆC - Relationships
            // ==========================================

            // PhieuDeXuatDungCu
            modelBuilder.Entity<PhieuDeXuatDungCu>()
                .HasOne(x => x.NvHoSo)
                .WithMany()
                .HasForeignKey(x => x.NvHoSoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PhieuDeXuatDungCu>()
                .HasOne(x => x.NguoiDuyet)
                .WithMany()
                .HasForeignKey(x => x.NguoiDuyetId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PhieuDeXuatDungCu>()
                .Property(x => x.GiaTien).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<PhieuDeXuatDungCu>()
                .Property(x => x.TongTien).HasColumnType("decimal(18,2)");

            // PhieuTamUng
            modelBuilder.Entity<PhieuTamUng>()
                .HasOne(x => x.NvHoSo)
                .WithMany()
                .HasForeignKey(x => x.NvHoSoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PhieuTamUng>()
                .HasOne(x => x.NguoiDuyet)
                .WithMany()
                .HasForeignKey(x => x.NguoiDuyetId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PhieuTamUng>()
                .Property(x => x.SoTien).HasColumnType("decimal(18,2)");

            // DonDiMuon
            modelBuilder.Entity<DonDiMuon>()
                .HasOne(x => x.NvHoSo)
                .WithMany()
                .HasForeignKey(x => x.NvHoSoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DonDiMuon>()
                .HasOne(x => x.NguoiDuyet)
                .WithMany()
                .HasForeignKey(x => x.NguoiDuyetId)
                .OnDelete(DeleteBehavior.SetNull);

            // ==========================================
            // ĐỀ XUẤT GIÁM ĐỐC
            // ==========================================
            modelBuilder.Entity<DeXuatGiamDoc>(entity =>
            {
                entity.HasOne(x => x.TaiKhoanTao)
                    .WithMany()
                    .HasForeignKey(x => x.TaiKhoanTaoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.TaiKhoanDuyet)
                    .WithMany()
                    .HasForeignKey(x => x.TaiKhoanDuyetId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(x => x.TaiKhoanTaoId)
                    .HasDatabaseName("idx_dxgd_tai_khoan_tao");

                entity.HasIndex(x => x.TrangThai)
                    .HasDatabaseName("idx_dxgd_trang_thai");
            });
        }
    }
}