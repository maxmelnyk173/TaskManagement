using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.Features.Tasks;
using TaskManagement.Features.Users;
using TaskManagement.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

builder.Services.AddMemoryCache();

builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite("Data Source=TaskManagement.db"));

builder.Services.AddUsers();
builder.Services.AddTasks(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    const string openApiUrl = "/swagger/v1/swagger.json";
    app.MapOpenApi(openApiUrl);
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(openApiUrl, "Tasks Management API v1");
    });
}

using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
db.Database.Migrate();

app.ConfigureExceptionHandler();
app.MapUserEndpoints();
app.MapTaskEndpoints();

app.Run();
