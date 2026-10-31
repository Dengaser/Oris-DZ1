using System.Text.Json;

namespace MyHttpServer.Framework
{
    public class ServerSettings
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 9999;
        public string Path { get; set; } = "";
    }

    public class Settings
    {
        public ServerSettings Server { get; set; } = new();

        public static Settings Load(string file = "settings.json")
        {
            var json = File.ReadAllText(file);
            return JsonSerializer.Deserialize<Settings>(json,
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? new Settings();
        }
    }
}
