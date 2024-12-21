using Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OpenAIEmbeddingController : ControllerBase
    {
        private readonly IOpenAIEmbeddingService _openAIEmbeddingService;

        public OpenAIEmbeddingController(IOpenAIEmbeddingService openAIEmbeddingService)
        {
            _openAIEmbeddingService = openAIEmbeddingService;
        }

        [HttpPost]
        public async Task<IActionResult> GetEmbeddingAsync([FromBody] string inputText)
        {
            var embeddind= await _openAIEmbeddingService.GenerateEmbeddingAsync(inputText);
            return Ok(embeddind);
        }

        [HttpPost("generate-response")]
        public async Task<IActionResult> GenerateResponse([FromQuery] string videoId, [FromBody] string userInput)
        {
            if (string.IsNullOrEmpty(videoId) || string.IsNullOrEmpty(userInput))
            {
                return BadRequest("Video ID ve kullanıcı mesajı boş olamaz.");
            }

            try
            {
                var response = await _openAIEmbeddingService.GenerateChatResponseAsync(userInput, videoId);
                return Ok(new { Response = response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Bir hata oluştu.", Error = ex.Message });
            }
        }

    }
}
