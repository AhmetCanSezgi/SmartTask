using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SmartTask.API.Middleware;
using SmartTask.API.Services;
using SmartTask.Application;
using SmartTask.Application.Common.Interfaces;
using SmartTask.Infrastructure;
using SmartTask.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Serilog — structured logging
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .WriteTo.Console()
       .WriteTo.File("logs/smarttask-.txt", rollingInterval: RollingInterval.Day));

// Katman DI kayıtları
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// API servisleri
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// OpenAPI (Scalar UI)
builder.Services.AddOpenApi();

var app = builder.Build();

// Otomatik migration
//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//    await db.Database.MigrateAsync();
//}

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // Swagger yerine modern Scalar UI
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
