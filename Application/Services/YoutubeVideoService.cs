using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Entities;
using Core.Interfaces;

namespace Application.Services
{
    public class YoutubeVideoService
    {
        private readonly IYoutubeVideoRepository _youtubeVideoRepository;

        public YoutubeVideoService(IYoutubeVideoRepository youtubeVideoRepository)
        {
            _youtubeVideoRepository = youtubeVideoRepository;
        }

        public async Task<List<YoutubeVideo>> GetAllVideosAsync()
        {
            return await _youtubeVideoRepository.GetAllAsync();
        }

        public async Task<YoutubeVideo> GetVideoByIdAsync(int id)
        {
            return await _youtubeVideoRepository.GetByIdAsync(id);
        }

        public async Task AddVideoAsync(YoutubeVideo video)
        {
            await _youtubeVideoRepository.AddAsync(video);
        }

        public async Task DeleteVideoAsync(int id)
        {
            var video = await _youtubeVideoRepository.GetByIdAsync(id);
            if (video != null)
            {
                await _youtubeVideoRepository.DeleteAsync(video);
            }
        }
    }
}