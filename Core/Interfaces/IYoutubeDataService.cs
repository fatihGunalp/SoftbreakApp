using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Entities;

namespace Core.Interfaces
{
   public interface IYoutubeDataService
    {
        Task<List<YoutubeVideo>> GetAllChannelVideos();
        Task<List<YoutubeComment>> GetVideoComments(string videoId);
        Task<List<YoutubeVideo>> GetNewChannelVideos(DateTime? lastVideoDate = null);
    }
}