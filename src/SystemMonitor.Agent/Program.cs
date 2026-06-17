using Serilog;
using SystemMonitor.Agent.Startup;

var builder = WebApplication.CreateBuilder(args);

builder.RegisterLogger();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddIdentity();
builder.Services.AddAuth(builder.Configuration);
builder.Services.AddSwaggerWithAuth();

//builder.Services.AddCors(builder.Configuration);

var app = builder.Build();

app.UseHttpsRedirection();
app.UsePersistence();
app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI();
//app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapIdentityEndpoints();
app.MapSocketEndpoints();
app.MapControllers();

app.Run();
