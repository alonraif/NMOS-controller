namespace NmosController.Domain.Enums;

public enum AuditActionType
{
    Unknown = 0,
    RegistryUpdated = 1,
    TopologyRefreshed = 2,
    ConnectionValidated = 3,
    ReceiverConnected = 4,
    ReceiverDisconnected = 5,
    PresetCreated = 6,
    PresetUpdated = 7,
    PresetDeleted = 8,
    PresetExecuted = 9,
    SettingsChanged = 10
}
