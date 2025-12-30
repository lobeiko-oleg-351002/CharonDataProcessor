namespace CharonDataProcessor.Configuration;

public class GatewayOptions
{
    public const string SectionName = "Gateway";

    public string BaseUrl { get; set; } = "http://localhost:5004";
}

