using ClosedXML.Excel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.ConstrainedExecution;

namespace CarsList
{

    //public class Image
    //{
    //    public string Id { get; set; }
    //    public string Alt { get; set; }
    //    public string FileName { get; set; }
    //    public string Preview { get; set; }
    //    public int Width { get; set; }
    //    public int Height { get; set; }
    //}

    //public class Image
    //{
    //    public string Id { get; set; }
    //    public string Alt { get; set; }
    //    public string FileName { get; set; }
    //    public string Preview { get; set; }
    //    public int Width { get; set; }
    //    public int Height { get; set; }
    //}


    //public class KeySpecification
    //{
    //    public int Id { get; set; }
    //    public string Title { get; set; }
    //    public string PrimaryValue { get; set; }
    //    public string SecondaryValue { get; set; }
    //}

    //public class Product
    //{
    //    public string Id { get; set; }
    //    public string Title { get; set; }
    //    public string EnglishTitle { get; set; }
    //    public string Slug { get; set; }
    //    public Image Image { get; set; }
    //    public List<KeySpecification> KeySpecifications { get; set; }
    //}

    //public class ProductsResponse
    //{
    //    public List<Product> Source { get; set; }
    //}

    //public class RootResponse
    //{
    //    public ProductsResponse Products { get; set; }
    //}

    //class Program
    //{
    //    static async Task Main()
    //    {
    //        string url = "https://api2.zoomit.ir/catalog/api/products/search?brands=peugeot%2Cbmw%2Csaipa%2Cchery%2Ctoyota%2Cmvm%2Cikco%2Cmercedes-benz%2Chyundai%2Cnissan%2Cjac%2Ckia%2Ctesla%2Cmazda%2Crenault%2Chaima%2Caston-martin%2Caudi%2Cchangan%2Csuzuki%2Cvolkswagen%2Cfarda%2Clifan%2Cmitsubishi%2Clexus%2Cmg%2Cporsche%2Cssangyong%2Csubaru%2Cdongfeng%2Cdayun-group%2Cxtrim%2Cbrilliance%2Chaval%2Calfa-romeo%2Cbesturn%2Ccitroen%2Cgreat-wall%2Czx-auto%2Cgac%2Cmini%2Cgeely%2Cbaic-group%2Chonda%2Cvolvo%2Clamari%2Cmaxmotor%2Cdaewoo%2Cmaserati%2Cbisu%2Cuaz%2Chongqi%2Cacura%2Cswm%2Czotye%2Cproton%2Cland-rover%2Cfiat%2Ckmc%2Cfaw%2Cds%2Csmart%2Camico%2Cbyd%2Chanteng%2Cisuzu%2Cborgward%2Copel%2Cbestune%2Cpars-khodro%2Cseat%2Calpine%2Clotus%2Cleapmotor%2Cjmc%2Cneta%2Cxiaomi%2Cskoda%2Ckarmania%2Cskywell%2Cavatr%2Cvenucia%2Crayen%2Cmaxus%2Cvgv%2Cpadra%2Csoueast%2Cdeepal%2Clynk-and-co%2Cxpeng%2Cartaban-motors&pageNumber=1&categorySlug=car&pageSize=200";

    //        using HttpClient client = new HttpClient();
    //        var response = await client.GetAsync(url);

    //        if (!response.IsSuccessStatusCode)
    //        {
    //            Console.WriteLine("Eror in recive");
    //            return;
    //        }

    //        string jsonStr = await response.Content.ReadAsStringAsync();

    //        // Deserialize به کلاس RootResponse
    //        RootResponse root = JsonConvert.DeserializeObject<RootResponse>(jsonStr);

    //        List<Product> products = root?.Products?.Source;

    //        if (products != null)
    //        {
    //            foreach (var product in products)
    //            {
    //                Console.WriteLine($"ID: {product.Id}, Title: {product.Title}, EnglishTitle: {product.EnglishTitle}");
    //                Console.WriteLine($"Image Preview: {product.Image?.Preview?.Substring(0, 50)}...");

    //                if (product.KeySpecifications != null)
    //                {
    //                    foreach (var spec in product.KeySpecifications)
    //                    {
    //                        Console.WriteLine($"  Spec: {spec.Title} => {spec.PrimaryValue}, {spec.SecondaryValue}");
    //                    }
    //                }
    //                Console.WriteLine("--------------------------------------");
    //            }
    //            var d = products[0].KeySpecifications[0].Title;
    //            var ds = products[0].KeySpecifications[0].PrimaryValue;
    //            var df = products[0].KeySpecifications[0].SecondaryValue;
    //            Console.ReadKey();
    //        }
    //        else
    //        {

    //            Console.WriteLine("No Found");
    //        }
    //    }
    //}


    //public class Image
    //{
    //    public string Id { get; set; }
    //    public string Alt { get; set; }
    //    public string FileName { get; set; }
    //    public string Preview { get; set; }
    //    public int Width { get; set; }
    //    public int Height { get; set; }
    //}

    //public class KeySpecification
    //{
    //    public int Id { get; set; }
    //    public string Title { get; set; }
    //    public string PrimaryValue { get; set; }
    //    public string SecondaryValue { get; set; }
    //}

    //public class Product
    //{
    //    public string Id { get; set; }
    //    public string Title { get; set; }
    //    public string EnglishTitle { get; set; }
    //    public string Slug { get; set; }
    //    public Image Image { get; set; }
    //    public List<KeySpecification> KeySpecifications { get; set; }
    //}

    //public class ProductsResponse
    //{
    //    public List<Product> Source { get; set; }
    //}

    //public class RootResponse
    //{
    //    public ProductsResponse Products { get; set; }
    //}



    //class Program
    //{
    //    static async Task Main()
    //    {
    //        string baseUrl = "https://api2.zoomit.ir/catalog/api/products/search?brands=peugeot,bmw,saipa,chery,toyota,mvm,ikco,mercedes-benz,hyundai,nissan,jac,kia,tesla,mazda,renault,haima,aston-martin,audi,changan,suzuki,volkswagen,farda,lifan,mitsubishi,lexus,mg,porsche,ssangyong,subaru,dongfeng,dayun-group,xtrim,brilliance,haval,alfa-romeo,besturn,citroen,great-wall,zx-auto,gac,mini,geely,baic-group,honda,volvo,lamari,maxmotor,daewoo,maserati,bisu,uaz,hongqi,acura,swm,zotye,proton,land-rover,fiat,kmc,faw,ds,smart,amico,byd,hanteng,isuzu,borgward,opel,bestune,pars-khodro,seat,alpine,lotus,leapmotor,jmc,neta,xiaomi,skoda,karmania,skywell,avatr,venucia,rayen,maxus,vgv,padra,soueast,deepal,lynk-and-co,xpeng,artaban-motors&categorySlug=car&pageSize=200";

    //        string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
    //        string path256 = Path.Combine(basePath, "256");
    //        string path1920 = Path.Combine(basePath, "1920");
    //        Directory.CreateDirectory(path256);
    //        Directory.CreateDirectory(path1920);

    //        using HttpClient client = new HttpClient();
    //        int page = 1;
    //        int totalDownloaded = 0;

    //        Console.WriteLine(" Strat ...");

    //        List<Product> aLLproducts=new List<Product>();

    //        while (true)
    //        {
    //            string url = $"{baseUrl}&pageNumber={page}";
    //            Console.WriteLine($"\n recive from {page}...");

    //            var response = await client.GetAsync(url);
    //            if (!response.IsSuccessStatusCode)
    //            {
    //                Console.WriteLine($" Error in {page} page: {response.StatusCode}");
    //                break;
    //            }

    //            string jsonStr = await response.Content.ReadAsStringAsync();
    //            RootResponse root = JsonConvert.DeserializeObject<RootResponse>(jsonStr);
    //            List<Product> products = root?.Products?.Source;

    //            if (products == null || products.Count == 0)
    //            {
    //                Console.WriteLine(" End .");
    //                break;
    //            }
    //            aLLproducts.AddRange(products);

    //            //foreach (var product in products)
    //            //{
    //            //    if (product.Image?.Id == null)
    //            //        continue;

    //            //    string safeName = MakeSafeFileName(product.Title ?? product.Image.FileName ?? product.Id);

    //            // URLها برای دو کیفیت
    //            //    string url256 = $"https://api2.zoomit.ir/media/{product.Image.Id}?w=256&q=75";
    //            //    string url1920 = $"https://api2.zoomit.ir/media/{product.Image.Id}?w=1920&q=75";

    //            //    string pathFile256 = Path.Combine(path256, safeName + ".jpg");
    //            //    string pathFile1920 = Path.Combine(path1920, safeName + ".jpg");

    //            //    try
    //            //    {
    //            //        // دانلود تصویر 256
    //            //        if (!File.Exists(pathFile256))
    //            //        {
    //            //            var bytes256 = await client.GetByteArrayAsync(url256);
    //            //            await File.WriteAllBytesAsync(pathFile256, bytes256);
    //            //        }

    //            //        // دانلود تصویر 1920
    //            //        if (!File.Exists(pathFile1920))
    //            //        {
    //            //            var bytes1920 = await client.GetByteArrayAsync(url1920);
    //            //            await File.WriteAllBytesAsync(pathFile1920, bytes1920);
    //            //        }

    //            //        Console.WriteLine($"Saved : {safeName}");
    //            //        totalDownloaded++;
    //            //    }
    //            //    catch (Exception ex)
    //            //    {
    //            //        Console.WriteLine($"⚠️ خطا در {safeName}: {ex.Message}");
    //            //    }
    //            //}

    //            page++;
    //        }

    //        Console.WriteLine($"\n🎉 تمام صفحات واکشی شد. مجموع عکس‌های دانلود شده: {totalDownloaded}");
    //    }

    //    static string MakeSafeFileName(string name)
    //    {
    //        foreach (char c in Path.GetInvalidFileNameChars())
    //            name = name.Replace(c, '_');
    //        return name.Length > 100 ? name.Substring(0, 100) : name;
    //    }

    public class ProductInfo
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string EnglishTitle { get; set; }
        public string Slug { get; set; }

        // اطلاعات برند
        public string BrandId { get; set; }
        public string BrandTitle { get; set; }
        public string BrandEnglishTitle { get; set; }
        public string BrandSlug { get; set; }

        // تصویر
        public string ImageId { get; set; }
        public string ImageUrl256 { get; set; }
        public string ImageUrl1920 { get; set; }
    }

    public class ZoomitFetcher
    {
        private static readonly HttpClient client = new HttpClient();
        private const string BaseUrl = "https://api2.zoomit.ir/catalog/api/products/search";
        private const string Brands = "peugeot,bmw,saipa,chery,toyota,mvm,ikco,mercedes-benz,hyundai,nissan,jac,kia,tesla,mazda,renault,haima,aston-martin,audi,changan,suzuki,volkswagen,farda,lifan,mitsubishi,lexus,mg,porsche,ssangyong,subaru,dongfeng,dayun-group,xtrim,brilliance,haval,alfa-romeo,besturn,citroen,great-wall,zx-auto,gac,mini,geely,baic-group,honda,volvo,lamari,maxmotor,daewoo,maserati,bisu,uaz,hongqi,acura,swm,zotye,proton,land-rover,fiat,kmc,faw,ds,smart,amico,byd,hanteng,isuzu,borgward,opel,bestune,pars-khodro,seat,alpine,lotus,leapmotor,jmc,neta,xiaomi,skoda,karmania,skywell,avatr,venucia,rayen,maxus,vgv,padra,soueast,deepal,lynk-and-co,xpeng,artaban-motors";

        public async Task<List<ProductInfo>> FetchAllProductsAsync()
        {
            var allProducts = new List<ProductInfo>();
            int page = 1;

            string dir256 = Path.Combine(Environment.CurrentDirectory, "Images", "256");
            string dir1920 = Path.Combine(Environment.CurrentDirectory, "Images", "1920");
            Directory.CreateDirectory(dir256);
            Directory.CreateDirectory(dir1920);

            while (true)
            {
                string url = $"{BaseUrl}?brands={Brands}&pageNumber={page}&categorySlug=car&pageSize=200";
                Console.WriteLine($"📦 Fetching page {page} ...");

                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode) break;

                var jsonStr = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(jsonStr);

                var source = json["products"]?["source"] as JArray;
                if (source == null || source.Count == 0)
                    break; // دیگه صفحه‌ای وجود نداره

                foreach (var item in source)
                {
                    var imageId = item["image"]?["id"]?.ToString();
                    var brand = item["brand"];

                    var product = new ProductInfo
                    {
                        Id = item["id"]?.ToString(),
                        Title = item["title"]?.ToString(),
                        EnglishTitle = item["englishTitle"]?.ToString(),
                        Slug = item["slug"]?.ToString(),

                        BrandId = brand?["id"]?.ToString(),
                        BrandTitle = brand?["title"]?.ToString(),
                        BrandEnglishTitle = brand?["englishTitle"]?.ToString(),
                        BrandSlug = brand?["slug"]?.ToString(),

                        ImageId = imageId,
                        //ImageUrl256 = imageId != null ? $"https://api2.zoomit.ir/media/{imageId}?w=256&q=75" : null,
                        //ImageUrl1920 = imageId != null ? $"https://api2.zoomit.ir/media/{imageId}?w=1920&q=75" : null
                    };

                    allProducts.Add(product);

                    if (!string.IsNullOrEmpty(imageId))
                    {
                        //await DownloadImageAsync(product.ImageUrl256, Path.Combine(dir256, $"{imageId}.jpg"));
                        //await DownloadImageAsync(product.ImageUrl1920, Path.Combine(dir1920, $"{imageId}.jpg"));
                    }
                }

                page++;
            }

            Console.WriteLine("✅ تمام صفحات واکشی شدند.");

            // استخراج لیست برندها از countByBrands
            try
            {
                var brandsJson = await client.GetStringAsync($"{BaseUrl}?brands={Brands}&pageNumber=1&categorySlug=car&pageSize=1");
                var jObj = JObject.Parse(brandsJson);
                var countByBrands = jObj["countByBrands"] as JArray;
                if (countByBrands != null)
                {
                    Console.WriteLine("\n📛 لیست برندها:");
                    foreach (var b in countByBrands)
                        Console.WriteLine($"- {b["slug"]}");
                }
            }
            catch { }

            return allProducts;
        }

        private async Task DownloadImageAsync(string url, string savePath)
        {
            try
            {
                var bytes = await client.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(savePath, bytes);
                Console.WriteLine($"📸 Saved: {Path.GetFileName(savePath)}");
            }
            catch
            {
                Console.WriteLine($"⚠️ Failed: {url}");
            }
        }
    }

    class Program
    {
        static async Task Main()
        {
            var fetcher = new ZoomitFetcher();
            var products = await fetcher.FetchAllProductsAsync();

            Console.WriteLine($"\nCount  : {products.Count}");

            SaveToExcel(products);
        }
        static void SaveToExcel(List<ProductInfo> products)
        {
            string filePath = Path.Combine(Environment.CurrentDirectory, "Products.xlsx");
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Products");

            // هدرها
            ws.Cell(1, 1).Value = "ID";
            ws.Cell(1, 2).Value = "Title";
            ws.Cell(1, 3).Value = "EnglishTitle";
            ws.Cell(1, 4).Value = "Slug";
            ws.Cell(1, 5).Value = "BrandId";
            ws.Cell(1, 6).Value = "BrandTitle";
            ws.Cell(1, 7).Value = "BrandEnglishTitle";
            ws.Cell(1, 8).Value = "BrandSlug";
            ws.Cell(1, 9).Value = "ImageId";
            ws.Cell(1, 10).Value = "ImageUrl256";
            ws.Cell(1, 11).Value = "ImageUrl1920";

            int row = 2;
            foreach (var p in products)
            {
                ws.Cell(row, 1).Value = p.Id;
                ws.Cell(row, 2).Value = p.Title;
                ws.Cell(row, 3).Value = p.EnglishTitle;
                ws.Cell(row, 4).Value = p.Slug;
                ws.Cell(row, 5).Value = p.BrandId;
                ws.Cell(row, 6).Value = p.BrandTitle;
                ws.Cell(row, 7).Value = p.BrandEnglishTitle;
                ws.Cell(row, 8).Value = p.BrandSlug;
                ws.Cell(row, 9).Value = p.ImageId;
                ws.Cell(row, 10).Value = p.ImageUrl256;
                ws.Cell(row, 11).Value = p.ImageUrl1920;
                row++;
            }

            ws.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
            Console.WriteLine($"📘 فایل Excel ذخیره شد: {filePath}");
        }
    }
}


