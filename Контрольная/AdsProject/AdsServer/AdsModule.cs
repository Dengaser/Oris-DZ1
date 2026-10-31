using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace AdsModule
{
    
    public class Ad
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public string Status { get; set; } = AdService.Active;
    }

    public enum ServiceResult { Ok, BadRequest, NotFound, Conflict }

    
    public class AdService
    {
        public const string Active = "active";
        public const string Archived = "archived";

        public static readonly string[] Categories = { "electronics", "furniture", "clothing" };
        public static readonly string[] Statuses = { Active, Archived };

        private readonly List<Ad> _ads = new();
        private readonly object _lock = new();
        private int _nextId = 1;

        public static bool IsValidTitle(string? title)
        {
            if (title == null) return false;
            var len = title.Trim().Length;
            return len >= 5 && len <= 100;
        }

        public static bool IsValidCategory(string? category) =>
            category != null && Categories.Contains(category);

        public static bool IsValidStatus(string? status) =>
            status != null && Statuses.Contains(status);

        
        private bool IsDuplicate(string title, string category, int? exceptId = null) =>
            _ads.Any(a => a.Id != exceptId
                          && a.Category == category
                          && string.Equals(a.Title, title, StringComparison.OrdinalIgnoreCase));

        public (ServiceResult Result, Ad? Ad) Create(string? title, string? category)
        {
            if (!IsValidTitle(title) || !IsValidCategory(category))
                return (ServiceResult.BadRequest, null);

            title = title!.Trim();
            lock (_lock)
            {
                if (IsDuplicate(title, category!))
                    return (ServiceResult.Conflict, null);

                var ad = new Ad { Id = _nextId++, Title = title, Category = category!, Status = Active };
                _ads.Add(ad);
                return (ServiceResult.Ok, ad);
            }
        }

        public (ServiceResult Result, List<Ad> Items) GetAll(string? category)
        {
            lock (_lock)
            {
                if (string.IsNullOrEmpty(category))
                    return (ServiceResult.Ok, _ads.ToList());
                if (!IsValidCategory(category))
                    return (ServiceResult.BadRequest, new List<Ad>());
                return (ServiceResult.Ok, _ads.Where(a => a.Category == category).ToList());
            }
        }

        public Ad? GetById(int id)
        {
            lock (_lock) return _ads.FirstOrDefault(a => a.Id == id);
        }

        public ServiceResult Update(int id, string? title, string? category, string? status)
        {
            if (!IsValidTitle(title) || !IsValidCategory(category) || !IsValidStatus(status))
                return ServiceResult.BadRequest;

            title = title!.Trim();
            lock (_lock)
            {
                var ad = _ads.FirstOrDefault(a => a.Id == id);
                if (ad == null) return ServiceResult.NotFound;
                if (IsDuplicate(title, category!, id)) return ServiceResult.Conflict;

                ad.Title = title;
                ad.Category = category!;
                ad.Status = status!;
                return ServiceResult.Ok;
            }
        }
    }

    
    public class AdsHandler
    {
        private readonly AdService _service;
        private readonly string _basePath;

        public AdsHandler(AdService service, string basePath = "")
        {
            _service = service;
            _basePath = (basePath ?? "").TrimEnd('/');
        }

        
        public async Task<bool> TryHandleAsync(HttpListenerContext ctx)
        {
            var req = ctx.Request;
            var path = req.Url!.AbsolutePath;
            if (_basePath.Length > 0 && path.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
                path = path.Substring(_basePath.Length);
            path = path.TrimEnd('/').ToLowerInvariant();
            var method = req.HttpMethod.ToUpperInvariant();

            switch (path, method)
            {
                case ("/ads", "GET"): await ListAsync(ctx); return true;
                case ("/ads/details", "GET"): await DetailsAsync(ctx); return true;
                case ("/ads/create", "POST"): await CreateAsync(ctx); return true;
                case ("/ads/update", "PUT"): await UpdateAsync(ctx); return true;
            }
            return false;
        }

        
        private async Task ListAsync(HttpListenerContext ctx)
        {
            var category = ctx.Request.QueryString["category"];
            var (result, items) = _service.GetAll(category);
            if (result == ServiceResult.BadRequest)
            {
                await WriteTextAsync(ctx, 400, "Unknown category");
                return;
            }

            var sb = new StringBuilder();
            sb.Append("<h1>Объявления</h1>");

            
            sb.Append($"<form method=\"get\" action=\"{_basePath}/ads\"><select name=\"category\"><option value=\"\">Все</option>");
            foreach (var c in AdService.Categories)
            {
                var sel = c == category ? " selected" : "";
                sb.Append($"<option value=\"{Enc(c)}\"{sel}>{Enc(c)}</option>");
            }
            sb.Append("</select> <button type=\"submit\">Фильтровать</button></form>");

            
            sb.Append("<ul>");
            foreach (var ad in items)
                sb.Append($"<li><a href=\"{_basePath}/ads/details?id={ad.Id}\">{Enc(ad.Title)}</a> — {Enc(ad.Category)} [{Enc(ad.Status)}]</li>");
            sb.Append("</ul>");

            
            sb.Append("<h2>Новое объявление</h2>");
            sb.Append($"<form method=\"post\" action=\"{_basePath}/ads/create\">");
            sb.Append("<input name=\"title\" placeholder=\"Название (5–100 символов)\" required> ");
            sb.Append("<select name=\"category\">");
            foreach (var c in AdService.Categories)
                sb.Append($"<option value=\"{Enc(c)}\">{Enc(c)}</option>");
            sb.Append("</select> <button type=\"submit\">Создать</button></form>");

            await WriteHtmlAsync(ctx, 200, "Объявления", sb.ToString());
        }

        
        private async Task DetailsAsync(HttpListenerContext ctx)
        {
            if (!int.TryParse(ctx.Request.QueryString["id"], out var id) || id <= 0)
            {
                await WriteTextAsync(ctx, 400, "Invalid id");
                return;
            }

            var ad = _service.GetById(id);
            if (ad == null)
            {
                await WriteTextAsync(ctx, 404, "Ad not found");
                return;
            }

            var body = $"<h1>{Enc(ad.Title)}</h1>" +
                       $"<p>Id: {ad.Id}</p>" +
                       $"<p>Категория: {Enc(ad.Category)}</p>" +
                       $"<p>Статус: {Enc(ad.Status)}</p>" +
                       $"<p><a href=\"{_basePath}/ads\">К списку</a></p>";
            await WriteHtmlAsync(ctx, 200, "Объявление", body);
        }

        
        private async Task CreateAsync(HttpListenerContext ctx)
        {
            var form = await ReadFormAsync(ctx.Request);
            var (result, ad) = _service.Create(form["title"], form["category"]);

            switch (result)
            {
                case ServiceResult.Ok:
                    ctx.Response.StatusCode = 303;
                    ctx.Response.RedirectLocation = $"{_basePath}/ads/details?id={ad!.Id}";
                    ctx.Response.Close();
                    break;
                case ServiceResult.Conflict:
                    await WriteTextAsync(ctx, 409, "Ad with the same title and category already exists");
                    break;
                default:
                    await WriteTextAsync(ctx, 400, "Invalid title or category");
                    break;
            }
        }

        
        private async Task UpdateAsync(HttpListenerContext ctx)
        {
            if (!int.TryParse(ctx.Request.QueryString["id"], out var id) || id <= 0)
            {
                await WriteTextAsync(ctx, 400, "Invalid id");
                return;
            }

            var form = await ReadFormAsync(ctx.Request);
            var result = _service.Update(id, form["title"], form["category"], form["status"]);

            switch (result)
            {
                case ServiceResult.Ok:
                    ctx.Response.StatusCode = 204;
                    ctx.Response.Close();
                    break;
                case ServiceResult.NotFound:
                    await WriteTextAsync(ctx, 404, "Ad not found");
                    break;
                case ServiceResult.Conflict:
                    await WriteTextAsync(ctx, 409, "Ad with the same title and category already exists");
                    break;
                default:
                    await WriteTextAsync(ctx, 400, "Invalid title, category or status");
                    break;
            }
        }

        
        private static string Enc(string s) => WebUtility.HtmlEncode(s);

        private static async Task<System.Collections.Specialized.NameValueCollection> ReadFormAsync(HttpListenerRequest req)
        {
            using var reader = new StreamReader(req.InputStream, req.ContentEncoding ?? Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            return HttpUtility.ParseQueryString(body);
        }

        private static async Task WriteHtmlAsync(HttpListenerContext ctx, int status, string title, string body)
        {
            var html = $"<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>{Enc(title)}</title></head><body>{body}</body></html>";
            await WriteAsync(ctx, status, "text/html; charset=utf-8", html);
        }

        private static Task WriteTextAsync(HttpListenerContext ctx, int status, string text) =>
            WriteHtmlAsync(ctx, status, $"Error {status}", $"<h1>{status}</h1><p>{Enc(text)}</p>");

        private static async Task WriteAsync(HttpListenerContext ctx, int status, string contentType, string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            ctx.Response.StatusCode = status;
            ctx.Response.ContentType = contentType;
            ctx.Response.ContentLength64 = bytes.Length;
            await ctx.Response.OutputStream.WriteAsync(bytes);
            ctx.Response.Close();
        }
    }
}
