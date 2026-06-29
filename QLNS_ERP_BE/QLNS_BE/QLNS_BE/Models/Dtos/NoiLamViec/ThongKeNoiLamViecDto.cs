namespace QLNS_BE.Models.Dtos.NoiLamViec
{
    public class ThongKeNoiLamViecDto
    {
        // Phiếu đề xuất dụng cụ
        public int TongDeXuat { get; set; }
        public int DeXuatChoDuyet { get; set; }
        public int DeXuatDaDuyet { get; set; }
        public int DeXuatTuChoi { get; set; }

        // Phiếu tạm ứng
        public int TongTamUng { get; set; }
        public int TamUngChoDuyet { get; set; }
        public int TamUngDaDuyet { get; set; }
        public int TamUngTuChoi { get; set; }

        // Đơn đi muộn / về sớm
        public int TongDiMuon { get; set; }
        public int DiMuonChoDuyet { get; set; }
        public int DiMuonDaDuyet { get; set; }
        public int DiMuonTuChoi { get; set; }
    }
}
