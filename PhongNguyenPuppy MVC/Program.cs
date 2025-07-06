using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PhongNguyenPuppy_MVC.Data;

var builder = WebApplication.CreateBuilder(args);

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

// Thêm dịch vụ Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/KhachHang/DangNhap"; // Đường dẫn đến trang đăng nhập
        options.AccessDeniedPath = "/Login/AccessDenied"; // Đường dẫn đến trang từ chối truy cập
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Thời gian hết hạn của cookie
        options.SlidingExpiration = true; // Kích hoạt gia hạn thời gian hết hạn khi người dùng hoạt động
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Đảm bảo bạn thêm dòng này để phục vụ các tệp tĩnh từ wwwroot
app.UseRouting();

app.UseSession(); // Thêm dòng này để sử dụng Session

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
