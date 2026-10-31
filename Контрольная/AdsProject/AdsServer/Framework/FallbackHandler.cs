using System.Net;
using System.Text;

namespace MyHttpServer.Framework.Handlers
{
    
    public class FallbackHandler : Handler
    {
        private readonly string _basePath;

        public FallbackHandler(string basePath = "")
        {
            _basePath = basePath.TrimEnd('/');
        }

        public override async Task HandleRequestAsync(HttpListenerContext context)
        {
            var path = context.Request.Url!.AbsolutePath;
            if (_basePath.Length > 0 && path.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
                path = path.Substring(_basePath.Length);
            path = path.TrimEnd('/');

            var response = context.Response;

            if (path.Length == 0)
            {
                response.StatusCode = 302;
                response.RedirectLocation = _basePath + "/ads";
                response.Close();
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(
                "<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>404</title></head>" +
                "<body><h1>404</h1><p>Страница не найдена.</p></body></html>");
            response.StatusCode = 404;
            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength64 = bytes.Length;
            await response.OutputStream.WriteAsync(bytes);
            response.Close();
        }
    }
}
