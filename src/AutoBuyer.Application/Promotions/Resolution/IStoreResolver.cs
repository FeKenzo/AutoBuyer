namespace AutoBuyer.Application.Promotions.Resolution;

public interface IStoreResolver
{
    StoreResolution? Resolve(
        string? storeHint,
        string productUrl);
}
