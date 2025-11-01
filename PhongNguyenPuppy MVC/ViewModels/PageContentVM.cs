namespace PhongNguyenPuppy_MVC.ViewModels
{
    public class PageContentVM
    {
        public string Title { get; set; } = string.Empty;
        public string IntroText { get; set; } = string.Empty;
        public List<ContentSection> Sections { get; set; } = new();
        public string BottomText { get; set; } = string.Empty;
    }

    public class ContentSection
    {
        public string Heading { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? IconClass { get; set; }
    }
}