using SystemMonitor.Sensor.Agent.Buffers;
using SystemMonitor.Sensor.Agent.Services;
using SystemMonitor.Sensor.Configurations;
using SystemMonitor.Sensor.Measurements.Collectors;
using SystemMonitor.Sensor.Startup;
using SystemMonitor.Shared.Notifications;

var builder = Host.CreateApplicationBuilder(args);

builder.RegisterLogger();
builder.Services.AddWindowsService(o => o.ServiceName = "SystemMonitor");
builder.Services.AddSystemd();
builder.Services.AddHostedService<AgentConnectionService>();

builder.Services.AddScoped<AgentConnectionHandler>();
builder.Services.AddScoped<IMeasurementCollector, WindowsMeasurementCollector>();
builder.Services.AddScoped<IMeasurementCollector, LinuxMeasurementCollector>();
builder.Services.AddSingleton<SensorConfiguration>();
builder.Services.AddSingleton<CircularBuffer>();
builder.Services.AddSingleton<Messenger>();
builder.Services.Configure<AgentConfiguration>(builder.Configuration.GetSection(nameof(AgentConfiguration)));
builder.Services.Configure<BufferConfiguration>(builder.Configuration.GetSection(nameof(BufferConfiguration)));

var host = builder.Build();
host.Run();
