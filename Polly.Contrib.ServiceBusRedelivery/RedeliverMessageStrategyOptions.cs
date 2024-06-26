﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Polly.Contrib.ServiceBusRedelivery;

/// <inheritdoc/>
public class RedeliverMessageStrategyOptions : RedeliverMessageStrategyOptions<object>
{
}

/// <summary>
/// Represents the options used to configure a redelivery strategy.
/// </summary>
/// <typeparam name="TResult">The type of result the redelivery strategy handles.</typeparam>
public class RedeliverMessageStrategyOptions<TResult> : ResilienceStrategyOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RedeliverMessageStrategyOptions{TResult}"/> class.
    /// </summary>
    public RedeliverMessageStrategyOptions() => Name = RedeliverMessageConstants.DefaultName;

    /// <summary>
    /// Gets or sets the message that will be redelivered when a failure occurs.
    /// </summary>
    [Required] 
    public ServiceBusReceivedMessage Message { get; set; } = default!;

    /// <summary>
    /// Gets or sets the sender that will be used to requeue the message.
    /// </summary>
    [Required]
    public ServiceBusSender Sender { get; set; } = default!;

    /// <summary>
    /// Gets or sets the receiver that received the original message.
    /// </summary>
    /// <remarks>
    /// Used to complete the original message
    /// </remarks>
    [Required]
    public ServiceBusReceiver Receiver { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the maximum number of redeliveries to use, in addition to the original call.
    /// </summary>
    /// <value>
    /// The default value is 5 redeliveries.
    /// </value>
    /// <remarks>
    /// To redeliver indefinitely use <see cref="int.MaxValue"/>. Note that the reported attempt number is capped at <see cref="int.MaxValue"/>.
    /// </remarks>
    [Range(1, RedeliverMessageConstants.MaxRedeliverCount)]
    public int MaxRedeliverAttempts { get; set; } = RedeliverMessageConstants.DefaultRedeliverCount;

    /// <summary>
    /// Gets or sets the type of the back-off.
    /// </summary>
    /// <remarks>
    /// This property is ignored when <see cref="DelayGenerator"/> is set.
    /// </remarks>
    /// <value>
    /// The default value is <see cref="DelayBackoffType.Constant"/>.
    /// </value>
    public DelayBackoffType BackoffType { get; set; } = RedeliverMessageConstants.DefaultBackoffType;

    /// <summary>
    /// Gets or sets the behavior when the last delivery attempt fails
    /// </summary>
    /// <value>
    /// The default value is <see cref="MessageAction.DeadLetter"/>
    /// </value>
    public MessageAction LastAttemptFailedAction { get; set; } = RedeliverMessageConstants.DefaultLastAttemptFailedAction;

    /// <summary>
    /// Gets or sets the base delay between retries.
    /// </summary>
    /// <remarks>
    /// This value is used with the combination of <see cref="BackoffType"/> to generate the final delay for each individual retry attempt:
    /// <list type="bullet">
    /// <item>
    /// <see cref="DelayBackoffType.Exponential"/>: Represents the median delay to target before the first retry.
    /// </item>
    /// <item>
    /// <see cref="DelayBackoffType.Linear"/>: Represents the initial delay, the following delays increasing linearly with this value.
    /// </item>
    /// <item>
    /// <see cref="DelayBackoffType.Constant"/> Represents the constant delay between retries.
    /// </item>
    /// </list>
    /// This property is ignored when <see cref="DelayGenerator"/> is set and returns a valid <see cref="TimeSpan"/> value.
    /// </remarks>
    /// <value>
    /// The default value is 30 seconds.
    /// </value>
    [Range(typeof(TimeSpan), "00:00:00", "7.00:00:00")]
    public TimeSpan Delay { get; set; } = RedeliverMessageConstants.DefaultBaseDelay;

    /// <summary>
    /// Gets or sets the maximum delay between retries.
    /// </summary>
    /// <remarks>
    /// This property is used to cap the maximum delay between retries. It is useful when you want to limit the maximum delay after a certain
    /// number of retries when it could reach a unreasonably high values, especially if <see cref="DelayBackoffType.Exponential"/> backoff is used.
    /// If not specified, the delay is not capped. This property is ignored for delays generated by <see cref="DelayGenerator"/>.
    /// </remarks>
    /// <value>
    /// The default value is <see langword="null"/>.
    /// </value>
    [Range(typeof(TimeSpan), "00:00:00", "7.00:00:00")]
    public TimeSpan? MaxDelay { get; set; }
    
    /// <summary>
    /// Gets or sets a predicate that determines whether the retry should be executed for a given outcome.
    /// </summary>
    /// <value>
    /// The default is a delegate that retries on any exception except <see cref="OperationCanceledException"/>. This property is required.
    /// </value>
    [Required]
    public Func<RedeliverMessagePredicateArguments<TResult>, ValueTask<bool>> ShouldHandle { get; set; } = args => new ValueTask<bool>(args.Outcome.Exception is not null);
    
    /// <summary>
    /// Gets or sets a generator that calculates the delay between redeliveries.
    /// </summary>
    /// <remarks>
    /// The generator can override the delay generated by the redelivery strategy. If the generator returns <see langword="null"/>, the delay generated
    /// by the redelivery strategy for that attempt will be used.
    /// </remarks>
    /// <value>
    /// The default value is <see langword="null"/>.
    /// </value>
    public Func<RedeliveryDelayGeneratorArguments<TResult>, ValueTask<TimeSpan?>>? DelayGenerator { get; set; }
    
    /// <summary>
    /// Gets or sets an event delegate that is raised when the redelivery happens.
    /// </summary>
    /// <remarks>
    /// After this event, the result produced the by user-callback is discarded and disposed to prevent resource over-consumption. If
    /// you need to preserve the result for further processing, create the copy of the result or extract and store all necessary information
    /// from the result within the event.
    /// </remarks>
    /// <value>
    /// The default value is <see langword="null"/>.
    /// </value>
    public Func<OnRedeliverArguments<TResult>, ValueTask>? OnRedeliver { get; set; }
}