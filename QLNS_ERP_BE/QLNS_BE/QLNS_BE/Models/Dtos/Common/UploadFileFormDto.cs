namespace QLNS_BE.Models.Dtos.Common
{
    public class UploadFileFormDto
    {
        // form-data key: file
        public IFormFile File { get; set; } = default!;
    }
}
