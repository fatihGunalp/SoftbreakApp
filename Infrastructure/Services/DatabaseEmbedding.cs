using Core.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class DatabaseEmbedding
    {
        private readonly SoftbreakContext _context;
        private readonly IOpenAIEmbeddingService _openAIEmbeddingService;

        public DatabaseEmbedding(SoftbreakContext context, IOpenAIEmbeddingService openAIEmbeddingService)
        {
            _context = context;
            _openAIEmbeddingService = openAIEmbeddingService;
        }

        public async Task UpdateEmbeddingsAsync()
        {
            // Videoları ve yorumları sorgula
            var videos = (await _context.YoutubeVideos
                .Where(v => v.Vector == null) // Sadece null olanları sorgula (SQL destekli)
                .ToListAsync()) // ToListAsync burada çağrılır
                .Where(v => v.Vector == null || v.Vector.Length == 0) // Client-side: Uzunluğu kontrol et
                .ToList();

            var comments = (await _context.YoutubeComments
                .Where(c => c.Vector == null) // Sadece null olanları sorgula (SQL destekli)
                .ToListAsync()) // ToListAsync burada çağrılır
                .Where(c => c.Vector == null || c.Vector.Length == 0) // Client-side: Uzunluğu kontrol et
                .ToList();

            // Embedding işlemleri
            if (!videos.Any() && !comments.Any())
            {
                return;
            }

            Console.WriteLine("Embedding güncelleme işlemi başladı...");

            foreach (var video in videos)
            {
                try
                {
                    video.Vector = (await _openAIEmbeddingService.GenerateEmbeddingAsync(video.Title + " " + video.Description)).ToArray();
                    Console.WriteLine($"Video Embedding güncellendi: {video.Title}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Video Embedding hatası ({video.Title}): {ex.Message}");
                }
            }

            foreach (var comment in comments)
            {
                try
                {
                    comment.Vector = (await _openAIEmbeddingService.GenerateEmbeddingAsync(comment.Text)).ToArray();
                    Console.WriteLine($"Yorum Embedding güncellendi: {comment.Text.Substring(0, Math.Min(50, comment.Text.Length))}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Yorum Embedding hatası: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();
            Console.WriteLine("Embedding güncelleme işlemi tamamlandı.");
        }



    }
}
