namespace PhongNguyenPuppy_MVC.Helpers
{
    public class FacebookChatSettings
    {
        public string PageId { get; set; } = string.Empty;
        public string AppId { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public string Theme { get; set; } = "blue";

        // Greeting hiển thị trong chat window (trước khi user gửi tin nhắn)
        public string LoggedInGreeting { get; set; } = "Xin chào! Chúng tôi có thể giúp gì cho bạn?";
        public string LoggedOutGreeting { get; set; } = "Xin chào! Hãy nhắn tin cho chúng tôi qua Facebook Messenger.";

        // Instant Reply (tự động gửi sau khi user gửi tin nhắn đầu tiên)
        public string InstantReplyMessage { get; set; } = "Xin chào! 👋 Cảm ơn bạn đã liên hệ với PhongNguyen Puppy. Chúng tôi sẽ phản hồi bạn sớm nhất có thể! 🐶";

        // Away message (khi offline)
        public string AwayMessage { get; set; } = "Chúng tôi hiện đang offline. Hãy để lại tin nhắn, chúng tôi sẽ phản hồi trong thời gian sớm nhất!";

        public string Language { get; set; } = "vi_VN";
    }
}