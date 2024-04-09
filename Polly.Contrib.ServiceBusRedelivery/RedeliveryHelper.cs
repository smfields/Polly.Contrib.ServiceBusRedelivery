using System;
using System.Threading.Tasks;
using System.Transactions;
using Azure.Messaging.ServiceBus;

namespace Polly.Contrib.ServiceBusRedelivery;

internal static class RedeliveryHelper
{
    private const double ExponentialFactor = 2.0;
    
    /// <summary>
    /// Returns true if the provided <see cref="TimeSpan"/> is valid for use as a delay.
    /// </summary>
    /// <param name="delay">Delay <see cref="TimeSpan"/> to validate.</param>
    /// <returns>True if the delay is valid</returns>
    public static bool IsValidDelay(TimeSpan delay) => delay >= TimeSpan.Zero;

    /// <summary>
    /// Redelivers a received message to the same topic/queue with a delay.
    /// </summary>
    /// <param name="message">The original message received from the service bus</param>
    /// <param name="sender">The <see cref="ServiceBusSender"/> that points to the correct topic/queue where the message should be redelivered.</param>
    /// <param name="receiver">The <see cref="ServiceBusSender"/> that received the original message.</param>
    /// <param name="delay">The delay that should be observed before redelivering the message.</param>
    /// <param name="newAttemptNumber">The attempt number of the redelivery.</param>
    /// <param name="timeProvider">The <see cref="TimeProvider"/> to use.</param>
    public static async Task RedeliverMessageWithDelay(
        ServiceBusReceivedMessage message,
        ServiceBusSender sender,
        ServiceBusReceiver receiver,
        TimeSpan delay,
        int newAttemptNumber,
        TimeProvider timeProvider)
    {
        using (var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            await receiver.CompleteMessageAsync(message).ConfigureAwait(false);
            var newMessage = new ServiceBusMessage(message)
            {
                ApplicationProperties =
                {
                    [RedeliverMessageConstants.AttemptNumberKey] = newAttemptNumber
                },
                ScheduledEnqueueTime = timeProvider.GetUtcNow() + delay
            };
            await sender.SendMessageAsync(newMessage).ConfigureAwait(false);
            ts.Complete();
        }
    }

    /// <summary>
    /// Calculates the delay based on the configured settings.
    /// </summary>
    /// <param name="type">Type of delay being used.</param>
    /// <param name="attempt">Current attempt number.</param>
    /// <param name="baseDelay">User-configured base delay.</param>
    /// <param name="maxDelay">User-configured maximum delay.</param>
    /// <returns></returns>
    public static TimeSpan GetRedeliveryDelay(
        DelayBackoffType type,
        int attempt,
        TimeSpan baseDelay,
        TimeSpan? maxDelay)
    {
        try
        {
            var delay = GetRetryDelayCore(type, attempt, baseDelay);

            if (maxDelay.HasValue && delay > maxDelay.Value)
            {
                return maxDelay.Value;
            }

            return delay;
        }
        catch (OverflowException)
        {
            if (maxDelay is { } value)
            {
                return value;
            }

            return TimeSpan.MaxValue;
        }
    }

    private static TimeSpan GetRetryDelayCore(DelayBackoffType type, int attempt, TimeSpan baseDelay)
    {
        if (baseDelay == TimeSpan.Zero)
        {
            return baseDelay;
        }

        return type switch
        {
            DelayBackoffType.Constant => baseDelay,
            DelayBackoffType.Linear => TimeSpan.FromMilliseconds((attempt + 1) * baseDelay.TotalMilliseconds),
            DelayBackoffType.Exponential => TimeSpan.FromMilliseconds(Math.Pow(ExponentialFactor, attempt) * baseDelay.TotalMilliseconds),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "The retry backoff type is not supported.")
        };
    }
}