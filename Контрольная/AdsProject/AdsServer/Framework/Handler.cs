using System.Net;

namespace MyHttpServer.Framework.Handlers
{
    public abstract class Handler
    {
        public Handler? Successor { get; set; }
        public abstract Task HandleRequestAsync(HttpListenerContext context);
    }
}
