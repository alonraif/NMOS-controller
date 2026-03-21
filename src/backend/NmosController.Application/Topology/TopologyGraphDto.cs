namespace NmosController.Application.Topology;

public sealed record TopologyGraphDto(
    RegistrySummaryDto Registry,
    IReadOnlyCollection<NmosNodeDto> Nodes,
    IReadOnlyCollection<NmosDeviceDto> Devices,
    IReadOnlyCollection<NmosSourceDto> Sources,
    IReadOnlyCollection<NmosFlowDto> Flows,
    IReadOnlyCollection<NmosSenderDto> Senders,
    IReadOnlyCollection<NmosReceiverDto> Receivers,
    DateTimeOffset RefreshedAtUtc);
