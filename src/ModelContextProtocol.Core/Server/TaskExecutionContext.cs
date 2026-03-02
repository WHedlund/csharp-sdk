namespace ModelContextProtocol.Server;

/// <summary>
/// Represents the execution context for a task being executed by the server.
/// This context flows with async execution and enables automatic task status updates.
/// </summary>
internal sealed class TaskExecutionContext
{
    /// <summary>
    /// Gets the AsyncLocal instance used to track the current task execution context.
    /// </summary>
    private static readonly AsyncLocal<TaskExecutionContext?> s_current = new();

    /// <summary>
    /// Gets or sets the current task execution context for the executing async flow.
    /// </summary>
    public static TaskExecutionContext? Current
    {
        get => s_current.Value;
        set => s_current.Value = value;
    }

    /// <summary>
    /// Gets the task ID of the currently executing task.
    /// </summary>
    public string TaskId { get; set; }

    /// <summary>
    /// Gets the session ID associated with the task.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Gets the task store used to persist task state.
    /// </summary>
    public IMcpTaskStore TaskStore { get; set; }

    /// <summary>
    /// Gets whether task status notifications should be sent.
    /// </summary>
    public bool SendNotifications { get; set; }

    /// <summary>
    /// Gets or sets the function to call when sending a task status notification.
    /// </summary>
    public Func<Protocol.McpTask, CancellationToken, Task>? NotifyTaskStatusFunc { get; set; }
}
