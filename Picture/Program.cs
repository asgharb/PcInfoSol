using System;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;

namespace Picture
{
    class Program
    {
        static void Main()
        {
            string sourcePath = "C:\\Users\\a.bizaval.DOURNA\\Desktop\\New folder (3)\\New folder";

            if (!Directory.Exists(sourcePath))
            {
                Console.WriteLine("پوشه یافت نشد!");
                return;
            }

            string destPath = Path.Combine(sourcePath, "CompressedSmart");
            Directory.CreateDirectory(destPath);

            var files = Directory.GetFiles(sourcePath)
                .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Console.WriteLine($"🔍 تعداد فایل‌ها: {files.Count}");

            foreach (var file in files)
            {
                FileInfo info = new FileInfo(file);
                if (info.Length < 500 * 1024)
                    continue;

                using (Image image = Image.Load(file))
                {
                    // حذف متادیتا (EXIF)
                    image.Metadata.ExifProfile = null;

                    // چرخش صحیح خودکار و حذف نویز رنگی جزئی
                    image.Mutate(x => x.AutoOrient().GaussianBlur(0.3f));

                    // اگر تصویر خیلی بزرگ است، فقط عرض را تا 1920 پیکسل کاهش می‌دهد
                    if (image.Width > 1920)
                    {
                        int newHeight = (int)(image.Height * (1920.0 / image.Width));
                        image.Mutate(x => x.Resize(1920, newHeight));
                    }

                    string newFile = Path.Combine(destPath, Path.GetFileNameWithoutExtension(file) + ".jpg");

                    // فشرده‌سازی تدریجی
                    int quality = 90;
                    byte[] result;
                    do
                    {
                        using var ms = new MemoryStream();
                        image.Save(ms, new JpegEncoder { Quality = quality });
                        result = ms.ToArray();
                        quality -= 5;
                    } while (result.Length > 100 * 1024 && quality > 40);

                    File.WriteAllBytes(newFile, result);
                    Console.WriteLine($"✅ {Path.GetFileName(file)} → {result.Length / 1024}KB (Q={quality + 5})");
                }
            }

            Console.WriteLine($"\n🎉 انجام شد! خروجی در پوشه: {destPath}");
        }
    }
}
