using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using TechStoreWeb.Services;
using TechStoreWeb.Data; // Lưu ý: Nếu tên Project của bạn khác 'TechStoreWeb', hãy sửa lại cho khớp

DotEnv.Load(
    Path.Combine(Directory.GetCurrentDirectory(), ".env"),
    Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"));

var builder = WebApplication.CreateBuilder(args);

// 1. Thêm dịch vụ MVC (Model-View-Controller)
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddHostedService<PromotionExpiryWorker>();
builder.Services.Configure<ChatbotOptions>(builder.Configuration.GetSection("Chatbot"));
builder.Services.AddScoped<IChatbotRagService, ChatbotRagService>();
builder.Services.AddScoped<IChatbotService, ChatbotService>();
builder.Services.AddHttpClient<ILlmClient, OpenAiResponsesClient>();

// Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Cấu hình Authentication (Cookie + Facebook OAuth)
var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.Cookie.Name = "TechStore.ExternalAuth";
    options.Cookie.HttpOnly = true;
});

// Chỉ đăng ký đăng nhập mạng xã hội khi đã có khoá thật.
// Nếu đăng ký với chuỗi rỗng, ASP.NET Core sẽ ném lỗi và app không khởi động được.
var facebookAppId = builder.Configuration["Authentication:Facebook:AppId"];
var facebookAppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

if (!string.IsNullOrWhiteSpace(facebookAppId) && !string.IsNullOrWhiteSpace(facebookAppSecret))
{
    authBuilder.AddFacebook(options =>
    {
        options.AppId = facebookAppId;
        options.AppSecret = facebookAppSecret;
        // Xóa scope 'email' mặc định vì Facebook App chưa được duyệt quyền này
        options.Scope.Clear();
        options.Scope.Add("public_profile");
        options.Fields.Add("name");
        options.SaveTokens = true;
        options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
        {
            OnRedirectToAuthorizationEndpoint = context =>
            {
                context.Response.Redirect(context.RedirectUri + "&auth_type=reauthenticate");
                return Task.CompletedTask;
            }
        };
    });
}

if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.SaveTokens = true;
    });
}

// 2. Cấu hình kết nối Database bằng Entity Framework Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Seed database and copy image files
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var env = services.GetRequiredService<IWebHostEnvironment>();

        // Ensure database is created
        context.Database.EnsureCreated();
        ChatbotDatabaseInitializer.EnsureCreated(context);
        PromotionDatabaseInitializer.EnsureCreated(context);

        TechStoreWeb.Data.DbInitializer.Initialize(context, env, builder.Configuration["SeedAdminPassword"]);

        // Áp/gỡ giá khuyến mại ngay lúc khởi động theo ngày hiệu lực.
        services.GetRequiredService<IPromotionService>().SyncAsync().GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

// 3. Cấu hình HTTP request pipeline (Luồng xử lý request)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // HSTS giúp tăng cường bảo mật khi chạy thực tế
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Cho phép đọc các file css, js, hình ảnh trong thư mục wwwroot

app.UseRouting();

// enable session middleware
app.UseSession();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// 4. Cấu hình Route mặc định (Trang chủ sẽ trỏ thẳng vào HomeController và action Index)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
