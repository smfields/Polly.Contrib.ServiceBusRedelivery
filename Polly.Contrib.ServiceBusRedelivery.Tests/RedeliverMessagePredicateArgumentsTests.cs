namespace Polly.Contrib.ServiceBusRedelivery.Tests;

public class RedeliverMessagePredicateArgumentsTests
{
    [Fact]
    public void Ctor_Ok()
    {
        var args = new RedeliverMessagePredicateArguments<int>(ResilienceContextPool.Shared.Get(), Outcome.FromResult(1), 2);

        args.Context.Should().NotBeNull();
        args.Outcome.Result.Should().Be(1);
        args.AttemptNumber.Should().Be(2);
    }
}