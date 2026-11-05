using DekParinya_Stock_UI.Models;

namespace DekParinya_Stock_UI.Services;

/// <summary>
/// บันทึกรูปแนบ GR ลง wwwroot/uploads/gr/{grId}/ (ต้นแบบ — ข้อมูลไฟล์อยู่ใน memory แต่ตัวไฟล์อยู่บนดิสก์)
/// ตรวจ: นามสกุล + ContentType + magic bytes, ขนาดไม่เกิน 5 MB/ไฟล์, สูงสุด 10 รูป/เอกสาร
/// </summary>
public class ImageUploadService
{
    public const int MaxFiles = 10;
    public const long MaxFileBytes = 5 * 1024 * 1024;
    public const long MaxRequestBytes = 60 * 1024 * 1024;

    private static readonly Dictionary<string, string> AllowedExt = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".png"] = "image/png",
        [".webp"] = "image/webp", [".gif"] = "image/gif"
    };

    private readonly IWebHostEnvironment _env;

    public ImageUploadService(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>ตรวจไฟล์ก่อนบันทึก คืนรายการข้อผิดพลาด (ว่าง = ผ่าน)</summary>
    public List<string> Validate(IReadOnlyCollection<IFormFile> files, int existingCount)
    {
        var errors = new List<string>();
        if (files.Count + existingCount > MaxFiles)
            errors.Add($"แนบรูปได้สูงสุด {MaxFiles} รูปต่อเอกสาร");

        foreach (var f in files)
        {
            var ext = Path.GetExtension(f.FileName);
            if (!AllowedExt.ContainsKey(ext) || !f.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                errors.Add($"{f.FileName}: รองรับเฉพาะไฟล์รูป JPG, PNG, WEBP, GIF");
            else if (f.Length == 0)
                errors.Add($"{f.FileName}: ไฟล์ว่าง");
            else if (f.Length > MaxFileBytes)
                errors.Add($"{f.FileName}: ขนาดเกิน 5 MB");
            else if (!HasImageSignature(f))
                errors.Add($"{f.FileName}: เนื้อไฟล์ไม่ใช่รูปภาพ");
        }
        return errors;
    }

    public async Task<List<GoodsReceiptAttachment>> SaveAsync(int grId, IEnumerable<IFormFile> files)
    {
        var folder = Path.Combine(_env.WebRootPath, "uploads", "gr", grId.ToString());
        Directory.CreateDirectory(folder);
        var result = new List<GoodsReceiptAttachment>();

        foreach (var f in files)
        {
            var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
            var id = Guid.NewGuid();
            var stored = id.ToString("N") + ext;
            await using (var stream = File.Create(Path.Combine(folder, stored)))
            {
                await f.CopyToAsync(stream);
            }
            result.Add(new GoodsReceiptAttachment
            {
                Id = id,
                FileName = Path.GetFileName(f.FileName),
                Url = $"/uploads/gr/{grId}/{stored}",
                ContentType = AllowedExt[ext],
                Size = f.Length,
                UploadedAt = DateTime.Now
            });
        }
        return result;
    }

    public void Delete(GoodsReceiptAttachment att)
    {
        var relative = att.Url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var full = Path.GetFullPath(Path.Combine(_env.WebRootPath, relative));
        var root = Path.GetFullPath(Path.Combine(_env.WebRootPath, "uploads", "gr"));
        if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(full))
            File.Delete(full);
    }

    private static bool HasImageSignature(IFormFile f)
    {
        Span<byte> h = stackalloc byte[12];
        using var s = f.OpenReadStream();
        var n = s.Read(h);
        if (n < 4) return false;
        if (h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return true;                         // JPEG
        if (h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47) return true;         // PNG
        if (h[0] == 0x47 && h[1] == 0x49 && h[2] == 0x46) return true;                         // GIF
        if (n >= 12 && h[0] == 0x52 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x46 &&
            h[8] == 0x57 && h[9] == 0x45 && h[10] == 0x42 && h[11] == 0x50) return true;        // WEBP
        return false;
    }

    public static string FormatSize(long bytes) =>
        bytes >= 1024 * 1024 ? $"{bytes / 1024d / 1024d:0.0} MB" : $"{Math.Max(1, bytes / 1024)} KB";
}
