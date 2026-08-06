using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Caching.Distributed;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("PoliticaPadrao",  policy =>
    {
        policy.WithOrigins("https://meufrontend.com",  "http://lovalhost:3000")
        .AllowAnyMethod().AllowAnyHeader().AllowCredentials();
    });
});
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("ip", context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString();

        return RateLimitPartition.GetSlidingWindowLimiter(ip, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow=6
            
        });
        
    });
    options.AddTokenBucketLimiter("token", options =>
    {
        options.TokenLimit = 100;
        options.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
        options.TokensPerPeriod = 20;
        options.QueueLimit = 0;
    });
    options.AddConcurrencyLimiter("concurrent", options =>
    {
        options.PermitLimit=20;
        options.QueueLimit=10;
    });
});


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer= true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        
        ValidIssuer = "StockSystem",
        ValidAudience = "StockSystem",
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("SecretTestKeyCool")
        )
    };
});
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    
});
builder.Services.AddControllers();
builder.Services.AddAuthorization();
var app = builder.Build();
app.UseExceptionHandler("/error");
app.UseRequestLogging();
app.UseCors("PoliticaPadrao");
app.UseRateLimiter();
app.MapControllers().RequireRateLimiting("ip");
app.UseAuthentication();
app.UseAuthorization();
app.Run();
