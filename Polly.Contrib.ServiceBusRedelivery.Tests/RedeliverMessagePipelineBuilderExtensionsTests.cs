using System.ComponentModel.DataAnnotations;
using Azure.Messaging.ServiceBus;
using Polly.Testing;

namespace Polly.Contrib.ServiceBusRedelivery.Tests;

public class RedeliverMessagePipelineBuilderExtensionsTests
{
    public static readonly TheoryData<Action<ResiliencePipelineBuilder>> OverloadsData = new()
    {
        builder =>
        {
            builder.AddMessageRedelivery(new RedeliverMessageStrategyOptions()
            {
                Message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                    body: new BinaryData("Sample Message")    
                ),
                Sender = Mock.Of<ServiceBusSender>(),
                Receiver = Mock.Of<ServiceBusReceiver>(),
                BackoffType = DelayBackoffType.Exponential,
                MaxRedeliverAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                ShouldHandle = _ => PredicateResult.True(),
            });

            AssertStrategy(builder, DelayBackoffType.Exponential, 3, TimeSpan.FromSeconds(2));
        },
    };

    public static readonly TheoryData<Action<ResiliencePipelineBuilder<int>>> OverloadsDataGeneric = new()
    {
        builder =>
        {
            builder.AddMessageRedelivery(new RedeliverMessageStrategyOptions<int>
            {
                Message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                    body: new BinaryData("Sample Message")    
                ),
                Sender = Mock.Of<ServiceBusSender>(),
                Receiver = Mock.Of<ServiceBusReceiver>(),
                BackoffType = DelayBackoffType.Exponential,
                MaxRedeliverAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                ShouldHandle = _ => PredicateResult.True()
            });

            AssertStrategy(builder, DelayBackoffType.Exponential, 3, TimeSpan.FromSeconds(2));
        },
    };
    
    [MemberData(nameof(OverloadsData))]
    [Theory]
    public void AddMessageRedelivery_Overloads_Ok(Action<ResiliencePipelineBuilder> configure)
    {
        var builder = new ResiliencePipelineBuilder();

        builder.Invoking(configure).Should().NotThrow();
    }

    [MemberData(nameof(OverloadsDataGeneric))]
    [Theory]
    public void AddMessageRedelivery_GenericOverloads_Ok(Action<ResiliencePipelineBuilder<int>> configure)
    {
        var builder = new ResiliencePipelineBuilder<int>();

        builder.Invoking(configure).Should().NotThrow();
    }
    
    [Fact]
    public void AddMessageRedelivery_DefaultOptions_Ok()
    {
        var builder = new ResiliencePipelineBuilder();
        var options = new RedeliverMessageStrategyOptions
        {
            Message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: new BinaryData("Sample Message")    
            ),
            Sender = Mock.Of<ServiceBusSender>(),
            Receiver = Mock.Of<ServiceBusReceiver>(),
            ShouldHandle = _ => PredicateResult.True()
        };

        builder.AddMessageRedelivery(options);

        AssertStrategy(builder, options.BackoffType, options.MaxRedeliverAttempts, options.Delay);
    }
    
    private static void AssertStrategy(
        ResiliencePipelineBuilder builder, 
        DelayBackoffType type, 
        int redeliveries, 
        TimeSpan delay, 
        Action<RedeliverMessageResilienceStrategy<object>>? assert = null)
    {
        var strategy = builder.Build().GetPipelineDescriptor().FirstStrategy.StrategyInstance.Should().BeOfType<RedeliverMessageResilienceStrategy<object>>().Subject;

        strategy.BackoffType.Should().Be(type);
        strategy.MaxRedeliverAttempts.Should().Be(redeliveries);
        strategy.BaseDelay.Should().Be(delay);

        assert?.Invoke(strategy);
    }

    private static void AssertStrategy<T>(
        ResiliencePipelineBuilder<T> builder,
        DelayBackoffType type,
        int redeliveries,
        TimeSpan delay,
        Action<RedeliverMessageResilienceStrategy<T>>? assert = null)
    {
        var strategy = builder.Build().GetPipelineDescriptor().FirstStrategy.StrategyInstance.Should().BeOfType<RedeliverMessageResilienceStrategy<T>>().Subject;

        strategy.BackoffType.Should().Be(type);
        strategy.MaxRedeliverAttempts.Should().Be(redeliveries);
        strategy.BaseDelay.Should().Be(delay);

        assert?.Invoke(strategy);
    }
    
    [Fact]
    public void AddMessageRedelivery_MissingMessage_Throws()
    {
        new ResiliencePipelineBuilder()
            .Invoking(b => b.AddMessageRedelivery(new RedeliverMessageStrategyOptions
            {
                Message = null!,
                Sender = Mock.Of<ServiceBusSender>(),
                Receiver = Mock.Of<ServiceBusReceiver>(),
                ShouldHandle = _ => PredicateResult.True()
            }))
            .Should()
            .Throw<ValidationException>();
    }
    
    [Fact]
    public void AddMessageRedelivery_MissingSender_Throws()
    {
        new ResiliencePipelineBuilder()
            .Invoking(b => b.AddMessageRedelivery(new RedeliverMessageStrategyOptions
            {
                Message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                    body: new BinaryData("Sample Message")    
                ),
                Sender = null!,
                Receiver = Mock.Of<ServiceBusReceiver>(),
                ShouldHandle = _ => PredicateResult.True()
            }))
            .Should()
            .Throw<ValidationException>();
    }
    
    [Fact]
    public void AddMessageRedelivery_MissingReceiver_Throws()
    {
        new ResiliencePipelineBuilder()
            .Invoking(b => b.AddMessageRedelivery(new RedeliverMessageStrategyOptions
            {
                Message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                    body: new BinaryData("Sample Message")    
                ),
                Sender = Mock.Of<ServiceBusSender>(),
                Receiver = null!,
                ShouldHandle = _ => PredicateResult.True()
            }))
            .Should()
            .Throw<ValidationException>();
    }
    
    [Fact]
    public void AddMessageRedelivery_MissingShouldHandle_Throws()
    {
        new ResiliencePipelineBuilder()
            .Invoking(b => b.AddMessageRedelivery(new RedeliverMessageStrategyOptions
            {
                Message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                    body: new BinaryData("Sample Message")    
                ),
                Sender = Mock.Of<ServiceBusSender>(),
                Receiver = Mock.Of<ServiceBusReceiver>(),
                ShouldHandle = null!
            }))
            .Should()
            .Throw<ValidationException>();
    }
    
    [Fact]
    public void AddMessageRedelivery_MissingBuilder_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            RedeliverMessagePipelineBuilderExtensions.AddMessageRedelivery(
                null!,
                new RedeliverMessageStrategyOptions());
        });
    }
    
    [Fact]
    public void AddMessageRedelivery_MissingOptions_Throws()
    {
        new ResiliencePipelineBuilder()
            .Invoking(b => b.AddMessageRedelivery(null!))
            .Should()
            .Throw<ArgumentNullException>();
    }
    
    [Fact]
    public void AddMessageRedelivery_GenericMissingBuilder_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            RedeliverMessagePipelineBuilderExtensions.AddMessageRedelivery(
                null!,
                new RedeliverMessageStrategyOptions<int>());
        });
    }
    
    [Fact]
    public void AddMessageRedelivery_GenericMissingOptions_Throws()
    {
        new ResiliencePipelineBuilder<int>()
            .Invoking(b => b.AddMessageRedelivery(null!))
            .Should()
            .Throw<ArgumentNullException>();
    }
}