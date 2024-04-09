namespace Polly.Contrib.ServiceBusRedelivery;

/// <summary>
/// Represents the arguments used by <see cref="RedeliverMessageStrategyOptions{TResult}.DelayGenerator"/> for generating the next redelivery delay.
/// </summary>
/// <typeparam name="TResult">The type of result.</typeparam>
/// <remarks>
/// Always use the constructor when creating this struct, otherwise we do not guarantee binary compatibility.
/// </remarks>
public readonly struct RedeliveryDelayGeneratorArguments<TResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RedeliveryDelayGeneratorArguments{TResult}"/> struct.
    /// </summary>
    /// <param name="outcome">The context in which the resilience operation or event occurred.</param>
    /// <param name="context">The outcome of the resilience operation or event.</param>
    /// <param name="attemptNumber">The zero-based attempt number.</param>
    public RedeliveryDelayGeneratorArguments(ResilienceContext context, Outcome<TResult> outcome, int attemptNumber)
    {
        Context = context;
        Outcome = outcome;
        AttemptNumber = attemptNumber;
    }

    /// <summary>
    /// Gets the outcome of the resilience operation or event.
    /// </summary>
    public Outcome<TResult> Outcome { get; }

    /// <summary>
    /// Gets the context in which the resilience operation or event occurred.
    /// </summary>
    public ResilienceContext Context { get; }

    /// <summary>
    /// Gets The zero-based attempt number.
    /// </summary>
    public int AttemptNumber { get; }
}