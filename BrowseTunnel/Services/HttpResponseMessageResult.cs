using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace BrowseTunnel.Services
{
    public class HttpResponseMessageResult : IActionResult
    {
        private readonly HttpResponseMessage _responseMessage;

        public HttpResponseMessageResult(HttpResponseMessage responseMessage)
        {
            _responseMessage = responseMessage; // could add throw if null
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;


            if (_responseMessage == null)
            {
                var message = "Response message cannot be null";

                throw new InvalidOperationException(message);
            }

            using (_responseMessage)
            {
                response.StatusCode = (int)_responseMessage.StatusCode;

                var responseFeature = context.HttpContext.Features.Get<IHttpResponseFeature>();
                if (responseFeature != null)
                {
                    responseFeature.ReasonPhrase = _responseMessage.ReasonPhrase;
                }

                var responseHeaders = _responseMessage.Headers;

                // Ignore the Transfer-Encoding header if it is just "chunked".
                // We let the host decide about whether the response should be chunked or not.
                if (responseHeaders.TransferEncodingChunked == true &&
                    responseHeaders.TransferEncoding.Count == 1)
                {
                    responseHeaders.TransferEncoding.Clear();
                }

                foreach (var header in responseHeaders)
                {
                    response.Headers.Append(header.Key, header.Value.ToArray());
                }

                if (_responseMessage.Content != null)
                {
                    var contentHeaders = _responseMessage.Content.Headers;

                    // Copy the response content headers only after ensuring they are complete.
                    // We ask for Content-Length first because HttpContent lazily computes this
                    // and only afterwards writes the value into the content headers.
                    //var unused = contentHeaders.ContentLength;

                    foreach (var header in contentHeaders)
                    {
                        response.Headers.Append(header.Key, header.Value.ToArray());
                    }

                    //await _responseMessage.Content.CopyToAsync(response.Body);

                    var responseMessageStream = await _responseMessage.Content.ReadAsStreamAsync();
                    using var reader = new StreamReader(responseMessageStream);
                    using var writer = new StreamWriter(response.Body);

                    var startingUrl = $"{context.HttpContext.Request.Scheme}://{context.HttpContext.Request.Host}/";

                    var replacer = new[] { "http://", "https://" };

                    var buffer = new char[4096];
                    while (true)
                    {
                        var actualLentgh = reader.Read(buffer, 0, buffer.Length);

                        var value = string.Concat(buffer.Take(actualLentgh));

                        foreach (var item in replacer)
                        {
                            if (value.Contains(item, StringComparison.OrdinalIgnoreCase))
                            {
                                value = value.Replace(item, startingUrl + item);
                            }
                        }

                        writer.Write(value);

                        if (actualLentgh < buffer.Length)
                            break;
                    }
                }
            }
        }
    }
}
