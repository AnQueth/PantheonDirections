using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Text.RegularExpressions;
using Tesseract;

namespace PantheonDirectionsWeb.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class OcrController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] OcrRequest request)
        {
            if (request?.Image == null)
            {
                return BadRequest("Invalid image data.");
            }

            try
            {
                var imageData = Convert.FromBase64String(request.Image.Split(',')[1]);
                using var ms = new MemoryStream(imageData);
                using var bitmap = new Bitmap(ms);

                var ocrResult = PerformOcr(bitmap);
                return Ok(ocrResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        private (double x, double y) Parse(string text)
        {
            string pattern = @"([\d\.\-]+) ([\d\.\-]+) ([\d\.\-]+) ([\d\.\-]+)";
            Regex regex = new Regex(pattern);
            Match match = regex.Match(text);
            if (match.Success)
            {
                return (double.Parse(match.Groups[1].Value), double.Parse(match.Groups[3].Value));

            }
            return default;
        }


        private OcrResult PerformOcr(Bitmap bitmap)
        {
            using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
            using var img = PixConverter.ToPix(bitmap);
            using var page = engine.Process(img);
            var text = page.GetText();

            (var x, var y) = Parse(text);
 
            return new OcrResult
            {
                X = x,
                Y = y
            };
        }
    }

    public class OcrRequest
    {
        public string Image { get; set; }
    }

    public class OcrResult
    {
        public double X { get; set; }
        public double Y { get; set; }

    }
}
