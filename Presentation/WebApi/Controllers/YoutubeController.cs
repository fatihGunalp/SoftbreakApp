using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Services;
using Core.Entities;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class YoutubeController : ControllerBase
    {
        private readonly YoutubeVideoService _youtubeVideoService;

        public YoutubeController(YoutubeVideoService youtubeVideoService)
        {
            _youtubeVideoService = youtubeVideoService;
        }

        [HttpGet]
        public async Task<ActionResult<List<YoutubeVideo>>> GetVideos()
        {
            var videos = await _youtubeVideoService.GetAllVideosAsync();
            return Ok(videos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<YoutubeVideo>> GetVideoById(int id)
        {
            var video = await _youtubeVideoService.GetVideoByIdAsync(id);
            if (video == null) return NotFound();
            return Ok(video);
        }

        [HttpPost]
        public async Task<IActionResult> AddVideo([FromBody] YoutubeVideo video)
        {
            await _youtubeVideoService.AddVideoAsync(video);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVideo(int id)
        {
            await _youtubeVideoService.DeleteVideoAsync(id);
            return NoContent();
        }
    }
}