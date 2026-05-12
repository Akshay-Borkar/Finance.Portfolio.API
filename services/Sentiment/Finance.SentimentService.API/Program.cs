using Finance.SentimentService.API.Middleware;
using Finance.SentimentService.Infrastructure;
using Finance.SharedKernel.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSentimentInfrastructure();
builder.Services.AddSharedJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
        policy.WithOrigins("http://localhost:4200", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

app.MapOpenApi();
app.UseCors("CorsPolicy");
app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { service = "sentiment", status = "healthy" }));

app.Run();
