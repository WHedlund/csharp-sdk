using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private static readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();

    public static Task RunOnMainThread(Action action, string context = null)
    {
        EnsureInstance();
        var tcs = new TaskCompletionSource<object>();
        _queue.Enqueue(() =>
        {
            action();
            tcs.SetResult(null);
        });
        return tcs.Task;
    }

    public static Task<T> RunOnMainThread<T>(Func<T> func, string context = null)
    {
        EnsureInstance();
        var tcs = new TaskCompletionSource<T>();

        _queue.Enqueue(() =>
        {
            T result = func();
            tcs.SetResult(result);
        });

        return tcs.Task;
    }

    public static Task RunOnMainThread(Func<Task> func, string context = null)
    {
        EnsureInstance();
        var tcs = new TaskCompletionSource<object>();

        _queue.Enqueue(async () =>
        {
            await func();
            tcs.SetResult(null);
        });

        return tcs.Task;
    }

    public static Task<T> RunOnMainThread<T>(Func<Task<T>> func, string context = null)
    {
        EnsureInstance();
        var tcs = new TaskCompletionSource<T>();

        _queue.Enqueue(async () =>
        {
            T result = await func();
            tcs.SetResult(result);
        });

        return tcs.Task;
    }

    private static void EnsureInstance()
    {
        if (_instance == null || !_instance)
        {
            var obj = new GameObject("UnityMainThreadDispatcher");
            _instance = obj.AddComponent<UnityMainThreadDispatcher>();
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

    void Update()
    {
        while (_queue.TryDequeue(out var action))
        {
            action?.Invoke();
        }
    }
}
