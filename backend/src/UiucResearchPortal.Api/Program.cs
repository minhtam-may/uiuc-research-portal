using Microsoft.EntityFrameworkCore;
using UiucResearchPortal.Application;
using UiucResearchPortal.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "FrontendCors";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:4200"];

        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Apply pending EF Core migrations on startup. This keeps a free-tier PaaS deploy
// (Render/Fly/etc., where there's no shell access to run `dotnet ef database update`
// by hand) self-sufficient: push code, the API brings the schema up to date itself.
// Safe to run on every boot -- EF Core no-ops when there's nothing pending.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UiucResearchPortal.Infrastructure.Data.AppDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Skip HTTPS redirection in the container: PaaS platforms like Render terminate TLS
// at their edge proxy and forward plain HTTP to the container on the internal port,
// so redirecting here would either no-op uselessly or, without forwarded-header
// handling configured, loop.
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseCors(FrontendCorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();
