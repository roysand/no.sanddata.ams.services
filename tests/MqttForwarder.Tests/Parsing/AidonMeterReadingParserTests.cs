using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using MqttForwarder.Contracts;
using MqttForwarder.Parsing;

namespace MqttForwarder.Tests.Parsing;

public class AidonMeterReadingParserTests
{
    private readonly AidonMeterReadingParser _parser = new(NullLogger<AidonMeterReadingParser>.Instance);

    // Captured live from the reader (2026-09-26): the frequent, short "power only" message.
    private const string FrequentPowerOnlyMessage =
        """
        {"id":"58:CF:79:9C:93:AE","name":"iot-ams-producer-home-s2","up":686592,"t":1790435942,"vcc":3.265,"rssi":-63,"temp":-127.00,"data":{"P":1007},"realtime":{"h":0.42,"d":26.0,"t":5,"x":4.19,"he":0.00,"de":0.0}}
        """;

    // Captured live from the reader: the less-frequent message that also carries meter identity
    // and the per-phase/reactive fields the v1 ingestion scope deliberately ignores.
    private const string PeriodicFullMessage =
        """
        {"id":"58:CF:79:9C:93:AE","name":"iot-ams-producer-home-s2","up":686591,"t":1790435940,"vcc":3.268,"rssi":-70,"temp":-127.00,"data":{"lv":"AIDON_V0001","meterId":"7359992896383454","type":"6525","P":1028,"Q":0,"PO":0,"QO":1026,"I1":4.80,"I2":0.00,"I3":-2.80,"U1":240.70,"U2":240.30,"U3":239.90},"realtime":{"h":0.42,"d":26.0,"t":5,"x":4.19,"he":0.00,"de":0.0}}
        """;

    [Fact]
    public void Parse_FrequentPowerOnlyMessage_ReturnsReadingWithoutMeterIdentity()
    {
        CanonicalReading? reading = _parser.Parse(Encoding.UTF8.GetBytes(FrequentPowerOnlyMessage));

        Assert.NotNull(reading);
        Assert.Equal("58:CF:79:9C:93:AE", reading!.DeviceId);
        Assert.Equal(1790435942, reading.TimestampUnixSeconds);
        Assert.Equal(1007, reading.PowerWatts);
        Assert.Null(reading.MeterId);
        Assert.Null(reading.MeterType);
    }

    [Fact]
    public void Parse_PeriodicFullMessage_ReturnsReadingWithMeterIdentity()
    {
        CanonicalReading? reading = _parser.Parse(Encoding.UTF8.GetBytes(PeriodicFullMessage));

        Assert.NotNull(reading);
        Assert.Equal("58:CF:79:9C:93:AE", reading!.DeviceId);
        Assert.Equal(1790435940, reading.TimestampUnixSeconds);
        Assert.Equal(1028, reading.PowerWatts);
        Assert.Equal("7359992896383454", reading.MeterId);
        Assert.Equal("6525", reading.MeterType);
    }

    [Fact]
    public void Parse_MissingDataObject_ReturnsNull()
    {
        const string payload = """{"id":"58:CF:79:9C:93:AE","t":1790435942}""";

        CanonicalReading? reading = _parser.Parse(Encoding.UTF8.GetBytes(payload));

        Assert.Null(reading);
    }

    [Fact]
    public void Parse_MissingDeviceId_ReturnsNull()
    {
        const string payload = """{"t":1790435942,"data":{"P":1007}}""";

        CanonicalReading? reading = _parser.Parse(Encoding.UTF8.GetBytes(payload));

        Assert.Null(reading);
    }

    [Fact]
    public void Parse_MalformedJson_ReturnsNullRatherThanThrowing()
    {
        const string payload = "{ this is not valid json";

        CanonicalReading? reading = _parser.Parse(Encoding.UTF8.GetBytes(payload));

        Assert.Null(reading);
    }
}
