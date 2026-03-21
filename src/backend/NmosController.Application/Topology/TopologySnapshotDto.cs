namespace NmosController.Application.Topology;

public sealed record TopologySnapshotDto(
    RegistrySummaryDto Registry,
    IReadOnlyCollection<NmosNodeDto> Nodes,
    IReadOnlyCollection<NmosDeviceDto> Devices,
    IReadOnlyCollection<NmosSourceDto> Sources,
    IReadOnlyCollection<NmosFlowDto> Flows,
    IReadOnlyCollection<NmosSenderDto> Senders,
    IReadOnlyCollection<NmosReceiverDto> Receivers,
    DateTimeOffset RetrievedAtUtc);
