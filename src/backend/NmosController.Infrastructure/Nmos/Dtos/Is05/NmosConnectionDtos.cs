using System.Text.Json;
using System.Text.Json.Serialization;

namespace NmosController.Infrastructure.Nmos.Dtos.Is05;

internal sealed class NmosConnectionStateDto
{
    [JsonPropertyName("sender_id")]
    public string? SenderId { get; set; }

    [JsonPropertyName("master_enable")]
    public bool? MasterEnable { get; set; }

    [JsonPropertyName("transport_params")]
    public JsonElement TransportParams { get; set; }

    [JsonPropertyName("transport_file")]
    public NmosTransportFileDto? TransportFile { get; set; }

    [JsonPropertyName("activation")]
    public NmosActivationDto? Activation { get; set; }
}

internal sealed class NmosActivationDto
{
    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    [JsonPropertyName("requested_time")]
    public string? RequestedTime { get; set; }

    [JsonPropertyName("activation_time")]
    public string? ActivationTime { get; set; }
}

internal sealed class NmosTransportFileDto
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("data")]
    public string? Data { get; set; }
}

internal sealed class NmosReceiverConstraintsDto : List<Dictionary<string, JsonElement>>;

internal sealed class NmosConnectionPatchRequestDto
{
    [JsonPropertyName("sender_id")]
    public string? SenderId { get; set; }

    [JsonPropertyName("master_enable")]
    public bool MasterEnable { get; set; }

    [JsonPropertyName("transport_params")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyCollection<Dictionary<string, object?>>? TransportParams { get; set; }

    [JsonPropertyName("transport_file")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public NmosTransportFileDto? TransportFile { get; set; }

    [JsonPropertyName("activation")]
    public NmosPatchActivationDto Activation { get; set; } = new();
}

internal sealed class NmosPatchActivationDto
{
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "activate_immediate";

    [JsonPropertyName("requested_time")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RequestedTime { get; set; }
}
