namespace BasketSystem.Application.Configuration;

public sealed class CodeChallengeApiOptions
{
    public const string SectionName = "CodeChallengeApi";

    public string BaseUrl { get; init; } = "https://azfun-impact-code-challenge-api.azurewebsites.net";

    public string Email { get; init; } = "seb@challenge.dk";

    public TimeSpan RefreshInterval { get; init; } = TimeSpan.FromMinutes(4);
}
