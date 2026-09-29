namespace AdvancedOrderSystem.Auth;

public class FrontendOptions
{
    public const string SectionName = "Frontend";

    // Where confirmation and password reset links point
    public string BaseUrl { get; set; } = "http://localhost:4200";

    public string BuildLink(string path, IDictionary<string, string> queryParameters)
    {
        var baseUrl = BaseUrl.TrimEnd('/');

        var query = string.Join(
            '&',
            queryParameters.Select(pair =>
                $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));

        return $"{baseUrl}/{path.TrimStart('/')}?{query}";
    }
}
