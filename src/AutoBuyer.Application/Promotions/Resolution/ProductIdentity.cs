namespace AutoBuyer.Application.Promotions.Resolution;

public sealed record ProductIdentity(
    string ExternalProductId,
    string CanonicalUrl,
    bool IsStoreNativeId);
