using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Services
{
   public class DatabaseInitializer : IDatabaseInitializer
    {
        private readonly SoftbreakContext _context;
        private readonly IYoutubeDataService _youtubeDataService;

        public DatabaseInitializer(SoftbreakContext context, IYoutubeDataService youtubeDataService)
        {
            _context = context;
            _youtubeDataService = youtubeDataService;
        }

        public async Task InitializeAsync()
        {
            // Veritabanı daha önce oluşturulmuş mu kontrol et
            var databaseExists = await _context.Database.GetService<IRelationalDatabaseCreator>()
                                                         .ExistsAsync();

            if (!databaseExists)
            {
                // Veritabanını oluştur
                await _context.Database.MigrateAsync();
                Console.WriteLine("Veritabanı oluşturuldu ve migrasyonlar uygulandı.");
            }

            // Veritabanında mevcut veri kontrolü
            var hasVideos = await _context.YoutubeVideos.AnyAsync();
            var hasComments = await _context.YoutubeComments.AnyAsync();

            if (!hasVideos || !hasComments)
            {
                Console.WriteLine("Veritabanı boş veya eksik, YouTube'dan veriler çekiliyor...");

                var videos = await _youtubeDataService.GetAllChannelVideos();

                foreach (var video in videos)
                {
                    // Video ve bağlı yorumları veritabanına ekle
                    if (video.Comments != null)
                    {
                        foreach (var comment in video.Comments)
                        {
                            comment.VideoId = video.Id; // Video ile ilişkilendir
                        }
                    }

                    _context.YoutubeVideos.Add(video);
                }

                await _context.SaveChangesAsync();
                Console.WriteLine("Videolar ve yorumlar başarıyla veritabanına eklendi.");
            }
        }
    }
}