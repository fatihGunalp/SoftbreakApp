using Core.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.ExternalServices
{
    public class OpenAIEmbeddingService : IOpenAIEmbeddingService
    {
        private readonly SoftbreakContext _context;
        private readonly OpenAIClient _openAIClient;
        private readonly string _prompt;

        public OpenAIEmbeddingService(SoftbreakContext context, IConfiguration configuration)
        {
            var apiKey = Environment.GetEnvironmentVariable("OpenAI_ApiKey");
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentNullException("OpenAI:ApiKey is not configured.");
            }
            _context = context;
            _openAIClient = new OpenAIClient(apiKey);
            _prompt = configuration["PromptSettings:MentatPrompt"];
        }

        public async Task<List<float>> GenerateEmbeddingAsync(string inputText)
        {
            // Girdi metni boş veya null ise hata fırlat
            if (string.IsNullOrWhiteSpace(inputText))
            {
                throw new ArgumentException("Girdi metni boş olamaz.", nameof(inputText));
            }

            // OpenAI Embeddings API'sine istek gönder
            var response = await _openAIClient.EmbeddingsEndpoint.CreateEmbeddingAsync(
                inputText,
                model: "text-embedding-ada-002" // Kullanılacak model
            );

            // Yanıtın geçerli olduğundan emin ol
            if (response != null && response.Data != null && response.Data.Count > 0)
            {
                // IReadOnlyList<double> türünden List<float> türüne dönüşüm
                return response.Data[0].Embedding.Select(value => (float)value).ToList();
            }

            // Geçersiz yanıt durumunda hata fırlat
            throw new Exception("Embedding oluşturulamadı. API'den geçersiz veya boş bir yanıt alındı.");
        }



        public async Task<string> GenerateChatResponseAsync(string userInput, string videoId)
        {
            // 1. Videoyu ve yorumları veritabanından çek
            var video = await _context.YoutubeVideos
                .Include(v => v.Comments)
                .FirstOrDefaultAsync(v => v.VideoId == videoId);

            if (video == null)
            {
                return "Belirtilen video bulunamadı.";
            }

            // 2. Kullanıcı mesajını embedding'e dönüştür
            var userEmbedding = await GenerateEmbeddingAsync(userInput);

            // 3. Videoya ait yorumlardan en alakalı olanları seç (Cosine Similarity)
            var relevantComments = video.Comments
                .Where(c => c.Vector != null)
                .Select(c => new
                {
                    Comment = c.Text,
                    Similarity = CalculateCosineSimilarity(userEmbedding.ToArray(), c.Vector)
                })
                .OrderByDescending(x => x.Similarity)
                .Take(5) // İlk 5 en alakalı yorumu seç
                .Select(x => x.Comment)
                .ToList();

            // 4. Prompt oluştur
            var contextBuilder = new StringBuilder();
            contextBuilder.AppendLine($"Video: {video.Title}");
            contextBuilder.AppendLine($"Açıklama: {video.Description}");
            contextBuilder.AppendLine("En Alakalı Yorumlar:");
            foreach (var comment in relevantComments)
            {
                contextBuilder.AppendLine($"- {comment}");
            }

            

            // ChatGPT için mesaj listesi oluştur
            var messages = new List<Message>
            {
                new Message(Role.System, _prompt),
                new Message(Role.User, userInput),
                new Message(Role.Assistant, contextBuilder.ToString())
            };

            // 5. Chat API isteği gönder
            var chatRequest = new ChatRequest(
                messages: messages,
                model: "gpt-4o"
            );

            var response = await _openAIClient.ChatEndpoint.GetCompletionAsync(chatRequest);

            // İlk cevabı döndür
            return response.Choices[0].Message.Content.ToString();
        }

        public double CalculateCosineSimilarity(float[] vector1, float[] vector2)
        {
            var dotProduct = vector1.Zip(vector2, (v1, v2) => v1 * v2).Sum();
            var magnitude1 = Math.Sqrt(vector1.Sum(v => v * v));
            var magnitude2 = Math.Sqrt(vector2.Sum(v => v * v));
            return dotProduct / (magnitude1 * magnitude2);
        }
    }
}
