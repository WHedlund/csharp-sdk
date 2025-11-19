using UnityEngine;
using System.Collections.Concurrent;
using ModelContextProtocol.Protocol; // <-- USE YOUR PROTOCOL ENUM

/// <summary>
/// Thread-safe logger for Unity that ensures logs from any thread
/// (background or main) are flushed on the Unity main thread,
/// so they always appear in the Console.
/// Log messages are filtered by a configurable minimum LoggingLevel.
/// </summary>
public class MainThreadLogger : MonoBehaviour
{
    private static MainThreadLogger _instance;
    private static readonly ConcurrentQueue<LogItem> logQueue = new();

    /// <summary>
    /// The minimum log level required for messages to be output.
    /// </summary>
    public static LoggingLevel MinimumLevel = LoggingLevel.Info;

    private struct LogItem
    {
        public string Message;
        public LoggingLevel Level;
    }

    /// <summary>
    /// Log a message from any thread, with specified log level.
    /// </summary>
    public static void LogFromAnyThread(string message, LoggingLevel level = LoggingLevel.Info)
    {
        EnsureInstance();
        if (level < MinimumLevel) return;
        logQueue.Enqueue(new LogItem { Message = message, Level = level });
    }

    // Shorthand static helpers (optional, for convenience)
    public static void Log(string message) => LogFromAnyThread(message, LoggingLevel.Info);
    public static void LogDebug(string message) => LogFromAnyThread(message, LoggingLevel.Debug);
    public static void LogNotice(string message) => LogFromAnyThread(message, LoggingLevel.Notice);
    public static void LogWarning(string message) => LogFromAnyThread(message, LoggingLevel.Warning);
    public static void LogError(string message) => LogFromAnyThread(message, LoggingLevel.Error);
    public static void LogCritical(string message) => LogFromAnyThread(message, LoggingLevel.Critical);
    public static void LogAlert(string message) => LogFromAnyThread(message, LoggingLevel.Alert);
    public static void LogEmergency(string message) => LogFromAnyThread(message, LoggingLevel.Emergency);

    /// <summary>
    /// Sets the minimum log level required for messages to be output.
    /// </summary>
    public static void SetMinimumLevel(LoggingLevel level)
    {
        MinimumLevel = level;
        LogFromAnyThread($"[MCP] Logging level changed to {level}", LoggingLevel.Info);
    }

    // Main Unity update loop: flush log queue to Unity's console on main thread
    void Update()
    {
        while (logQueue.TryDequeue(out var logItem))
        {
            switch (logItem.Level)
            {
                case LoggingLevel.Debug:
                    Debug.Log("[Debug] " + logItem.Message);
                    break;
                case LoggingLevel.Info:
                case LoggingLevel.Notice:
                    Debug.Log(logItem.Message);
                    break;
                case LoggingLevel.Warning:
                    Debug.LogWarning(logItem.Message);
                    break;
                case LoggingLevel.Error:
                case LoggingLevel.Critical:
                case LoggingLevel.Alert:
                case LoggingLevel.Emergency:
                    Debug.LogError(logItem.Message);
                    break;
                default:
                    Debug.Log(logItem.Message);
                    break;
            }
        }
    }

    // Ensures a singleton MonoBehaviour instance exists in the scene
    private static void EnsureInstance()
    {
        if (_instance == null || !_instance)
        {
            var obj = new GameObject("MainThreadLogger");
            _instance = obj.AddComponent<MainThreadLogger>();
            DontDestroyOnLoad(obj);
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
}
