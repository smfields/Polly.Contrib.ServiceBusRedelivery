namespace Polly.Contrib.ServiceBusRedelivery;

/// <summary>
/// Actions that can be taken on a message
/// </summary>
public enum MessageAction
{
    /// <summary>
    /// Complete the message
    /// </summary>
    Complete,
    
    /// <summary>
    /// Abandon the message
    /// </summary>
    Abandon,
    
    /// <summary>
    /// Defer the message
    /// </summary>
    Defer,
    
    /// <summary>
    /// Send the message to the dead letter queue
    /// </summary>
    DeadLetter
}