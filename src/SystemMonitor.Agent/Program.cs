using Serilog;
using SystemMonitor.Agent.Startup;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.RegisterLogger();
builder.Services.AddSignalR();
builder.Services.AddCors(builder.Configuration);

builder.Services.AddIdentity();
builder.Services.AddAuth(builder.Configuration);
builder.Services.AddSwaggerWithAuth();


var app = builder.Build();

app.UsePersistence();
app.MapControllers();
app.MapSocketEndpoints();

app.UseSerilogRequestLogging();
app.UseCors();

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapIdentityEndpoints();

app.Run();
