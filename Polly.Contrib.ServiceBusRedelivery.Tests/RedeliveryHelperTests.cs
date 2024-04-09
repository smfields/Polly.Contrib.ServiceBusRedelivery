using Azure.Messaging.ServiceBus;

namespace Polly.Contrib.ServiceBusRedelivery.Tests;

public class RedeliveryHelperTests
{
    [Fact]
    public void IsValidDelay_Ok()
    {
        RedeliveryHelper.IsValidDelay(TimeSpan.Zero).Should().BeTrue();
        RedeliveryHelper.IsValidDelay(TimeSpan.FromSeconds(1)).Should().BeTrue();
        RedeliveryHelper.IsValidDelay(TimeSpan.MaxValue).Should().BeTrue();
        RedeliveryHelper.IsValidDelay(TimeSpan.MinValue).Should().BeFalse();
        RedeliveryHelper.IsValidDelay(TimeSpan.FromMilliseconds(-1)).Should().BeFalse();
    }
    
    [Fact]
    public void UnsupportedRetryBackoffType_Throws()
    {
        const DelayBackoffType type = (DelayBackoffType)99;
        Assert.Throws<ArgumentOutOfRangeException>(() => RedeliveryHelper.GetRedeliveryDelay(type, 0, TimeSpan.FromSeconds(1), null));
    }
    
    [Fact]
    public void GetRedeliveryDelay_Constant_Ok()
    {
        RedeliveryHelper.GetRedeliveryDelay(DelayBackoffType.Constant, 0, TimeSpan.Zero, null).Should().Be(TimeSpan.Zero);
        RedeliveryHelper.GetRedeliveryDelay(DelayBackoffType.Constant, 1, TimeSpan.Zero, null).Should().Be(TimeSpan.Zero);
        RedeliveryHelper.GetRedeliveryDelay(DelayBackoffType.Constant, 2, TimeSpan.Zero, null).Should().Be(TimeSpan.Zero);
        
        RedeliveryHelper.GetRedeliveryDelay(DelayBackoffType.Constant, 0, TimeSpan.FromSeconds(1), null).Should().Be(TimeSpan.FromSeconds(1));
        RedeliveryHelper.GetRedeliveryDelay(DelayBackoffType.Constant, 1, TimeSpan.FromSeconds(1), null).Should().Be(TimeSpan.FromSeconds(1));
        RedeliveryHelper.GetRedeliveryDelay(DelayBackoffType.Constant, 2, TimeSpan.FromSeconds(1), null).Should().Be(TimeSpan.FromSeconds(1));
    }
    
    [Fact]
    public void GetRedeliveryDelay_Linear_Ok()
    {
        RedeliveryHelper.GetRedeliveryDelay(DelayBackoffType.Linear, 0, TimeSpan.Zero, null).Should().Be(TimeSpan.Zero);
        RedeliveryHelper.GetRedeliveryDelay(DelayBackoffType.Linear, 1, TimeSpan.Zero, null).Should().Be(TimeSpan.Zero);
        RedeliveryHelper.GetRedeliveryDelay(DelayBackoffType.Linear, 2, TimeSpan.Zero, null).Should().Be(TimeSpan.Zero);
        
        RedeliveryHelper.GetRedeliveryDelay(DelayBackoffType.Linear, 0, TimeSpan.FromSeconds(1), null).Should().Be(TimeSpan.FromSeconds(1));
        RedeliveryHelper.GetRedeliveryDelay(DelayBackoffType.Linear, 1, TimeSpan.FromSeconds(1), null).Should().Be(TimeSpan.FromSeconds(2));
        RedeliveryHelper.GetRedeliveryDelay(DelayBackoffType.Linear, 2, TimeSpan.FromSeconds(1), null).Should().Be(TimeSpan.FromSeconds(3));
    }
    
    [Fact]
    public void GetRedeliveryDelay_Exponential_Ok()
    {
        RedeliveryHelper.GetRedeliveryDelay(DelayBackoffType.Exponential, 0, TimeSpan.Zero, null).Should().Be(TimeSpan.Zero);
        RedeliveryHelper.GetRedeliveryDelay(DelayBackoffType.Exponential, 1, TimeSpan.Zero, null).Should().Be(TimeSpan.Zero);
        RedeliveryHelper.GetRedeliveryDelay(DelayBackoffType.Exponential, 2, TimeSpan.Zero, null).Should().Be(TimeSpan.Zero);
        
        RedeliveryHelper.GetRedeliveryDelay(DelayBackoffType.Exponential, 0, TimeSpan.FromSeconds(1), null).Should().Be(TimeSpan.FromSeconds(1));
        RedeliveryHelper.GetRedeliveryDelay(DelayBackoffType.Exponential, 1, TimeSpan.FromSeconds(1), null).Should().Be(TimeSpan.FromSeconds(2));
        RedeliveryHelper.GetRedeliveryDelay(DelayBackoffType.Exponential, 2, TimeSpan.FromSeconds(1), null).Should().Be(TimeSpan.FromSeconds(4));
    }
    
    [InlineData(DelayBackoffType.Linear)]
    [InlineData(DelayBackoffType.Exponential)]
    [InlineData(DelayBackoffType.Constant)]
    [Theory]
    public void MaxDelay_Ok(DelayBackoffType type)
    {
        var expected = TimeSpan.FromSeconds(1);

        RedeliveryHelper.GetRedeliveryDelay(type, 2, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1)).Should().Be(expected);
    }
    
    [Fact]
    public void MaxDelay_DelayLessThanMaxDelay_Respected()
    {
        var expected = TimeSpan.FromSeconds(1);

        RedeliveryHelper.GetRedeliveryDelay(DelayBackoffType.Constant, 2, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)).Should().Be(expected);
    }
    
    [Fact]
    public void GetRedeliveryDelay_Overflow_ReturnsMaxTimeSpan()
    {
        RedeliveryHelper.GetRedeliveryDelay(DelayBackoffType.Exponential, 1000, TimeSpan.FromDays(1), null).Should().Be(TimeSpan.MaxValue);
    }
    
    [Fact]
    public void GetRetryDelay_OverflowWithMaxDelay_ReturnsMaxDelay()
    {
        RedeliveryHelper.GetRedeliveryDelay(DelayBackoffType.Exponential, 1000, TimeSpan.FromDays(1), TimeSpan.FromDays(2)).Should().Be(TimeSpan.FromDays(2));
    }

    [Fact]
    public async Task RedeliverMessageWithDelay_CompletesMessage()
    {
        var currentTime = DateTimeOffset.UtcNow;
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(new BinaryData("MessageBody"));
        var sender = Mock.Of<ServiceBusSender>();
        var receiverMock = new Mock<ServiceBusReceiver>();
        var timeProvider = Mock.Of<TimeProvider>(x => x.GetUtcNow() == currentTime);
        await RedeliveryHelper.RedeliverMessageWithDelay(
            message,
            sender,
            receiverMock.Object,
            TimeSpan.FromSeconds(10),
            2,
            timeProvider
        );
        
        receiverMock.Verify(x => x.CompleteMessageAsync(message, It.IsAny<CancellationToken>()));
    }
    
    [Fact]
    public async Task RedeliverMessageWithDelay_SchedulesNewMessage()
    {
        var currentTime = DateTimeOffset.UtcNow;
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(new BinaryData("MessageBody"));
        var senderMock = new Mock<ServiceBusSender>();
        var sender = senderMock.Object;
        var receiver = Mock.Of<ServiceBusReceiver>();
        var timeProvider = Mock.Of<TimeProvider>(x => x.GetUtcNow() == currentTime);
        await RedeliveryHelper.RedeliverMessageWithDelay(
            message,
            sender,
            receiver,
            TimeSpan.FromSeconds(10),
            2,
            timeProvider
        );
        
        senderMock.Verify(
            x => x.SendMessageAsync(
                It.Is<ServiceBusMessage>(m => m.MessageId == message.MessageId), 
                It.IsAny<CancellationToken>()));
    }
    
    [Fact]
    public async Task RedeliverMessageWithDelay_UpdatesAttemptNumber()
    {
        var currentTime = DateTimeOffset.UtcNow;
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(new BinaryData("MessageBody"));
        var senderMock = new Mock<ServiceBusSender>();
        var sender = senderMock.Object;
        var receiver = Mock.Of<ServiceBusReceiver>();
        var timeProvider = Mock.Of<TimeProvider>(x => x.GetUtcNow() == currentTime);
        var newAttemptNumber = 2;
        await RedeliveryHelper.RedeliverMessageWithDelay(
            message,
            sender,
            receiver,
            TimeSpan.FromSeconds(10),
            newAttemptNumber,
            timeProvider
        );
        
        senderMock.Verify(
            x => x.SendMessageAsync(
                It.Is<ServiceBusMessage>(m => (int)m.ApplicationProperties[RedeliverMessageConstants.AttemptNumberKey] == newAttemptNumber), 
                It.IsAny<CancellationToken>()));
    }
    
    [Fact]
    public async Task RedeliverMessageWithDelay_SetsScheduledEnqueueTime()
    {
        var currentTime = DateTimeOffset.UtcNow;
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(new BinaryData("MessageBody"));
        var senderMock = new Mock<ServiceBusSender>();
        var sender = senderMock.Object;
        var receiver = Mock.Of<ServiceBusReceiver>();
        var timeProvider = Mock.Of<TimeProvider>(x => x.GetUtcNow() == currentTime);
        var delay = TimeSpan.FromSeconds(10);
        await RedeliveryHelper.RedeliverMessageWithDelay(
            message,
            sender,
            receiver,
            TimeSpan.FromSeconds(10),
            2,
            timeProvider
        );
        
        senderMock.Verify(
            x => x.SendMessageAsync(
                It.Is<ServiceBusMessage>(m => m.ScheduledEnqueueTime == currentTime + delay), 
                It.IsAny<CancellationToken>()));
    }
}