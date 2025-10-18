using HtmlAgilityPack;

namespace CarsList
{
    public class CarScraper
    {
        public List<CarProduct> ParseCars(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var result = new List<CarProduct>();

            // هر محصول در یک div با کلاس خاص قرار دارد
            var carNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'sc-adfd7063-0')]");

            if (carNodes == null)
                return result;

            foreach (var node in carNodes)
            {
                try
                {
                    var nameNode = node.SelectSingleNode(".//a//span[contains(@class, 'callout-bold')]");
                    var imgNode = node.SelectSingleNode(".//img");
                    var linkNode = node.SelectSingleNode(".//a[contains(@href, '/product/')]");
                    var priceNode = node.SelectSingleNode(".//span[@aria-label='قیمت']");

                    var car = new CarProduct
                    {
                        Name = nameNode?.InnerText.Trim(),
                        Link = linkNode?.GetAttributeValue("href", null),
                        ImageUrl = imgNode?.GetAttributeValue("src", null),
                        Price = priceNode?.InnerText.Trim(),
                    };

                    // ویژگی‌ها مثل "سدان", "5 سرنشین" و غیره
                    var specs = node.SelectNodes(".//span[contains(@class, 'sc-adfd7063-1')]");
                    if (specs != null && specs.Count >= 8)
                    {
                        car.BodyType = specs.ElementAtOrDefault(0)?.InnerText.Trim();
                        car.Seats = specs.ElementAtOrDefault(1)?.InnerText.Trim();
                        car.EngineVolume = specs.ElementAtOrDefault(2)?.InnerText.Trim();
                        car.FuelUsage = specs.ElementAtOrDefault(3)?.InnerText.Trim();
                        car.Power = specs.ElementAtOrDefault(4)?.InnerText.Trim();
                        car.Torque = specs.ElementAtOrDefault(5)?.InnerText.Trim();
                        car.Gearbox = specs.ElementAtOrDefault(6)?.InnerText.Trim();
                        car.GearsCount = specs.ElementAtOrDefault(7)?.InnerText.Trim();
                    }

                    result.Add(car);
                }
                catch { /* نادیده بگیر و ادامه بده */ }
            }

            return result;
        }
    }

    public class CarProduct
    {
        public string Brand { get; set; }
        public string Name { get; set; }
        public string Link { get; set; }
        public string ImageUrl { get; set; }
        public string BodyType { get; set; }           // سدان، SUV و ...
        public string Seats { get; set; }              // 5 سرنشین
        public string EngineVolume { get; set; }       // 1498 سی‌سی
        public string FuelUsage { get; set; }          // 7.5 لیتر در 100 کیلومتر
        public string Power { get; set; }              // 156 اسب بخار
        public string Torque { get; set; }             // 210 نیوتن‌متر
        public string Gearbox { get; set; }            // جعبه‌دنده خودکار
        public string GearsCount { get; set; }         // 9 دنده
        public string Price { get; set; }              // قیمت به تومان
    }
}
