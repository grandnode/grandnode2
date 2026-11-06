namespace Grand.Data;

/// <summary>
///     A key stayed held by another caller for the whole wait; unlike a database timeout, nothing failed
/// </summary>
public class ResourceLockTimeoutException(string key)
    : TimeoutException($"The resource '{key}' is in use by another operation");
