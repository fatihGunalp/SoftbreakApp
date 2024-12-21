using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class YoutubeVideoRepository : Repository<YoutubeVideo>, IYoutubeVideoRepository
    {
        public YoutubeVideoRepository(SoftbreakContext context) : base(context)
        {
        }

        // Videoları ve yorumları listeleyen metot
        public async Task<List<YoutubeVideo>> GetVideosWithCommentsAsync()
        {
            return await _context.YoutubeVideos
                .Include(v => v.Comments) // Videolarla ilişkili yorumları dahil et
                .Select(v => new YoutubeVideo
                {
                    Id = v.Id,
                    VideoId = v.VideoId,
                    Title = v.Title,
                    Description = v.Description,
                    Tags = v.Tags,
                    PublishedAt = v.PublishedAt,
                    Comments = v.Comments.Select(c => new YoutubeComment
                    {
                        Id = c.Id,
                        Author = c.Author,
                        Text = c.Text,
                        LikeCount = c.LikeCount,
                        PublishedAt = c.PublishedAt,
                        VideoId = c.VideoId
                    }).ToList()
                })
                .ToListAsync();
        }
    }
}