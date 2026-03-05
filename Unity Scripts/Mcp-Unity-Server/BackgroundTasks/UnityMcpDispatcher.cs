using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Lightweight main-thread dispatcher used to run actions posted from background threads in Unity.
/// </summary>
public class UnityMcpDispatcher : MonoBehaviour
{
    static UnityMcpDispatcher instance;

    readonly ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        while (queue.TryDequeue(out var action))
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Debug.LogError("Exception in UnityMcpDispatcher action: " + ex);
            }
        }
    }

    public static Task Run(Action action)
    {
        EnsureInstance();

        var tcs = new TaskCompletionSource<object>();

        instance.queue.Enqueue(() =>
        {
            try
            {
                action();
                tcs.SetResult(null);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return tcs.Task;
    }

    public static Task<T> Run<T>(Func<T> func)
    {
        EnsureInstance();

        var tcs = new TaskCompletionSource<T>();

        instance.queue.Enqueue(() =>
        {
            try
            {
                var result = func();
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return tcs.Task;
    }

    public static Task RunAsync(Func<Task> func)
    {
        EnsureInstance();

        var tcs = new TaskCompletionSource<object>();

        instance.queue.Enqueue(() =>
        {
            _ = RunAsyncInternal(func, tcs);
        });

        return tcs.Task;
    }

    public static Task<T> RunAsync<T>(Func<Task<T>> func)
    {
        EnsureInstance();

        var tcs = new TaskCompletionSource<T>();

        instance.queue.Enqueue(() =>
        {
            _ = RunAsyncInternal(func, tcs);
        });

        return tcs.Task;
    }

    static async Task RunAsyncInternal(Func<Task> func, TaskCompletionSource<object> tcs)
    {
        try
        {
            await func().ConfigureAwait(false);
            tcs.SetResult(null);
        }
        catch (Exception ex)
        {
            tcs.SetException(ex);
        }
    }

    static async Task RunAsyncInternal<T>(Func<Task<T>> func, TaskCompletionSource<T> tcs)
    {
        try
        {
            var result = await func().ConfigureAwait(false);
            tcs.SetResult(result);
        }
        catch (Exception ex)
        {
            tcs.SetException(ex);
        }
    }

    static void EnsureInstance()
    {
        if (instance != null)
            return;

        var go = new GameObject("UnityMcpDispatcher");
        instance = go.AddComponent<UnityMcpDispatcher>();
        DontDestroyOnLoad(go);
    }
}
