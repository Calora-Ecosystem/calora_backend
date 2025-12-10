using BRB.Core.File;
using Core.Services.File.Contracts;
using Microsoft.AspNetCore.Mvc;
using OpenCvSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;

namespace Calora.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FaceAnalysisController : ControllerBase
    {
        private readonly string _cascadePath;

        public FaceAnalysisController(IWebHostEnvironment env)
        {
            _cascadePath = Path.Combine(env.ContentRootPath, "Resources", "haarcascade_frontalface_default.xml");
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> Analyze([FromForm] UploadFileDto1 image)
        {
            if (image == null || image.File == null)
                return BadRequest("Rasm yuborilmadi");

            using var ms = new MemoryStream();
            await image.File.CopyToAsync(ms);
            var bytes = ms.ToArray();

            var cascade = new CascadeClassifier(_cascadePath);
            using var mat = Mat.FromImageData(bytes, ImreadModes.Color);
            var faces = cascade.DetectMultiScale(mat);

            if (faces.Length == 0)
                return Ok(new { success = false, message = "Yuz aniqlanmadi" });

            var face = faces[0];

            ms.Position = 0;
            using Image<Rgba32> fullImg = Image.Load<Rgba32>(ms);

            var cropped = fullImg.Clone(x =>
                x.Crop(new Rectangle(face.X, face.Y, face.Width, face.Height)));

            var result = AnalyzeFace(cropped);

            return Ok(result);
        }

        private object AnalyzeFace(Image<Rgba32> face)
        {
            // Har bir parametrni hisoblaymiz
            double redness = DetectRedness(face);           // Yuzda toshmalar
            double darkEyes = DetectDarkCircles(face);     // Ko'z osti qoraygan
            double energy = DetectEnergy(face);            // Energiya darajasi
            double stress = (redness / 2 + (100 - energy) / 2); // Stress darajasi (heuristik)
            double sleep = 100 - darkEyes;                // Uyqu darajasi

            // Main sog'liq foizi
            int healthIndex = (int)Math.Round(100 - ((redness + darkEyes + (100 - energy) + stress + (100 - sleep)) / 5));

            // Natijani formatlash
            return new
            {
                main = new
                {
                    healthPercent = healthIndex,    // 79/100 kabi
                    text = $"Sog'lomlik foizi: {healthIndex}/100"
                },
                details = new[]
                {
                    new { title = "Yuzda toshmalar bor - jigaringiz yoki oshqozoningizni tekshirtiring.", percent = (int)redness },
                    new { title = "Ko’z osti qoraygan - Uyqu sifatini yaxshilang.", percent = (int)darkEyes },
                    new { title = "Energiya darajasi: O‘rtacha - Ko’proq suv iching va faol bo‘ling.", percent = (int)energy },
                    new { title = "Stress darajasi: Yuqori - dam olish va meditatsiya qiling.", percent = (int)stress },
                    new { title = "Uyqu darajasi: Yetarli emas - Kechqurung ertaroq uxlashni odat qiling !", percent = (int)sleep }
                }
            };
        }


        private double DetectRedness(Image<Rgba32> img)
        {
            int w = img.Width, h = img.Height;
            double red = 0, total = w * h;

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var p = img[x, y];
                    if (p.R > 180 && p.G < 120)
                        red++;
                }

            return (red / total) * 100;
        }

        private double DetectDarkCircles(Image<Rgba32> img)
        {
            int w = img.Width, h = img.Height;
            int startY = (int)(h * 0.65);

            double dark = 0, count = 0;

            for (int y = startY; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var p = img[x, y];
                    double brightness = (p.R + p.G + p.B) / 3.0;

                    if (brightness < 80)
                        dark++;

                    count++;
                }

            return (dark / count) * 100;
        }

        private double DetectEnergy(Image<Rgba32> img)
        {
            int w = img.Width, h = img.Height;
            double total = 0, count = w * h;

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var p = img[x, y];
                    total += (p.R + p.G + p.B) / 3.0;
                }

            double avg = total / count;
            return Math.Min(100, avg / 2);
        }
    }

    public class UploadFileDto1
    {
        public IFormFile File { get; set; } = default!;
    }
}
