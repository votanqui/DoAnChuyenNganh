using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using DoAnChuyenNganh.data;
using MailKit.Net.Smtp;
using MimeKit;
using System.Threading.Tasks;
using DoAnChuyenNganh.Models;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.Facebook;
using DoAnChuyenNganh.Services;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình DbContext với SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cấu hình xác thực cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.None; // Thêm dòng này
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Thêm dòng này
    })
    .AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        googleOptions.CallbackPath = "/signin-google";
        googleOptions.Events = new OAuthEvents
        {
            OnRemoteFailure = context =>
            {
                // Kiểm tra xem lỗi là do người dùng từ chối quyền truy cập
                if (context.Failure != null && context.Failure.Message.Contains("access_denied"))
                {
                    // Điều hướng người dùng trở lại trang login với thông báo lỗi
                    context.Response.Redirect("/Account/Login?error=access_denied");
                    context.HandleResponse(); // Chặn hệ thống xử lý ngoại lệ
                }
                else
                {
                    // Xử lý lỗi khác nếu có
                    context.Response.Redirect("/Account/Login?error=google_error");
                    context.HandleResponse();
                }
                return Task.CompletedTask;
            }
        };
    })
     .AddFacebook(facebookOptions =>
     {
         facebookOptions.AppId = builder.Configuration["Authentication:Facebook:AppId"];
         facebookOptions.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
         facebookOptions.CallbackPath = "/signin-facebook";

         facebookOptions.Events = new OAuthEvents
         {
             OnRemoteFailure = context =>
             {
                 // Kiểm tra xem lỗi là do người dùng từ chối quyền truy cập
                 if (context.Failure != null && context.Failure.Message.Contains("access_denied"))
                 {
                     // Điều hướng người dùng trở lại trang login với thông báo lỗi
                     context.Response.Redirect("/Account/Login?error=access_denied");
                     context.HandleResponse(); // Chặn hệ thống xử lý ngoại lệ
                 }
                 else
                 {
                     // Xử lý lỗi khác nếu có
                     context.Response.Redirect("/Account/Login?error=facebook_error");
                     context.HandleResponse();
                 }
                 return Task.CompletedTask;
             }
         };
     });
// Thêm dịch vụ MVC
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IVnPayService, VnPayService>();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60); // Thời gian session tồn tại
    options.Cookie.HttpOnly = true; // Chỉ cho phép session qua HTTP, tăng cường bảo mật
    options.Cookie.IsEssential = true; // Đảm bảo cookie session luôn được gửi
});
// Thêm dịch vụ xác thực
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
  
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminOrEmployeeRole", policy =>
      policy.RequireRole("employee")); // Cho phép Admin hoặc Nhân viên

});

var app = builder.Build();

// Cấu hình pipeline xử lý yêu cầu HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseSession();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Kích hoạt xác thực và ủy quyền
app.UseAuthentication();
app.UseAuthorization();

// Định nghĩa các route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();



