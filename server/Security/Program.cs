using DataAccess;
using Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.ConfigureInfrastructureServices(
    builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db");
builder.Services.ConfigureDomainServices(
    builder.Configuration["JwtSecret"] ?? throw new InvalidOperationException("JwtSecret is missing."));

var app = builder.Build();

// Create the database and tables the first time the app starts.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();