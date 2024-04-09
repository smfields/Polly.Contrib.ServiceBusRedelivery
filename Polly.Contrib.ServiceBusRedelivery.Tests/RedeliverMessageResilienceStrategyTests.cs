using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Time.Testing;

namespace Polly.Contrib.ServiceBusRedelivery.Tests;

public class RedeliverMessageResilienceStrategyTests
{
    private readonly RedeliverMessageStrategyOptions _options = new();
    private readonly FakeTimeProvider _timeProvider = new();

    public RedeliverMessageResilienceStrategyTests()
    {
        _options.Message = ServiceBusModelFactory.ServiceBusReceivedMessage(new BinaryData("MessageBody"));
        _options.Sender = Mock.Of<ServiceBusSender>();
        _options.Receiver = Mock.Of<ServiceBusReceiver>();
        _options.ShouldHandle = _ => new ValueTask<bool>(true);
    }

    [Fact]
    public void LastAttemptCompletion_CompleteMessageAction_Respected()
    {
        var mockReceiver = new Mock<ServiceBusReceiver>();
        _options.Receiver = mockReceiver.Object;
        SetupLastAttempt();
        _options.LastAttemptFailedAction = MessageAction.Complete;
        var sut = CreateSut();

        sut.Execute(() => new object());
        
        mockReceiver.Verify(x => x.CompleteMessageAsync(_options.Message, It.IsAny<CancellationToken>()));
    }
    
    [Fact]
    public void LastAttemptCompletion_AbandonMessageAction_Respected()
    {
        var mockReceiver = new Mock<ServiceBusReceiver>();
        _options.Receiver = mockReceiver.Object;
        SetupLastAttempt();
        _options.LastAttemptFailedAction = MessageAction.Abandon;
        var sut = CreateSut();

        sut.Execute(() => new object());
        
        mockReceiver.Verify(x => x.AbandonMessageAsync(_options.Message, It.IsAny<IDictionary<string,object>>(),It.IsAny<CancellationToken>()));
    }
    
    [Fact]
    public void LastAttemptCompletion_DeferMessageAction_Respected()
    {
        var mockReceiver = new Mock<ServiceBusReceiver>();
        _options.Receiver = mockReceiver.Object;
        SetupLastAttempt();
        _options.LastAttemptFailedAction = MessageAction.Defer;
        var sut = CreateSut();

        sut.Execute(() => new object());
        
        mockReceiver.Verify(x => x.DeferMessageAsync(_options.Message, It.IsAny<IDictionary<string,object>>(), It.IsAny<CancellationToken>()));
    }
    
    [Fact]
    public void LastAttemptCompletion_DeadLetterMessageAction_Respected()
    {
        var mockReceiver = new Mock<ServiceBusReceiver>();
        _options.Receiver = mockReceiver.Object;
        SetupLastAttempt();
        _options.LastAttemptFailedAction = MessageAction.DeadLetter;
        var sut = CreateSut();

        sut.Execute(() => new object());
        
        mockReceiver.Verify(x => x.DeadLetterMessageAsync(_options.Message, It.IsAny<IDictionary<string,object>>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public void LastAttemptCompletion_ThrowsException_IsPropagatedToOutcome()
    {
        var expectedException = new Exception("SomeException");
        var mockReceiver = new Mock<ServiceBusReceiver>();
        mockReceiver
            .Setup(x => x.CompleteMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<CancellationToken>()))
            .Throws(expectedException);
        _options.Receiver = mockReceiver.Object;
        SetupLastAttempt();
        _options.LastAttemptFailedAction = MessageAction.Complete;
        var sut = CreateSut();

        Assert.Throws<Exception>(() =>
        {
            sut.Execute(() => new object());
        });
    }

    [Fact]
    public void NonHandledMessages_NotCompleted()
    {
        var mockReceiver = new Mock<ServiceBusReceiver>();
        _options.Receiver = mockReceiver.Object;
        _options.ShouldHandle = _ => new ValueTask<bool>(false);
        var sut = CreateSut();

        sut.Execute(() => new object());
        
        mockReceiver.Verify(x => x.CompleteMessageAsync(_options.Message, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void MessageRedeliveryScheduled_AttemptNumberIncremented()
    {
        _options.Message = ServiceBusModelFactory.ServiceBusReceivedMessage(new BinaryData("Message"),
            properties: new Dictionary<string, object>
            {
                { RedeliverMessageConstants.AttemptNumberKey, 2 }
            });
        var mockSender = new Mock<ServiceBusSender>();
        _options.Sender = mockSender.Object;
        var sut = CreateSut();

        sut.Execute(() => new object());
        
        mockSender.Verify(x => x.SendMessageAsync(It.Is<ServiceBusMessage>(m => (int)m.ApplicationProperties[RedeliverMessageConstants.AttemptNumberKey] == 3), It.IsAny<CancellationToken>()));
    }
    
    [Fact]
    public void MessageRedeliveryScheduled_MessageScheduledWithDelay()
    {
        var currentTime = _timeProvider.GetUtcNow();
        var delay = TimeSpan.FromSeconds(10);
        _options.DelayGenerator = _ => ValueTask.FromResult<TimeSpan?>(delay);
        var mockSender = new Mock<ServiceBusSender>();
        _options.Sender = mockSender.Object;
        var sut = CreateSut();

        sut.Execute(() => new object());
        
        mockSender.Verify(x => x.SendMessageAsync(It.Is<ServiceBusMessage>(m => m.ScheduledEnqueueTime == currentTime + delay), It.IsAny<CancellationToken>()));
    }

    private void SetupLastAttempt()
    {
        _options.Message = ServiceBusModelFactory.ServiceBusReceivedMessage(new BinaryData("Message"),
            properties: new Dictionary<string, object>
            {
                { RedeliverMessageConstants.AttemptNumberKey, 1 }
            });
        _options.MaxRedeliverAttempts = 1;
    }

    private ResiliencePipeline<object> CreateSut(TimeProvider? timeProvider = null)
    {
        return new ResiliencePipelineBuilder<object>()
            {
                TimeProvider = timeProvider ?? _timeProvider
            }
            .AddMessageRedelivery(_options)
            .Build();
    }
}