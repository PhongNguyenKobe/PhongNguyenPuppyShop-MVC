using System;
using System.Xml.Serialization;

namespace PhongNguyenPuppy_MVC.Helpers
{
    [XmlRoot("url")]
    public class SitemapNode
    {
        [XmlElement("loc")]
        public string Loc { get; set; }

        [XmlElement("lastmod")]
        public string LastMod { get; set; }

        [XmlElement("changefreq")]
        public string ChangeFreq { get; set; }

        [XmlElement("priority")]
        public string Priority { get; set; }

        public SitemapNode()
        {
            // Giá trị mặc định
            ChangeFreq = "weekly";
            Priority = "0.8"; // Ưu tiên mặc định
            LastMod = DateTime.UtcNow.ToString("yyyy-MM-dd");
        }

        public SitemapNode(string loc, string changeFreq = "weekly", string priority = "0.8")
        {
            Loc = loc;
            ChangeFreq = changeFreq;
            Priority = priority;
            LastMod = DateTime.UtcNow.ToString("yyyy-MM-dd");
        }
    }
}