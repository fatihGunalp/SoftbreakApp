using Application.Services;
using Core.Interfaces;
using Infrastructure.Context;
using Infrastructure.ExternalServices;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// PostgreSQL veritabanı bağlantısı ve Context'in eklenmesi
builder.Services.AddDbContext<SoftbreakContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQLConnection"), b => b.MigrationsAssembly("WebApi")));

// Dependency Injection
builder.Services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
builder.Services.AddScoped<IYoutubeDataService, YoutubeDataService>();
builder.Services.AddScoped<IYoutubeVideoRepository, YoutubeVideoRepository>();
builder.Services.AddScoped<YoutubeVideoService>();

// CORS Ayarları (Özelleştirin)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Veritabanı başlatma işlemi
using (var scope = app.Services.CreateScope())
{
    var databaseInitializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
    await databaseInitializer.InitializeAsync();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll"); // CORS politikası uygulanıyor
app.UseAuthorization();

app.MapControllers();

app.Run();