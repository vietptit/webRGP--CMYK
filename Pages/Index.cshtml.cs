using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

public class IndexModel : PageModel
{
    [BindProperty]
    public IFormFile UploadedImage { get; set; }
    public string CPath { get; set; }
    public string MPath { get; set; }
    public string YPath { get; set; }
    public string KPath { get; set; }
    public string CmykPath { get; set; }

    public void OnGet() {}

    public async Task<IActionResult> OnPostAsync()
    {
        if (UploadedImage != null && UploadedImage.Length > 0)
        {
            var fileName = "img" + DateTime.Now.Ticks;
            var folder = Path.Combine("wwwroot", "converted");
            Directory.CreateDirectory(folder);

            using var ms = new MemoryStream();
            await UploadedImage.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);
            using var img = Image.Load<Rgba32>(ms);

            int w = img.Width, h = img.Height;
            var cyan = new Image<Rgba32>(w, h);
            var magenta = new Image<Rgba32>(w, h);
            var yellow = new Image<Rgba32>(w, h);
            var black = new Image<Rgba32>(w, h);
            var cmyk = new Image<Rgba32>(w, h);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var p = img[x, y];
                    float r = p.R / 255f, g = p.G / 255f, b = p.B / 255f;
                    float k = 1 - Math.Max(r, Math.Max(g, b));
                    float c = (1 - r - k) / (1 - k + 0.0001f);
                    float m = (1 - g - k) / (1 - k + 0.0001f);
                    float yel = (1 - b - k) / (1 - k + 0.0001f);

                    cyan[x, y] = new Rgba32(0, (byte)(g * 255), (byte)(b * 255));
                    magenta[x, y] = new Rgba32((byte)(r * 255), 0, (byte)(b * 255));
                    yellow[x, y] = new Rgba32((byte)(r * 255), (byte)(g * 255), 0);
                    byte kbyte = (byte)((1 - k) * 255);
                    black[x, y] = new Rgba32(kbyte, kbyte, kbyte);

                    var rC = (byte)((1 - c) * (1 - k) * 255);
                    var gC = (byte)((1 - m) * (1 - k) * 255);
                    var bC = (byte)((1 - yel) * (1 - k) * 255);
                    cmyk[x, y] = new Rgba32(rC, gC, bC);
                }
            }

            CPath = "/converted/" + fileName + "_C.png";
            MPath = "/converted/" + fileName + "_M.png";
            YPath = "/converted/" + fileName + "_Y.png";
            KPath = "/converted/" + fileName + "_K.png";
            CmykPath = "/converted/" + fileName + "_CMYK.png";

            await cyan.SaveAsPngAsync(Path.Combine(folder, fileName + "_C.png"));
            await magenta.SaveAsPngAsync(Path.Combine(folder, fileName + "_M.png"));
            await yellow.SaveAsPngAsync(Path.Combine(folder, fileName + "_Y.png"));
            await black.SaveAsPngAsync(Path.Combine(folder, fileName + "_K.png"));
            await cmyk.SaveAsPngAsync(Path.Combine(folder, fileName + "_CMYK.png"));
        }
        return Page();
    }
}
