using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Api.Tests;

public class StartupConfigurationValidationTests
{
    [Theory]
    [InlineData("EligibilityRules:RestrictedStates:0", "XX")]
    [InlineData("Blacklist:Ssns:0", "12345")]
    public void Startup_WhenRuleDataIsInvalid_Fails(string key, string value)
    {
        using var factory = new ApiTestFactory().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { [key] = value })));

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains(typeof(OptionsValidationException), Flatten(exception));
    }

    private static IEnumerable<Type> Flatten(Exception? exception)
    {
        for (; exception is not null; exception = exception.InnerException)
        {
            yield return exception.GetType();
        }
    }
}
