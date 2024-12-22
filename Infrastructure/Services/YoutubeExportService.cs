using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class YoutubeExportService
    {
        private readonly SoftbreakContext _context;

        public YoutubeExportService(SoftbreakContext context)
        {
            _context = context;
        }

        public async Task<string> ExportDataToJsonAsync(string filePath)
        {
            // Verileri al
            var videos = await _context.YoutubeVideos
                .Include(v => v.Comments)
                .Select(v => new
                {
                    v.Id,
                    v.VideoId,
                    v.Title,
                    v.Description,
                    v.PublishedAt,
                    v.Tags,
                    Comments = v.Comments.Select(c => new
                    {
                        c.Id,
                        c.VideoId,
                        c.Author,
                        c.PublishedAt,
                        c.Text
                    })
                })
                .ToListAsync();

            // JSON'a dönüştür
            var jsonData = JsonSerializer.Serialize(videos, new JsonSerializerOptions
            {
                WriteIndented = true, // JSON'u okunabilir formatta yaz
                ReferenceHandler = ReferenceHandler.IgnoreCycles // Döngüsel referansları yoksay
            });

            // Dosyaya yaz
            await File.WriteAllTextAsync(filePath, jsonData);
            return filePath;
        }
    }
}
