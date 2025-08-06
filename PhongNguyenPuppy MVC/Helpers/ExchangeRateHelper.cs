using System.Xml.Serialization;
using System.Xml;
using System.Text;
using System.Net.Http;

namespace PhongNguyenPuppy_MVC.Helpers
{
    [XmlRoot("ExrateList")]
    public class ExrateList
    {
        [XmlElement("Exrate")]
        public List<Exrate> Exrates { get; set; }
    }

    public class Exrate
    {
        [XmlAttribute("CurrencyCode")]
        public string CurrencyCode { get; set; }

        [XmlAttribute("Sell")]
        public string Sell { get; set; }
    }

    public static class ExchangeRateHelper
    {
        public static async Task<double?> ConvertVNDtoUSDAsync(double amountVND)
        {
            string url = "https://portal.vietcombank.com.vn/Usercontrols/TVPortal.TyGia/pXML.aspx?b=8";
            using var client = new HttpClient();

            try
            {
                var xml = await client.GetStringAsync(url);
                var serializer = new XmlSerializer(typeof(ExrateList));
                using var reader = new StringReader(xml);
                var data = (ExrateList)serializer.Deserialize(reader);

                var usdRate = data?.Exrates?.FirstOrDefault(x => x.CurrencyCode == "USD");
                if (usdRate != null && double.TryParse(usdRate.Sell, out double rate))
                {
                    return Math.Round(amountVND / rate, 2);
                }
            }
            catch
            {
                // Log lỗi nếu muốn
                return null;
            }

            return null;
        }
    }
}
