namespace AutoBuyer.Application.Promotions.Resolution;

public interface IPromotionUrlResolver
{
    Task<PromotionUrlResolution> ResolveAsync(
        string originalUrl,
        CancellationToken cancellationToken);
}
