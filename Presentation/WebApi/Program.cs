using Application.Services;
using Core.Interfaces;
using Infrastructure.BackgroundServices;
using Infrastructure.Context;
using Infrastructure.ExternalServices;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("Connection string not found in environment variables.");
}

// PostgreSQL veritabanı bağlantısı ve Context'in eklenmesi
builder.Services.AddDbContext<SoftbreakContext>(options =>
    options.UseNpgsql(connectionString, b => b.MigrationsAssembly("WebApi")));

//OpenAI_ApiKey
var apiKey = Environment.GetEnvironmentVariable("OpenAI_ApiKey");

// Dependency Injection
builder.Services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
builder.Services.AddScoped<IYoutubeDataService, YoutubeDataService>();
builder.Services.AddScoped<IYoutubeVideoRepository, YoutubeVideoRepository>();
builder.Services.AddScoped<IOpenAIEmbeddingService, OpenAIEmbeddingService>();
builder.Services.AddScoped<IYoutubeVideoRepository,YoutubeVideoRepository>();
builder.Services.AddScoped<YoutubeVideoService>();
builder.Services.AddScoped<DatabaseEmbedding>();
builder.Services.AddScoped<YoutubeExportService>();

// YouTube veri güncellemelerini zamanlayarak kontrol eden Background Service ekleniyor.
builder.Services.AddHostedService<YoutubeSyncBackgroundService>();

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

// Veritabanı Embedding güncelleme servisini çalıştır
using (var scope = app.Services.CreateScope())
{
    var updaterService = scope.ServiceProvider.GetRequiredService<DatabaseEmbedding>();
    await updaterService.UpdateEmbeddingsAsync();
}

// JSON dışa aktarma servisini çalıştır
using (var scope = app.Services.CreateScope())
{
    var exportService = scope.ServiceProvider.GetRequiredService<YoutubeExportService>();
    var filePath = await exportService.ExportDataToJsonAsync("YoutubeData.json");
    Console.WriteLine($"JSON verisi oluşturuldu: {filePath}");
}



app.UseHttpsRedirection();
app.UseCors("AllowAll"); // CORS politikası uygulanıyor
app.UseAuthorization();

app.MapControllers();

app.Run();