namespace Polly.Contrib.ServiceBusRedelivery.Tests;

public class OnRedeliverArgumentsTests
{
    [Fact]
    public void Ctor_Ok()
    {
        var args = new OnRedeliverArguments<int>(ResilienceContextPool.Shared.Get(), Outcome.FromResult(1), 2, TimeSpan.FromSeconds(3), TimeSpan.MaxValue);

        args.Context.Should().NotBeNull();
        args.Outcome.Result.Should().Be(1);
        args.AttemptNumber.Should().Be(2);
        args.RedeliveryDelay.Should().Be(TimeSpan.FromSeconds(3));
        args.Duration.Should().Be(TimeSpan.MaxValue);
    }
}