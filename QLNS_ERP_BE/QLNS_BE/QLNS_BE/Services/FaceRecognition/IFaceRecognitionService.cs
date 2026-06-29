namespace QLNS_BE.Services.FaceRecognition
{
    /// <summary>
    /// Interface cho Face Recognition Service
    /// Có thể implement bằng nhiều cách: Python API, Dlib, Azure, hoặc Simple Hash
    /// </summary>
    public interface IFaceRecognitionService
    {
        /// <summary>
        /// Trích xuất face encoding từ ảnh
        /// </summary>
        /// <param name="imageStream">Stream ảnh</param>
        /// <returns>Face encoding dạng JSON string, hoặc null nếu không phát hiện mặt</returns>
        Task<string?> ExtractFaceEncodingAsync(Stream imageStream);

        /// <summary>
        /// So sánh 2 face encodings
        /// </summary>
        /// <param name="encoding1">Encoding thứ nhất</param>
        /// <param name="encoding2">Encoding thứ hai</param>
        /// <returns>Độ giống nhau (0-1), càng cao càng giống</returns>
        double CompareEncodings(string encoding1, string encoding2);

        /// <summary>
        /// Tìm nhân viên từ ảnh (so sánh với tất cả face data trong DB)
        /// ⚠️ CHỈ DÙNG cho KIOSK chấm công - không yêu cầu đăng nhập
        /// </summary>
        /// <param name="imageStream">Stream ảnh cần nhận diện</param>
        /// <returns>Tuple (NvHoSoId, Confidence Score)</returns>
        Task<(int? nvId, double? confidence)> IdentifyEmployeeAsync(Stream imageStream);

        /// <summary>
        /// Xác minh khuôn mặt có thuộc về nhân viên chỉ định không
        /// ✅ DÙNG cho chấm công có đăng nhập - chỉ verify face của người đang login
        /// </summary>
        /// <param name="nvHoSoId">ID nhân viên cần verify</param>
        /// <param name="imageStream">Stream ảnh cần verify</param>
        /// <returns>Tuple (IsMatch, Confidence Score)</returns>
        Task<(bool isMatch, double? confidence)> VerifyEmployeeFaceAsync(int nvHoSoId, Stream imageStream);

        /// <summary>
        /// Kiểm tra ảnh có khuôn mặt không
        /// </summary>
        Task<bool> HasFaceAsync(Stream imageStream);

        /// <summary>
        /// Đánh giá chất lượng ảnh (độ nét, ánh sáng)
        /// </summary>
        Task<double> EvaluateImageQualityAsync(Stream imageStream);

        /// <summary>
        /// Xóa cache face encodings (gọi khi có face mới đăng ký/xóa)
        /// TỐI ƯU: Đảm bảo IdentifyEmployeeAsync load lại data mới
        /// </summary>
        void InvalidateFaceCache();
    }
}
