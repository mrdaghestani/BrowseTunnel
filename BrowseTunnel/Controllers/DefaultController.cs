using BrowseTunnel.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace BrowseTunnel.Controllers
{
    [ApiController]
    [Route("{**rawUrl}")]
    public class DefaultController : ControllerBase
    {
        private static HttpClient _client = new HttpClient();
        private readonly ILogger<DefaultController> _logger;

        public DefaultController(ILogger<DefaultController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [HttpPost]
        [HttpHead]
        [HttpOptions]
        [HttpPut]
        public async Task<IActionResult> Execute(CancellationToken cancellationToken)
        {
            var rawUrl = Request.Path.Value?.TrimStart('/');

            if (string.IsNullOrWhiteSpace(rawUrl))
            {
                return Ok("Welcome!");
            }
            var isUrlValid = Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri);
            if (!isUrlValid)
            {
                return Ok("Invalid!");
            }

            var method = new HttpMethod(Request.Method);

            using var request = new HttpRequestMessage(method, uri);

            foreach (var header in Request.Headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }



            var responseMessage = await _client.SendAsync(request, cancellationToken);

            return new HttpResponseMessageResult(responseMessage);
        }
    }
}