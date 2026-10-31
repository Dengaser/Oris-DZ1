using System.Net;
using System.Threading.Tasks;
using AdsModule;

namespace MyHttpServer.Framework.Handlers
{
    public class AdsChainHandler : Handler
    {
        private readonly AdsHandler _ads;

        public AdsChainHandler(string basePath = "")
        {
            _ads = new AdsHandler(new AdService(), basePath);
        }

        public override async Task HandleRequestAsync(HttpListenerContext context)
        {
            
            if (await _ads.TryHandleAsync(context))
                return;

            
            if (Successor != null)
            {
                await Successor.HandleRequestAsync(context);
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.Close();
            }
        }
    }
}
