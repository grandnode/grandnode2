namespace Grand.SharedKernel;

/// <summary>
///     Represents errors that occur during application execution
/// </summary>
[Serializable]
public class GrandException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the Exception class.
    /// </summary>
    public GrandException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the Exception class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public GrandException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the Exception class with a specified error message and the exception
    ///     that caused it.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public GrandException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}