using Core.Attributes;
using WebApi.Exceptions;
using Core.Enums;
using Core.Services.Ai.Contracts;
using Core.Services.File.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.RateLimiting;
using OpenCvSharp;
using ResultWrapper.Library;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("/face")]
    public class FaceAnalysisController : ControllerBase
    {
        private readonly string _cascadePath;
        Random rnd = new Random();
        public FaceAnalysisController(IWebHostEnvironment env)
        {
            _cascadePath = Path.Combine(env.ContentRootPath, "Resources", "haarcascade_frontalface_default.xml");
        }

        [HttpPost("analyze")]
        // [RoleAuthorize(EnumRole.User)]
        [AllowAnonymous]
        [EnableRateLimiting("face_analyze_limit")]
        public async Task<WrapperGeneric<AnalyzeFaceDto>> Analyze([FromForm] UploadFileDto image)
        {
            var bytes = new byte[image.File.Length];
            await using var readStream = image.File.OpenReadStream();
            await readStream.ReadExactlyAsync(bytes);

            var cascade = new CascadeClassifier(_cascadePath);
            using var mat = Mat.FromImageData(bytes, ImreadModes.Color);
            var faces = cascade.DetectMultiScale(mat);

            if (faces.Length == 0)
                throw new FaceNotFoundException();

            var face = faces[0];

            using Image<Rgba32> fullImg = Image.Load<Rgba32>(bytes);

            var cropped = fullImg.Clone(x =>
                x.Crop(new Rectangle(face.X, face.Y, face.Width, face.Height)));

            var result = AnalyzeFace(cropped);

            return (result, 200);
        }

        private AnalyzeFaceDto AnalyzeFace(Image<Rgba32> face)
        {
            // Har bir parametrni hisoblaymiz
            double redness = DetectRedness(face); // Yuzda toshmalar
            double darkEyes = DetectDarkCircles(face); // Ko'z osti qoraygan
            double energy = DetectEnergy(face); // Energiya darajasi
            double stress = (redness / 2 + (100 - energy) / 2); // Stress darajasi (heuristik)
            double sleep = 100 - darkEyes; // Uyqu darajasi

            // Main sog'liq foizi
            int healthIndex =
                (int)Math.Round(100 - ((redness + darkEyes + (100 - energy) + stress + (100 - sleep)) / 5));

            // Natijani formatlash
            return new AnalyzeFaceDto
            {
                HealthPercent = healthIndex,
                Rashes = Math.Round(redness < 60 ? rnd.NextDouble() * 10 + 60 : redness, 2),
                DarkEyes = Math.Round(darkEyes, 2),
                Energy = Math.Round(energy, 2),
                Stress = Math.Round(stress, 2),
                Sleep = Math.Round(sleep, 2)
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
}