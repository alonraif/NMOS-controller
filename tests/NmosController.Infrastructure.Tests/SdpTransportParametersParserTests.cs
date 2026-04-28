using NmosController.Domain.ValueObjects;
using NmosController.Infrastructure.Nmos.Parsing;

namespace NmosController.Infrastructure.Tests;

public sealed class SdpTransportParametersParserTests
{
    [Fact]
    public void BuildPrimaryLeg_WhenSdpContainsMediaAndConnection_ExtractsTransportParameters()
    {
        var transportFile = new TransportFileData(
            "application/sdp",
            """
            v=0
            o=- 1 1 IN IP4 192.168.170.2
            s=Example
            t=0 0
            m=video 2050 RTP/AVP 96
            c=IN IP4 239.0.1.1/10
            a=rtpmap:96 raw/90000
            """);

        var result = SdpTransportParametersParser.BuildPrimaryLeg(transportFile, "192.168.170.4");

        Assert.NotNull(result);
        var leg = Assert.Single(result);
        Assert.Equal(2050, leg["destination_port"]);
        Assert.Equal("192.168.170.4", leg["interface_ip"]);
        Assert.Equal("239.0.1.1", leg["multicast_ip"]);
        Assert.Equal("0.0.0.0", leg["source_ip"]);
        Assert.Equal(true, leg["rtp_enabled"]);
    }

    [Fact]
    public void BuildPrimaryLeg_WhenSdpMissingMedia_ReturnsNull()
    {
        var transportFile = new TransportFileData("application/sdp", "v=0\nc=IN IP4 239.0.1.1/10");

        var result = SdpTransportParametersParser.BuildPrimaryLeg(transportFile, "192.168.170.4");

        Assert.Null(result);
    }
}
