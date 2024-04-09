using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Polly.Telemetry;

namespace Polly.Contrib.ServiceBusRedelivery;

/// <summary>
/// <see cref="ResilienceStrategy{T}"/> that will redeliver the message to the queue/topic in the event of a handled fault
/// </summary>
/// <typeparam name="T">The type of result this strategy handles</typeparam>
internal sealed class RedeliverMessageResilienceStrategy<T> : ResilienceStrategy<T>
{
    private readonly TimeProvider _timeProvider;
    private readonly ResilienceStrategyTelemetry _telemetry;

    public RedeliverMessageResilienceStrategy(
        RedeliverMessageStrategyOptions<T> options,
        TimeProvider timeProvider,
        ResilienceStrategyTelemetry telemetry)
    {
        Message = options.Message;
        Sender = options.Sender;
        Receiver = options.Receiver;
        MaxRedeliverAttempts = options.MaxRedeliverAttempts;
        BackoffType = options.BackoffType;
        LastAttemptFailedAction = options.LastAttemptFailedAction;
        BaseDelay = options.Delay;
        MaxDelay = options.MaxDelay;
        DelayGenerator = options.DelayGenerator;
        OnRedeliver = options.OnRedeliver;
        ShouldHandle = options.ShouldHandle;
        
        _timeProvider = timeProvider;
        _telemetry = telemetry;
    }
    
    public ServiceBusReceivedMessage Message { get; }
    public ServiceBusSender Sender { get; }
    public ServiceBusReceiver Receiver { get; }
    public int MaxRedeliverAttempts { get; }
    public DelayBackoffType BackoffType { get; }
    public MessageAction LastAttemptFailedAction { get; }
    public TimeSpan BaseDelay { get; }
    public TimeSpan? MaxDelay { get; }
    public Func<RedeliveryDelayGeneratorArguments<T>, ValueTask<TimeSpan?>>? DelayGenerator { get; }
    public Func<OnRedeliverArguments<T>, ValueTask>? OnRedeliver { get; }
    public Func<RedeliverMessagePredicateArguments<T>, ValueTask<bool>> ShouldHandle { get; }

    protected override async ValueTask<Outcome<T>> ExecuteCore<TState>(
        Func<ResilienceContext, TState, ValueTask<Outcome<T>>> callback, 
        ResilienceContext context, 
        TState state)
    {
        var attemptNumber = GetCurrentAttemptNumber(Message);
        var startTimestamp = _timeProvider.GetTimestamp();
        var outcome = await callback(context, state).ConfigureAwait(context.ContinueOnCapturedContext);
        var shouldRedeliverArgs = new RedeliverMessagePredicateArguments<T>(context, outcome, attemptNumber);
        var handled = await ShouldHandle(shouldRedeliverArgs).ConfigureAwait(context.ContinueOnCapturedContext);
        var executionTime = _timeProvider.GetElapsedTime(startTimestamp);
        
        _telemetry.Report(
            new ResilienceEvent(handled ? ResilienceEventSeverity.Warning : ResilienceEventSeverity.Information, "ExecutionAttempt"),
            context,
            outcome,
            new ExecutionAttemptArguments(attemptNumber, executionTime, handled));
        
        if (context.CancellationToken.IsCancellationRequested || !handled)
        {
            return outcome;
        }

        if (IsLastAttempt(attemptNumber, out var incrementAttempts))
        {
            try
            {
                switch (LastAttemptFailedAction)
                {
                    case MessageAction.Complete:
                        await Receiver.CompleteMessageAsync(Message).ConfigureAwait(context.ContinueOnCapturedContext);
                        break;
                    case MessageAction.Abandon:
                        await Receiver.AbandonMessageAsync(Message).ConfigureAwait(context.ContinueOnCapturedContext);
                        break;
                    case MessageAction.Defer:
                        await Receiver.DeferMessageAsync(Message).ConfigureAwait(context.ContinueOnCapturedContext);
                        break;
                    case MessageAction.DeadLetter:
                        await Receiver.DeadLetterMessageAsync(Message).ConfigureAwait(context.ContinueOnCapturedContext);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(LastAttemptFailedAction));
                }
            }
            catch (Exception e)
            {
                return Outcome.FromException<T>(e);
            }
            
            return outcome;
        }

        var delay = RedeliveryHelper.GetRedeliveryDelay(
            BackoffType, 
            attemptNumber, 
            BaseDelay,
            MaxDelay);
        
        if (DelayGenerator is not null)
        {
            var delayArgs = new RedeliveryDelayGeneratorArguments<T>(context, outcome, attemptNumber);
            var newDelay = await DelayGenerator(delayArgs).ConfigureAwait(false);

            if (newDelay.HasValue && RedeliveryHelper.IsValidDelay(newDelay.Value))
            {
                delay = newDelay.Value;
            }
        }

        var onRedeliverArgs = new OnRedeliverArguments<T>(context, outcome, attemptNumber, delay, executionTime);
        _telemetry.Report(
            new ResilienceEvent(ResilienceEventSeverity.Warning, RedeliverMessageConstants.OnRedeliverEvent), 
            context,
            outcome,
            onRedeliverArgs);

        if (OnRedeliver is not null)
        {
            await OnRedeliver(onRedeliverArgs).ConfigureAwait(context.ContinueOnCapturedContext);
        }

        try
        {
            await RedeliveryHelper.RedeliverMessageWithDelay(
                Message,
                Sender,
                Receiver,
                delay,
                attemptNumber + (incrementAttempts ? 1 : 0),
                _timeProvider).ConfigureAwait(context.ContinueOnCapturedContext);
        }
        catch (Exception e)
        {
            return Outcome.FromException<T>(e);
        }

        return outcome;
    }
    
    private int GetCurrentAttemptNumber(ServiceBusReceivedMessage message)
    {
        if (message.ApplicationProperties.TryGetValue(RedeliverMessageConstants.AttemptNumberKey, out var attemptNumberObj) && attemptNumberObj is int attemptNumber)
        {
            return attemptNumber;
        }

        return 0;
    }
    
    private bool IsLastAttempt(int attempt, out bool incrementAttempts)
    {
        if (attempt == int.MaxValue)
        {
            incrementAttempts = false;
            return false;
        }

        incrementAttempts = true;
        return attempt >= MaxRedeliverAttempts;
    }
}