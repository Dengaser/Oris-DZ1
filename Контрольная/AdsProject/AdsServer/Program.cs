using System.Text;
using MyHttpServer.Framework;

Console.OutputEncoding = Encoding.UTF8;

var server = new HttpServer();
server.Start();

Console.WriteLine("Введите 'exit' для остановки сервера...");

while (true)
{
    var command = Console.ReadLine();
    if (command?.Trim().ToLower() == "exit")
    {
        server.Stop();
        break;
    }
}
