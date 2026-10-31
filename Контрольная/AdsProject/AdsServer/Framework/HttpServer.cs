using System.Net;
using MyHttpServer.Framework.Handlers;

namespace MyHttpServer.Framework
{
    public class HttpServer
    {
        private readonly HttpListener _listener = new();
        private bool _isRunning;
        private Handler? _handlerChain;

        public void Start()
        {
            try
            {
                if (!File.Exists("settings.json"))
                {
                    Console.WriteLine("Ошибка: файл settings.json не найден! Текущая папка: " + Directory.GetCurrentDirectory());
                    return;
                }

                var settings = Settings.Load();
                var basePath = "/" + settings.Server.Path.Trim('/');
                if (basePath == "/") basePath = "";

                
                var ads = new AdsChainHandler(basePath);
                var fallback = new FallbackHandler(basePath);
                ads.Successor = fallback;
                _handlerChain = ads;

                var host = settings.Server.Host;
                var port = settings.Server.Port;

                _listener.Prefixes.Clear();
                _listener.Prefixes.Add($"http://{host}:{port}{basePath}/");
                
                if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                    _listener.Prefixes.Add($"http://127.0.0.1:{port}{basePath}/");

                _listener.Start();
                _isRunning = true;

                Console.WriteLine($"Сервер запущен: http://localhost:{port}{basePath}/ads");

                _ = ListenAsync();
            }
            catch (HttpListenerException ex)
            {
                Console.WriteLine($"Ошибка доступа к сети (порт занят или нужны права администратора): {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критическая ошибка при запуске: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;
            _isRunning = false;
            _listener.Stop();
            _listener.Close();
            Console.WriteLine("Сервер завершил работу");
        }

        private async Task ListenAsync()
        {
            while (_isRunning)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = ProcessRequestAsync(context);
                }
                catch (HttpListenerException) { break; }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка прослушивания: {ex.Message}");
                }
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            try
            {
                await _handlerChain!.HandleRequestAsync(context);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки запроса: {ex.Message}");
                try
                {
                    context.Response.StatusCode = 500;
                    context.Response.Close();
                }
                catch { /* ответ уже закрыт */ }
            }
        }
    }
}
