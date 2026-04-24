using BRB.Core.File;
using Core;
using WebApi.Exceptions;
using Core.Attributes;
using Core.Enums;
using Core.Services.File.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using WebCore.Controller;
using WebCore.Enum;

namespace WebApi.Controllers;

/// <inheritdoc />
[ApiController]
[Route("/file")]
[RoleAuthorize(EnumRole.User)]
public class FileController(FileService fileService) : AuthorizedController
{
    /// <summary>
    /// Upload a file
    /// </summary>
    [HttpPost]
#if DEBUG
    [AllowAnonymous]
#endif
    public async Task<Wrapper> Upload([FromForm] UploadFileDto dto) =>
        fileService.ConvertToUrl(await fileService.Upload(dto.File));

    /// <summary>
    /// Upload a image with resize options
    /// Allowed content types:
    /// image/png
    /// image/jpeg
    /// image/bmp
    /// image/webp
    /// </summary>
    [HttpPost("process-image-save")]
#if DEBUG
    [AllowAnonymous]
#endif
    public async Task<Wrapper> UploadImageWithResize([FromForm] UploadFileWithResizeDto dto)
    {
        IImageFormat imageFormat = dto.File.ContentType switch
        {
            "image/png" => PngFormat.Instance,
            "image/jpeg" => JpegFormat.Instance,
            "image/bmp" => BmpFormat.Instance,
            "image/webp" => WebpFormat.Instance,
            _ => throw new FileTypeNotAllowedException()
        };

        var file = dto.File;

        using var image = await Image.LoadAsync(file.OpenReadStream());
        image.Mutate(x => x.Resize(new Size(dto.Width, dto.Height)));

        var fileStream = new MemoryStream();
        await image.SaveAsync(fileStream, imageFormat, CancellationToken.None);

        fileStream.Position = 0;

        return fileService.ConvertToUrl(await fileService.SaveFileAsync(file.FileName, fileStream));
    }
}