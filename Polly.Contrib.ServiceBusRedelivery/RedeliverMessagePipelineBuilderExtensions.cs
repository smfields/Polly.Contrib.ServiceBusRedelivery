using System;
using System.ComponentModel.DataAnnotations;

namespace Polly.Contrib.ServiceBusRedelivery;

/// <summary>
/// Extensions for adding redelivery to <see cref="ResiliencePipelineBuilder"/>.
/// </summary>
public static class RedeliverMessagePipelineBuilderExtensions
{
    /// <summary>
    /// Adds message redelivery to the builder.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="options">The redelivery options.</param>
    /// <returns>The builder instance with the redelivery strategy added.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="options"/> is <see langword="null"/></exception>
    /// <exception cref="ValidationException">Thrown when <paramref name="options"/> are invalid.</exception>
    public static ResiliencePipelineBuilder AddMessageRedelivery(
        this ResiliencePipelineBuilder builder,
        RedeliverMessageStrategyOptions options)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        return builder.AddStrategy(
            context => new RedeliverMessageResilienceStrategy<object>(options, context.TimeProvider, context.Telemetry),
            options);
    }
    
    /// <summary>
    /// Adds message redelivery to the builder.
    /// </summary>
    /// <typeparam name="TResult">The type of result the redelivery handles.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="options">The redelivery options.</param>
    /// <returns>The builder instance with the redelivery strategy added.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="options"/> is <see langword="null"/></exception>
    /// <exception cref="ValidationException">Thrown when <paramref name="options"/> are invalid.</exception>
    public static ResiliencePipelineBuilder<TResult> AddMessageRedelivery<TResult>(
        this ResiliencePipelineBuilder<TResult> builder,
        RedeliverMessageStrategyOptions<TResult> options)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        return builder.AddStrategy(
            context => new RedeliverMessageResilienceStrategy<TResult>(options, context.TimeProvider, context.Telemetry), 
            options);
    }
}