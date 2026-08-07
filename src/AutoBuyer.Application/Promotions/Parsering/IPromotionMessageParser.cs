namespace AutoBuyer.Application.Promotions.Parsing;

public interface IPromotionMessageParser
{
    PromotionParseResult Parse(string? message);
}