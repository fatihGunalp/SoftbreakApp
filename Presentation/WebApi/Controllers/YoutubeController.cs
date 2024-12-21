using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Services;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class YoutubeController : ControllerBase
    {
        private readonly IYoutubeVideoRepository _youtubeVideoRepository;

        public YoutubeController(IYoutubeVideoRepository youtubeVideoRepository)
        {
            _youtubeVideoRepository = youtubeVideoRepository;
        }

        [HttpGet]
        public async Task<ActionResult<List<YoutubeVideo>>> GetVideos()
        {
            var videos = await _youtubeVideoRepository.GetVideosWithCommentsAsync();
            return Ok(videos);
        }

       
    }
}