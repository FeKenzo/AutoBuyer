namespace AutoBuyer.Domain.Entities;

public sealed class Store : Entity
{
    private Store()
    {
        // Necessário para o Entity Framework.
    }

    public Store(string name, string baseUrl)
    {
        SetName(name);
        SetBaseUrl(baseUrl);

        IsEnabled = true;
        CreatedAt = DateTime.UtcNow;
    }

    public string Name { get; private set; } = string.Empty;

    public string BaseUrl { get; private set; } = string.Empty;

    public bool IsEnabled { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public void Enable()
    {
        IsEnabled = true;
    }

    public void Disable()
    {
        IsEnabled = false;
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "O nome da loja é obrigatório.",
                nameof(name));

        Name = name.Trim();
    }

    public void SetBaseUrl(string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "A URL base da loja é inválida.",
                nameof(baseUrl));
        }

        BaseUrl = uri.GetLeftPart(UriPartial.Authority);
    }
}