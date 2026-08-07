namespace AutoBuyer.Application.Contracts.Requests.ProductTargets;

public sealed record ChangeMonitoringStatusRequest(
    bool Enabled);