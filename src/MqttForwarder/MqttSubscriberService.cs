using System.Buffers;
using System.Security.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MqttForwarder.Configuration;
using MqttForwarder.Contracts;
using MqttForwarder.Outbox;
using MqttForwarder.Parsing;

namespace MqttForwarder;

/// <summary>Subscribes to the reader's MQTT topic and enqueues every parsed reading to the outbox.</summary>
public class MqttSubscriberService : BackgroundService
{
    private readonly MqttOptions _options;
    private readonly IMeterReadingParser _parser;
    private readonly IOutboxStore _outbox;
    private readonly ILogger<MqttSubscriberService> _logger;
    private readonly MqttClientFactory _factory = new();
    private IMqttClient? _client;

    public MqttSubscriberService(
        IOptions<MqttOptions> options,
        IMeterReadingParser parser,
        IOutboxStore outbox,
        ILogger<MqttSubscriberService> logger)
    {
        _options = options.Value;
        _parser = parser;
        _outbox = outbox;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _client = _factory.CreateMqttClient();

        _client.ApplicationMessageReceivedAsync += async args =>
        {
            try
            {
                CanonicalReading? reading = _parser.Parse(args.ApplicationMessage.Payload.ToArray());
                if (reading is not null)
                {
                    await _outbox.EnqueueAsync(reading, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process an incoming MQTT message");
            }
        };

        _client.DisconnectedAsync += async args =>
        {
            _logger.LogWarning(args.Exception, "Disconnected from MQTT broker, reconnecting in 5s");
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            await ConnectAndSubscribeAsync(stoppingToken);
        };

        await ConnectAndSubscribeAsync(stoppingToken);

        // Keep the service alive until cancellation; all real work happens in the event handlers above.
        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
    }

    private async Task ConnectAndSubscribeAsync(CancellationToken ct)
    {
        MqttClientOptions options = BuildClientOptions();

        try
        {
            await _client!.ConnectAsync(options, ct);
            _logger.LogInformation("Connected to MQTT broker at {ServerUri}", _options.ServerUri);

            MqttClientSubscribeOptions subscribeOptions = _factory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(_options.Topic))
                .Build();
            await _client.SubscribeAsync(subscribeOptions, ct);
            _logger.LogInformation("Subscribed to topic {Topic}", _options.Topic);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect/subscribe, will retry on next disconnect signal");
        }
    }

    private MqttClientOptions BuildClientOptions()
    {
        MqttClientOptionsBuilder builder = _factory.CreateClientOptionsBuilder()
            .WithClientId($"mqtt-forwarder-{Guid.NewGuid():N}")
            .WithTcpServer(_options.ServerUri, _options.ServerPort)
            .WithCleanSession();

        if (!string.IsNullOrEmpty(_options.UserName))
        {
            builder = builder.WithCredentials(_options.UserName, _options.Password);
        }

        if (_options.UseTls)
        {
            builder = builder.WithTlsOptions(o => o.WithSslProtocols(SslProtocols.Tls12 | SslProtocols.Tls13));
        }

        return builder.Build();
    }

    public override void Dispose()
    {
        _client?.Dispose();
        base.Dispose();
    }
}
