using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using HaveTickets.Infrastructure;
using HaveTickets.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Configurar Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS para acceso desde GitHub Pages
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Rate Limiting para seguridad (anti-DDoS)
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter(policyName: "fixed", config =>
    {
        config.PermitLimit = 100;
        config.Window = TimeSpan.FromMinutes(1);
        config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        config.QueueLimit = 2;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddInfrastructure("Data Source=havetickets.db");

var app = builder.Build();

// Inicializar la base de datos con datos de prueba
using (var scope = app.Services.CreateScope())
{
    DataSeeder.Initialize(scope.ServiceProvider);
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseRateLimiter();

app.MapGet("/", () => "HaveTickets API is running!")
   .RequireRateLimiting("fixed");

app.Run();
