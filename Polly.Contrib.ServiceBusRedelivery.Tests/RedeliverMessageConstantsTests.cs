namespace Polly.Contrib.ServiceBusRedelivery.Tests;

public class RedeliverMessageConstantsTests
{
    [Fact]
    public void EnsureDefaults()
    {
        RedeliverMessageConstants.DefaultBackoffType.Should().Be(DelayBackoffType.Constant);
        RedeliverMessageConstants.DefaultBaseDelay.Should().Be(TimeSpan.FromSeconds(30));
        RedeliverMessageConstants.DefaultRedeliverCount.Should().Be(5);
        RedeliverMessageConstants.MaxRedeliverCount.Should().Be(int.MaxValue);
        RedeliverMessageConstants.OnRedeliverEvent.Should().Be("OnRedeliver");
        RedeliverMessageConstants.AttemptNumberKey.Should().Be("AttemptNumber");
    }
}