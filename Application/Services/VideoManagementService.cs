using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Entities;
using Core.Interfaces;

namespace Application.Services
{
    public class VideoManagementService
    {
        private readonly IYoutubeDataService _youtubeDataService;

        public VideoManagementService(IYoutubeDataService youtubeDataService)
        {
            _youtubeDataService = youtubeDataService;
        }

        public async Task<List<YoutubeVideo>> SyncAllVideos()
        {
            return await _youtubeDataService.GetAllChannelVideos();
        }
    }
}