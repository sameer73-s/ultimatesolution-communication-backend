using UltimateSolution.Application.DependencyInjection;
using UltimateSolution.Infrastructure.DependencyInjection;
using UltimateSolution.API.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapOpenApi();
app.MapControllers();

app.Run();

public partial class Program;
