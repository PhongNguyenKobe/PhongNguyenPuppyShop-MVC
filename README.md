# 🐾 PhongNguyenPuppy - Website thương mại điện tử bán phụ kiện và sản phẩm cho cún

> ⚠️ *Dự án này hiện vẫn đang trong quá trình phát triển và sẽ tiếp tục được hoàn thiện trong tương lai.*

## 📌 Giới thiệu
**PhongNguyenPuppy** là một Website thương mại điện tử được phát triển bằng **ASP.NET Core MVC** với kiến trúc phân tầng, tích hợp nhiều dịch vụ bên thứ ba, tối ưu SEO, bảo mật, phục vụ cho việc quản lý sản phẩm (hàng hóa) tại cửa hàng cún. Dự án bao gồm các chức năng như thêm, sửa, xóa sản phẩm, quản lý loại sản phẩm và nhà cung cấp. Đây là một dự án học tập, được cá nhân hóa từ khóa học trên YouTube để phù hợp với nhu cầu thực hành.

## 🎓 Nguồn học tập
- **Khóa học YouTube**: [ASP.NET Core MVC - Quản lý sản phẩm](https://www.youtube.com/watch?v=7hwegNKkq9k&list=PLE5Bje814fYbtRxvDgmWJ6fUpIZXtbNrb)
- **Tác giả**: *HIENLTH*

## 🛠️ Công nghệ sử dụng
- ASP.NET Core MVC (.NET 9), C#
- SQL Server, Entity Framework Core
- Bootstrap 5, JavaScript, jQuery
- MailChimp, Gmail SMTP, PayPal, VNPay APIs, GHN

---

## ✨ Tính năng nổi bật

### 🔧 Khu vực Admin
- Quản lý sản phẩm, danh mục, nhà cung cấp (CRUD đầy đủ)
- Quản lý mã giảm giá với điều kiện linh hoạt
- Dashboard thống kê doanh thu, đơn hàng, sản phẩm bán chạy
- Upload & quản lý hình ảnh sản phẩm với validation
- Tìm kiếm, lọc, sắp xếp dữ liệu động
- Gửi email tự động xác nhận đơn hàng
- Gửi, tạo email marketing gửi khách hàng
- Theo dõi lịch sử mua hàng chi tiết

### 🛒 Khu vực User
- Đăng ký, đăng nhập, quên mật khẩu qua email token bảo mật
- Giỏ hàng động cập nhật real-time bằng AJAX
- Thanh toán: COD, PayPal, VNPay
- Tự động tạo hóa đơn điện tử sau thanh toán
- Theo dõi đơn hàng và lịch sử mua hàng cá nhân

### 📣 Marketing & Giao diện
- Tích hợp MailChimp để đăng ký nhận bản tin
- Trang Subscribe chuyên biệt, call-to-action hiệu quả
- Giao diện responsive, hiện đại, thân thiện
- Phân tách layout riêng biệt cho Admin và User
- Tái sử dụng code với Partial Views, breadcrumb navigation

### 🔍 Tối ưu hóa SEO
- Meta tags động: title, description, keywords
- Open Graph & Twitter Cards (summary_large_image)
- Structured data (JSON-LD) theo chuẩn Schema.org
- Canonical URL, semantic HTML, favicon đa định dạng
- Preconnect Google Fonts, helper class `SeoData` quản lý metadata

### 🔐 Kỹ thuật & Bảo mật
- Repository Pattern & Dependency Injection
- Async/await tối ưu hiệu năng
- Tích hợp API: PayPal, VNPay, MailChimp, GHN
- Token-based authentication cho reset password
- Input validation & chống SQL injection với EF Core
- Tổ chức code rõ ràng với Areas (Admin/Customer)

## ⚙️ Yêu cầu hệ thống
| Thành phần         | Yêu cầu tối thiểu                     |
|--------------------|---------------------------------------|
| .NET SDK           | 9.0 trở lên                           |
| Cơ sở dữ liệu      | SQL Server                            |
| IDE                | Visual Studio 2022 / VS Code          |
| Thư viện frontend  | Bootstrap, jQuery (đã tích hợp sẵn)  |

## 📬 Liên hệ
- Tác giả: PhongNguyen
- Email: phongnguyenfe@gmail.com
- Cập nhật lần cuối: 12/11/2025
---
## 📬 Video demo chức năng thanh toán
- Youtube: [Demo thanh toán](https://www.youtube.com/watch?v=IL-HstHvRiM)
## 🖼️ Hình ảnh giao diện ứng dụng

Dưới đây là một số hình ảnh minh họa cho giao diện ứng dụng ASP.NET Core MVC tương tự với dự án **PhongNguyenPuppy**:

- Trang chủ
<img width="980" height="477" alt="image" src="https://github.com/user-attachments/assets/164ff2f9-6b9c-4b92-90d7-50210d4df6aa" />

- Điểm SEO khi chạy Lighthouse
<img width="975" height="512" alt="image" src="https://github.com/user-attachments/assets/4267898b-d21a-4630-98bf-0fe5ddcdf05b" />

- Sharing social media
<img width="975" height="482" alt="image" src="https://github.com/user-attachments/assets/4c605702-7362-441c-897c-02d3d44f176d" />

- Đánh giá SEOQuake trang chi tiết sản phẩm
<img width="975" height="519" alt="image" src="https://github.com/user-attachments/assets/168fcfc6-50e4-4273-8d34-85ad8c76cef3" />

- Livechat
<img width="616" height="495" alt="image" src="https://github.com/user-attachments/assets/b75f5f8d-4942-4be6-9045-08db20a04a85" />
Livechat Facebook
<img width="616" height="495" alt="image" src="https://github.com/user-attachments/assets/3740174f-a170-4e01-92e1-9a795cfcfb84" />
<img width="975" height="548" alt="image" src="https://github.com/user-attachments/assets/ac11a66b-d52e-432f-ae08-c08a0df085ee" />
<img width="975" height="490" alt="image" src="https://github.com/user-attachments/assets/94c48015-bf0e-4820-9143-9422d6b8f066" />
Livechat Tawk.to
<img width="386" height="647" alt="image" src="https://github.com/user-attachments/assets/b8bf05bf-4490-4cf9-8ba4-44b70335e891" />
<img width="880" height="469" alt="image" src="https://github.com/user-attachments/assets/22d7674c-de73-4430-ae7d-b69e1077fed5" />
- Admin
*<img width="980" height="477" alt="image" src="https://github.com/user-attachments/assets/053d3f32-d34a-4608-90b1-bfd527baa22f" />*

