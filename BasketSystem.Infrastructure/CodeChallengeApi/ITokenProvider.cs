namespace BasketSystem.Infrastructure.CodeChallengeApi;

public interface ITokenProvider
{
    Task<string> GetTokenAsync(CancellationToken cancellationToken = default);

    void Invalidate();
}
