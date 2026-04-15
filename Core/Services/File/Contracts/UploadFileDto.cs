using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Core.Services.File.Contracts;

public class UploadFileDto
{
    [Required(ErrorMessage = "File is required")]
    public IFormFile File { get; set; } = default!;
}

public class UploadFileWithResizeDto : UploadFileDto
{
    /// <summary>
    /// Default: 128
    /// </summary>
    public int Width { get; set; } = 128;

    /// <summary>
    /// Default: 128
    /// </summary>
    public int Height { get; set; } = 128;
}