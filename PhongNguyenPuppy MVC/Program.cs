using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PhongNguyenPuppy_MVC.Data;
using PhongNguyenPuppy_MVC.Helpers;
using PhongNguyenPuppy_MVC.Services;

var builder = WebApplication.CreateBuilder(args);

// Đọc thêm từ appsettings.Secret.json
builder.Configuration.AddJsonFile("appsettings.Secret.json", optional: true, reloadOnChange: true);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<PhongNguyenPuppyContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("PhongNguyenPuppy"));
});

builder.Services.AddDistributedMemoryCache();

// Thêm dịch vụ Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromSeconds(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Thêm dịch vụ Email
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<MyEmailHelper>();


// Thêm dịch vụ Cookie
// Thêm dịch vụ Cookie cho 2 scheme riêng biệt
builder.Services.AddAuthentication()
.AddCookie("CustomerScheme", options =>
{
    options.LoginPath = "/KhachHang/DangNhap";
    options.AccessDeniedPath = "/KhachHang/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
})
.AddCookie("AdminScheme", options =>
{
    options.LoginPath = "/Admin/Admin/Login";
    options.AccessDeniedPath = "/Admin/Admin/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});




// Thêm dịch vụ PayPal dạng Singleton() - chỉ có một instances duy nhất trong toàn bộ ứng dụng
builder.Services.AddSingleton(x => new PaypalClient(
    builder.Configuration["PaypalOptions:AppId"] ?? throw new ArgumentNullException("ClientId is not configured"),
    builder.Configuration["PaypalOptions:AppSecret"] ?? throw new ArgumentNullException("ClientSecret is not configured"),
    builder.Configuration["PaypalOptions:Mode"] ?? throw new ArgumentNullException("Mode is not configured")
));

// Thêm dịch vụ VnPay
builder.Services.AddSingleton<IVnPayService, VnPayService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


app.UseHttpsRedirection();
app.UseStaticFiles(); //  thêm dòng này để phục vụ các tệp tĩnh từ wwwroot
app.UseRouting();

app.UseSession(); // Thêm dòng này để sử dụng Session

app.UseAuthentication(); // Thêm dòng này để sử dụng Authentication
app.UseAuthorization();


app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
