using DataAccess;
using Security;
using Security.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.ConfigureInfrastructureServices(
    builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db");
builder.Services.ConfigureDomainServices(
    builder.Configuration["JwtSecret"] ?? throw new InvalidOperationException("JwtSecret is missing."));

var app = builder.Build();

// Create the database and the first admin the first time the app starts.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    scope.ServiceProvider.GetRequiredService<IUserService>().CreateFirstAdmin(
        app.Configuration["AdminUsername"],
        app.Configuration["AdminPassword"]);
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