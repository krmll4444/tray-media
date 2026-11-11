using TrayMedia.Services;
using Xunit;

namespace TrayMedia.Tests;

public class TimeFormattingTests
{
    [Theory]
    [InlineData(0, "0:00")]
    [InlineData(5, "0:05")]
    [InlineData(65, "1:05")]
    [InlineData(125.9, "2:05")]
    [InlineData(-3, "0:00")]
    public void Format_returns_m_ss(double input, string expected)
    {
        Assert.Equal(expected, TimeFormatting.Format(input));
    }
}
