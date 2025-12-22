using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PhongNguyenPuppy_MVC.Areas.Admin.Helpers;
using PhongNguyenPuppy_MVC.Areas.Admin.Services;
using PhongNguyenPuppy_MVC.Data;
using PhongNguyenPuppy_MVC.Helpers;
using PhongNguyenPuppy_MVC.Models;
using PhongNguyenPuppy_MVC.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Secret.json", optional: true, reloadOnChange: true);

// Cấu hình lowercase URLs cho toàn bộ ứng dụng
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = false; // Giữ query string case-sensitive
});

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<PhongNguyenPuppyContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("PhongNguyenPuppy"));
});

builder.Services.AddDistributedMemoryCache();

builder.Services.AddScoped<IKhachHangRepository, KhachHangRepository>();
builder.Services.AddScoped<IDichVuGuiEmail, DichVuGuiEmail>();
builder.Services.AddScoped<KhachHangService>();
builder.Services.AddScoped<IDichVuThongKe, DichVuThongKe>();

// Đăng ký HttpClient và GHNService
builder.Services.AddHttpClient<IGHNService, GHNService>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<MyEmailHelper>();
// Đăng ký TinyMceSettings từ appsettings.Secret.json
builder.Services.Configure<TinyMceSettings>(
    builder.Configuration.GetSection("TinyMceSettings"));
// Đăng ký AppSettings từ appsettings.Secret.json
builder.Services.Configure<AppSettings>(
    builder.Configuration.GetSection("AppSettings"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "CustomerScheme";
    options.DefaultChallengeScheme = "CustomerScheme";
})
.AddCookie("CustomerScheme", options =>
{
    options.LoginPath = "/khachhang/dangnhap";
    options.AccessDeniedPath = "/khachhang/accessdenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
})
.AddCookie("AdminScheme", options =>
{
    options.LoginPath = "/admin/admin/login"; 
    options.AccessDeniedPath = "/admin/admin/accessdenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});

builder.Services.AddSingleton(x => new PaypalClient(
    builder.Configuration["PaypalOptions:AppId"] ?? throw new ArgumentNullException("ClientId is not configured"),
    builder.Configuration["PaypalOptions:AppSecret"] ?? throw new ArgumentNullException("ClientSecret is not configured"),
    builder.Configuration["PaypalOptions:Mode"] ?? throw new ArgumentNullException("Mode is not configured")
));

builder.Services.AddSingleton<IVnPayService, VnPayService>();

builder.Services.Configure<FacebookChatSettings>(builder.Configuration.GetSection("FacebookChatSettings"));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<PhongNguyenPuppy_MVC.Services.IViewRenderService, PhongNguyenPuppy_MVC.Services.ViewRenderService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Chỉ redirect GET requests (NGOẠI TRỪ ADMIN AREA)
app.Use(async (context, next) =>
{
    var url = context.Request.Path.Value;
    var method = context.Request.Method;

    //THÊM: Bỏ qua lowercase cho Admin Area
    if (method == "GET" &&
        !string.IsNullOrEmpty(url) &&
        url != url.ToLower() &&
        !url.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect(url.ToLower() + context.Request.QueryString, permanent: true);
        return;
    }

    await next();
});

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();


// THÊM: Route cho Admin Area
app.MapAreaControllerRoute(
    name: "admin_area",
    areaName: "Admin",
    pattern: "Admin/{controller=Admin}/{action=Login}/{id?}");

app.MapControllerRoute(
    name: "sitemap",
    pattern: "sitemap.xml",
    defaults: new { controller = "Sitemap", action = "Index" });


app.MapControllerRoute(
    name: "sitemap",
    pattern: "sitemap.xml",
    defaults: new { controller = "Sitemap", action = "Index" });

// THAY ĐỔI ROUTE HANGHOA SANG SANPHAM
app.MapControllerRoute(
    name: "product-detail",
    pattern: "san-pham/{slug}-{id:int}",
    defaults: new { controller = "HangHoa", action = "Detail" });

app.MapControllerRoute(
    name: "product-category",
    pattern: "danh-muc/{slug}-{id:int}",
    defaults: new { controller = "HangHoa", action = "Index" });

// THÊM route search sản phẩm
app.MapControllerRoute(
    name: "product-search",
    pattern: "san-pham/tim-kiem",
    defaults: new { controller = "HangHoa", action = "Search" });

// THÊM route danh sách sản phẩm
app.MapControllerRoute(
    name: "products",
    pattern: "san-pham",
    defaults: new { controller = "HangHoa", action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();