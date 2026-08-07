namespace AutoBuyer.Domain.Enums;

public enum StoreMonitoringStatus
{
    Supported = 1,
    TemporarilyBlocked = 2,
    RequiresSession = 3,
    RequiresManualAction = 4,
    Unsupported = 5
}   