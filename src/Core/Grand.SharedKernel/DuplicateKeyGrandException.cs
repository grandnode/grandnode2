namespace Grand.SharedKernel;

/// <summary>
///     Represents a write rejected by the store because it violates a unique index
/// </summary>
/// <remarks>
///     Thrown by the repository implementations so that callers can react to a unique key collision
///     without referencing a database driver.
/// </remarks>
[Serializable]
public class DuplicateKeyGrandException : GrandException
{
    /// <summary>
    ///     Initializes a new instance of the DuplicateKeyGrandException class.
    /// </summary>
    public DuplicateKeyGrandException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the DuplicateKeyGrandException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DuplicateKeyGrandException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the DuplicateKeyGrandException class with a specified error message
    ///     and the driver exception that caused it.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public DuplicateKeyGrandException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
