using CRAnalyzer.Core.Interfaces;
using CRAnalyzer.Infrastructure.Data;
using CRAnalyzer.Infrastructure.Repositories;
using CRAnalyzer.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")
    , b => b.MigrationsAssembly("CRAnalyzer.Web")));

// Repositories
builder.Services.AddScoped<IAnalysisRepository, AnalysisRepository>();

// Services
builder.Services.AddScoped<IPromptGeneratorService, PromptGeneratorService>();
builder.Services.AddScoped<IAIService, AIService>();
builder.Services.AddScoped<IDocumentParserService, DocumentParserService>();
builder.Services.AddScoped<IRepositoryScannerService, RepositoryScannerService>();

// Session for theme preference
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Increase max request body size for large uploads
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50 MB
});

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

// Ensure uploads directory exists
var uploadPath = Path.Combine(app.Environment.WebRootPath, builder.Configuration["FileUpload:UploadPath"] ?? "uploads");
Directory.CreateDirectory(uploadPath);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
