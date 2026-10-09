namespace Domain.Tests;

public class SsnTests
{
    [Theory]
    [InlineData("123-45-6789", "123456789")]
    [InlineData("123456789", "123456789")]
    [InlineData("123-456789", "123456789")]
    public void Normalize_ReturnsDigitsOnly(string input, string expected) =>
        Assert.Equal(expected, Ssn.Normalize(input));

    [Theory]
    [InlineData("123-45-6789", true)]
    [InlineData("123456789", true)]
    [InlineData("923-45-6789", false)]
    [InlineData("999999999", false)]
    [InlineData("12345678", false)]
    [InlineData("", false)]
    public void IsValid_RequiresNineDigitsNotStartingWithNine(string input, bool expected) =>
        Assert.Equal(expected, Ssn.IsValid(input));
}
