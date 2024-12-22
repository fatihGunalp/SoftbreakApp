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
builder.Services.AddSingleton<ILoggingService, LoggingService>();
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
    var dbLogger = scope.ServiceProvider.GetRequiredService<ILoggingService>(); // Benzersiz ad kullanılıyor
    try
    {
        var databaseInitializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
        dbLogger.LogInformation("Veritabanı başlatma işlemi başlatılıyor.");
        await databaseInitializer.InitializeAsync();
        dbLogger.LogInformation("Veritabanı başlatma işlemi başarıyla tamamlandı.");
    }
    catch (Exception ex)
    {
        dbLogger.LogError("Veritabanı başlatma işleminde hata oluştu.", ex);
        throw;
    }
}



// Veritabanı Embedding güncelleme servisini çalıştır
using (var scope = app.Services.CreateScope())
{
    var embeddingLogger = scope.ServiceProvider.GetRequiredService<ILoggingService>(); // Benzersiz ad kullanılıyor
    try
    {
        var updaterService = scope.ServiceProvider.GetRequiredService<DatabaseEmbedding>();
        embeddingLogger.LogInformation("Embedding güncelleme işlemi başlatılıyor.");
        await updaterService.UpdateEmbeddingsAsync();
        embeddingLogger.LogInformation("Embedding güncelleme işlemi başarıyla tamamlandı.");
    }
    catch (Exception ex)
    {
        embeddingLogger.LogError("Embedding güncelleme işleminde hata oluştu.", ex);
        throw;
    }
}



// JSON dışa aktarma servisini çalıştır
using (var scope = app.Services.CreateScope())
{
    var exportLogger = scope.ServiceProvider.GetRequiredService<ILoggingService>(); // Benzersiz ad kullanılıyor
    try
    {
        var exportService = scope.ServiceProvider.GetRequiredService<YoutubeExportService>();
        exportLogger.LogInformation("JSON dışa aktarma işlemi başlatılıyor.");
        var filePath = await exportService.ExportDataToJsonAsync("YoutubeData.json");
        exportLogger.LogInformation($"JSON verisi başarıyla dışa aktarıldı: {filePath}");
    }
    catch (Exception ex)
    {
        exportLogger.LogError("JSON dışa aktarma işleminde hata oluştu.", ex);
        throw;
    }
}





app.UseHttpsRedirection();
var logger = app.Services.GetRequiredService<ILoggingService>();
logger.LogInformation("HTTPS yönlendirme etkinleştirildi.");

app.UseCors("AllowAll");
logger.LogInformation("CORS politikası uygulandı.");

app.UseAuthorization();

app.MapControllers();

app.Run();