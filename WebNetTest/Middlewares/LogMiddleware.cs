using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.Extensions.Caching.Distributed;
public class LogMiddleware
{
    private readonly  RequestDelegate  _next;
    private  readonly ILogger<LogMiddleware> _logger;

    private const int MaxLogsGuardados = 200;
    public LogMiddleware(RequestDelegate next,ILogger<LogMiddleware> logger)
    {
        _next=  next;
        _logger  =  logger;
    }

    public async Task InvokeAsync(HttpContext context, IDistributedCache cache)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path;
        var ip = context.Connection.RemoteIpAddress?.ToString();
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var  entry = new LogEntry
            {
                Method = method,
                Path = path,
                StatusCode = context.Response.StatusCode,
                ElapsedMs = sw.ElapsedMilliseconds,
                Ip  = ip
            };
            _logger.LogInformation(
                "{Method} {Path} -> {StatusCode} ({Elapsed}ms)",
                method,  path, context.Response.StatusCode,  sw.ElapsedMilliseconds
            );
            _ = SalvarLogNoCacheAsync(cache, entry);
        }
    }
    private async Task SalvarLogNoCacheAsync(IDistributedCache cache, LogEntry entry)
    {
        try
        {
            var chaveIndividual =  $"log:{entry.Id}";
            await  cache.SetStringAsync(
                chaveIndividual,
                JsonSerializer.Serialize(entry),
                new DistributedCacheEntryOptions {AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)}
            );
            var listaJson = await cache.GetStringAsync("logs:recentes");
            var  lista = listaJson is not null?  
                JsonSerializer.Deserialize<List<string>>(listaJson)!: new List<string>();

            lista.Insert(0, chaveIndividual);
            if(lista.Count>MaxLogsGuardados) lista = lista.Take(MaxLogsGuardados).ToList();

            await cache.SetStringAsync("logs:recentes", JsonSerializer.Serialize(lista), 
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)});
        }
        catch(Exception ex)
        {
            _logger.LogWarning(ex, "Falha a o salvar log no cache");
        }
        }
}

public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app) 
        => app.UseMiddleware<LogMiddleware>();
}