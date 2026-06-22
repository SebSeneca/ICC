using Xunit;

namespace BasketSystem.Tests.TestSupport;

/// <summary>
/// A <see cref="FactAttribute"/> that is skipped unless RUN_INTEGRATION_TESTS=1 (or true) is set,
/// so the default test run stays fast and network-free.
/// </summary>
public sealed class IntegrationFactAttribute : FactAttribute
{
    public IntegrationFactAttribute()
    {
        if (!IntegrationTestGate.Enabled)
            Skip = "Integration test skipped. Set RUN_INTEGRATION_TESTS=1 to run tests against the real API.";
    }
}

public static class IntegrationTestGate
{
    public static bool Enabled =>
        Environment.GetEnvironmentVariable("RUN_INTEGRATION_TESTS") is "1" or "true";
}
