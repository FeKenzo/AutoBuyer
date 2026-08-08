namespace AutoBuyer.TelegramIngestion.Worker;

public sealed class TelegramIngestionOptions
{
    public const string SectionName = "TelegramIngestion";

    public bool Enabled { get; init; }

    public int ApiId { get; init; }

    public string ApiHash { get; init; } = string.Empty;

    public string PhoneNumber { get; init; } = string.Empty;

    public string ChannelUsername { get; init; } =
        "NesTechPromocoes";

    public int InitialHistoryLimit { get; init; } = 50;

    public string SessionPath { get; init; } =
        "data/wtelegram.session";

    public string UpdatesStatePath { get; init; } =
        "data/wtelegram-updates.state";
}
