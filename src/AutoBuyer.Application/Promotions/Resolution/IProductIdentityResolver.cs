namespace AutoBuyer.Application.Promotions.Resolution;

public interface IProductIdentityResolver
{
    ProductIdentity Resolve(
        StoreResolution store,
        string productUrl);
}
