using DartsScoring.Api.Data;
using DartsScoring.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost,1433;Database=DartsScoring;User Id=sa;Password=Your_password123;TrustServerCertificate=True";

builder.Services.AddDbContext<DartsContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IScoringService, ScoringService>();
builder.Services.AddScoped<IMatchService, MatchService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DartsContext>();
    context.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("frontend");
app.UseHttpsRedirection();

app.MapControllers();

app.Run();
