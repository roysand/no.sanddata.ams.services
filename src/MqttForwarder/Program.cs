using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MqttForwarder;
using MqttForwarder.Configuration;
using MqttForwarder.Forwarding;
using MqttForwarder.Outbox;
using MqttForwarder.Parsing;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);

builder.Services
    .AddOptions<MqttOptions>()
    .Bind(builder.Configuration.GetSection(MqttOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<AmsApiOptions>()
    .Bind(builder.Configuration.GetSection(AmsApiOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection(OutboxOptions.SectionName));

builder.Services.AddSingleton<IMeterReadingParser, AidonMeterReadingParser>();
builder.Services.AddSingleton<IOutboxStore, SqliteOutboxStore>();

builder.Services.AddHttpClient<IAmsApiClient, AmsApiClient>((provider, client) =>
{
    AmsApiOptions options = provider.GetRequiredService<IOptions<AmsApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.DefaultRequestHeaders.Add("X-API-Key", options.ApiKey);
});

builder.Services.AddHostedService<MqttSubscriberService>();
builder.Services.AddHostedService<OutboxDrainService>();

IHost host = builder.Build();
host.Run();
