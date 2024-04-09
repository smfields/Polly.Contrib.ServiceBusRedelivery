using System;

namespace Polly.Contrib.ServiceBusRedelivery;

/// <summary>
/// Represents the arguments used by <see cref="RedeliverMessageStrategyOptions{TResult}.OnRedeliver"/> for handling the redelivery event.
/// </summary>
/// <typeparam name="TResult">The type of result.</typeparam>
/// <remarks>
/// Always use the constructor when creating this struct, otherwise we do not guarantee binary compatibility.
/// </remarks>
public readonly struct OnRedeliverArguments<TResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OnRedeliverArguments{TResult}"/> struct.
    /// </summary>
    /// <param name="outcome">The context in which the resilience operation or event occurred.</param>
    /// <param name="context">The outcome of the resilience operation or event.</param>
    /// <param name="attemptNumber">The zero-based attempt number.</param>
    /// <param name="redeliveryDelay">The delay before the next redelivery.</param>
    /// <param name="duration">The duration of this attempt.</param>
    public OnRedeliverArguments(
        ResilienceContext context, 
        Outcome<TResult> outcome, 
        int attemptNumber, 
        TimeSpan redeliveryDelay, 
        TimeSpan duration)
    {
        Context = context;
        Outcome = outcome;
        AttemptNumber = attemptNumber;
        RedeliveryDelay = redeliveryDelay;
        Duration = duration;
    }

    /// <summary>
    /// Gets the outcome that will be retried.
    /// </summary>
    public Outcome<TResult> Outcome { get; }

    /// <summary>
    /// Gets the context of this event.
    /// </summary>
    public ResilienceContext Context { get; }

    /// <summary>
    /// Gets the zero-based attempt number.
    /// </summary>
    public int AttemptNumber { get; }

    /// <summary>
    /// Gets the delay before the next redelivery.
    /// </summary>
    public TimeSpan RedeliveryDelay { get; }

    /// <summary>
    /// Gets the duration of this attempt.
    /// </summary>
    public TimeSpan Duration { get; }
}