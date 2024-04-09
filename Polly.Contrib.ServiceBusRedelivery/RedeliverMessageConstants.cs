using System;

namespace Polly.Contrib.ServiceBusRedelivery;

internal static class RedeliverMessageConstants
{
    public const string DefaultName = "RedeliverMessage";

    public const string OnRedeliverEvent = "OnRedeliver";

    public const DelayBackoffType DefaultBackoffType = DelayBackoffType.Constant;

    public const MessageAction DefaultLastAttemptFailedAction = MessageAction.DeadLetter;

    public const int DefaultRedeliverCount = 5;

    public const int MaxRedeliverCount = int.MaxValue;

    public static readonly TimeSpan DefaultBaseDelay = TimeSpan.FromSeconds(30);

    public const string AttemptNumberKey = "AttemptNumber";
}