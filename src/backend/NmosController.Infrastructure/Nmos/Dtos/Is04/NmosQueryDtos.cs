using System.Text.Json.Serialization;

namespace NmosController.Infrastructure.Nmos.Dtos.Is04;

internal sealed class NmosNodeResourceDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("hostname")]
    public string? Hostname { get; set; }

    [JsonPropertyName("api")]
    public NmosNodeApiDto? Api { get; set; }

    [JsonPropertyName("interfaces")]
    public List<NmosNodeInterfaceDto>? Interfaces { get; set; }
}

internal sealed class NmosNodeApiDto
{
    [JsonPropertyName("versions")]
    public List<string>? Versions { get; set; }
}

internal sealed class NmosNodeInterfaceDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("port_id")]
    public string? PortId { get; set; }
}

internal sealed class NmosDeviceResourceDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("node_id")]
    public string NodeId { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string DeviceType { get; set; } = string.Empty;

    [JsonPropertyName("senders")]
    public List<string>? SenderIds { get; set; }

    [JsonPropertyName("receivers")]
    public List<string>? ReceiverIds { get; set; }

    [JsonPropertyName("tags")]
    public Dictionary<string, string[]>? Tags { get; set; }
}

internal sealed class NmosSourceResourceDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("format")]
    public string Format { get; set; } = string.Empty;

    [JsonPropertyName("grain_rate")]
    public NmosRateDto? GrainRate { get; set; }

    [JsonPropertyName("parents")]
    public List<string>? Parents { get; set; }
}

internal sealed class NmosFlowResourceDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("source_id")]
    public string SourceId { get; set; } = string.Empty;

    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("format")]
    public string Format { get; set; } = string.Empty;

    [JsonPropertyName("media_type")]
    public string? MediaType { get; set; }

    [JsonPropertyName("grain_rate")]
    public NmosRateDto? GrainRate { get; set; }

    [JsonPropertyName("frame_width")]
    public int? FrameWidth { get; set; }

    [JsonPropertyName("frame_height")]
    public int? FrameHeight { get; set; }

    [JsonPropertyName("sample_rate")]
    public NmosSampleRateDto? SampleRate { get; set; }

    [JsonPropertyName("parents")]
    public List<string>? Parents { get; set; }
}

internal sealed class NmosSenderResourceDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("flow_id")]
    public string? FlowId { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("transport")]
    public string? Transport { get; set; }

    [JsonPropertyName("manifest_href")]
    public string? ManifestHref { get; set; }

    [JsonPropertyName("interface_bindings")]
    public List<string>? InterfaceBindings { get; set; }

    [JsonPropertyName("subscription")]
    public NmosSenderSubscriptionDto? Subscription { get; set; }
}

internal sealed class NmosSenderSubscriptionDto
{
    [JsonPropertyName("receiver_id")]
    public string? ReceiverId { get; set; }
}

internal sealed class NmosReceiverResourceDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("format")]
    public string Format { get; set; } = string.Empty;

    [JsonPropertyName("transport")]
    public string? Transport { get; set; }

    [JsonPropertyName("interface_bindings")]
    public List<string>? InterfaceBindings { get; set; }

    [JsonPropertyName("caps")]
    public NmosReceiverCapsDto? Caps { get; set; }
}

internal sealed class NmosReceiverCapsDto
{
    [JsonPropertyName("media_types")]
    public List<string>? MediaTypes { get; set; }
}

internal sealed class NmosRateDto
{
    [JsonPropertyName("numerator")]
    public int Numerator { get; set; }

    [JsonPropertyName("denominator")]
    public int? Denominator { get; set; }
}

internal sealed class NmosSampleRateDto
{
    [JsonPropertyName("numerator")]
    public int Numerator { get; set; }

    [JsonPropertyName("denominator")]
    public int? Denominator { get; set; }
}
