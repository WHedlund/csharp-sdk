using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using ModelContextProtocol;
using Newtonsoft.Json;

public static class MCPCapabilityCreator
{
    public static ToolsCapability CreateToolsCapability(List<McpServerTool> tools, Action<string> logFunction = null)
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        return new ToolsCapability
        {
            ListToolsHandler = (ctx, ct) =>
            {
                var result = new ListToolsResult
                {
                    Tools = tools.ConvertAll(t => t.ProtocolTool)
                };
                return new ValueTask<ListToolsResult>(result);
            },

            CallToolHandler = (ctx, ct) =>
            {
                var targetTool = tools.Find(t => t.ProtocolTool.Name == ctx.Params?.Name)
                    ?? throw new McpException($"Unknown tool: '{ctx.Params?.Name}'", McpErrorCode.InvalidParams);

                var task = UnityMainThreadDispatcher.RunOnMainThread(
                    async () =>
                    {
                        var response = await targetTool.InvokeAsync(ctx, ct);

                        var toolName = ctx.Params?.Name ?? "(unknown)";
                        var errorText = response.Content?.FirstOrDefault() is TextContentBlock textBlock
                                        ? textBlock.Text
                                        : "(no error text)";
                        var argsJson = ctx.Params?.Arguments is { Count: > 0 } argsDict
                            ? JsonConvert.SerializeObject(argsDict.ToDictionary(
                                kvp => kvp.Key,
                                kvp => {
                                    try { return JsonConvert.DeserializeObject(kvp.Value.GetRawText()); }
                                    catch { return $"(unreadable: {kvp.Value.ValueKind})"; }
                                }), Formatting.None)
                            : "(no arguments)";
                        if (logFunction != null)
                            logFunction($"[Tool Call] '{toolName}' | Args: {argsJson} | Error: {errorText}");
                        if (response.IsError == true)
                            MainThreadLogger.LogError($"[Tool Error] '{toolName}' failed | Args: {argsJson} | Error: {errorText}");
                        return response;
                    },
                    context: $"CallToolHandler: {ctx.Params?.Name}"
                );

                return new ValueTask<CallToolResult>(task);
            }
        };
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }

    /// <summary>
    /// Sets up logging capability that allows the client to dynamically change log levels.
    /// </summary>
    public static LoggingCapability CreateLoggingCapability()
    {
        return new LoggingCapability
        {
            SetLoggingLevelHandler = async (ctx, ct) =>
            {
                var requestedLevel = ctx.Params?.Level;
                if (requestedLevel == null)
                    throw new McpException("Missing required argument 'level'", McpErrorCode.InvalidParams);

                MainThreadLogger.SetMinimumLevel(requestedLevel.Value);

                await ctx.Server.SendNotificationAsync("notifications/message", new
                {
                    Level = "info",
                    Logger = "unity-server",
                    Data = $"Logging level set to {requestedLevel.Value}"
                }, cancellationToken: ct);

                return new EmptyResult();
            }
        };
    }

    public static ResourcesCapability CreateResourceCapability(
        ConcurrentDictionary<string, bool> subscriptions, List<Tuple<object, MethodInfo, Resource>> resources)
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        // 1. List available resource templates (patterns)
        // Kan generera 'context'  s� som vi gjort f�rr men p� f�fr�gan av python
        async ValueTask<ListResourceTemplatesResult> ListResourceTemplates(
            RequestContext<ListResourceTemplatesRequestParams> ctx, CancellationToken ct)
        {
            return new ListResourceTemplatesResult
            {
                ResourceTemplates = new List<ResourceTemplate>
                {
                    new ResourceTemplate
                    {
                        Name = "CameraSnapshot",
                        Description = "Grab a snapshot from the front facing camera.",
                        UriTemplate = "resource://snapshot"
                    }
                }
            };
        }

        // 2. List concrete resources
        async ValueTask<ListResourcesResult> ListResources(
            RequestContext<ListResourcesRequestParams> ctx, CancellationToken ct)
        {
            List<Resource> allResources = resources.Select(t => t.Item3).ToList();
            return new ListResourcesResult { Resources = allResources };
        }

        // 3. Read a resource value (returns dynamic value)
        async ValueTask<ReadResourceResult> ReadResource(
            RequestContext<ReadResourceRequestParams> ctx, CancellationToken ct)
        {
            var uri = ctx.Params?.Uri ?? throw new McpException("Missing required argument 'uri'", McpErrorCode.InvalidParams);

            var selected = resources.FirstOrDefault(r => r.Item3.Uri == uri);
            if (selected == null)
                throw new McpException($"Resource not found: '{uri}'", McpErrorCode.InvalidParams);

            object result = await UnityMainThreadDispatcher.RunOnMainThread(() =>
                selected.Item2.Invoke(selected.Item1, null));

            string mime = selected.Item3.MimeType ?? "application/octet-stream";

            ResourceContents content = mime switch
            {
                "image/png" => new BlobResourceContents
                {
                    Blob = result as string,
                    MimeType = mime,
                    Uri = uri
                },
                "text/plain" or "application/json" => new TextResourceContents
                {
                    Text = result as string,
                    MimeType = mime,
                    Uri = uri
                },
                _ => throw new NotSupportedException($"Unsupported MIME type: {mime}")
            };

            return new ReadResourceResult
            {
                Contents = new List<ResourceContents> { content }
            };
        }

        // 4. Subscribe handler
        async ValueTask<EmptyResult> Subscribe(
        RequestContext<SubscribeRequestParams> ctx, CancellationToken ct)
        {
            if (ctx.Params?.Uri is null)
                throw new McpException("Missing required argument 'uri'", McpErrorCode.InvalidParams);

            if (ctx.Params?.Uri is { } uri)
                subscriptions[uri] = true;
            return new EmptyResult();
        }

        // 5. Unsubscribe handler
        async ValueTask<EmptyResult> Unsubscribe(
            RequestContext<UnsubscribeRequestParams> ctx, CancellationToken ct)
        {
            if (ctx.Params?.Uri is null)
                throw new McpException("Missing required argument 'uri'", McpErrorCode.InvalidParams);

            if (ctx.Params?.Uri is { } uri)
                subscriptions.TryRemove(uri, out _);
            return new EmptyResult();
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        var resourcesCapability = new ResourcesCapability
        {
            Subscribe = true, // enables subscriptions
            ListChanged = false, // change to true if you want to send notifications when resource list changes
            ListResourceTemplatesHandler = ListResourceTemplates,
            ListResourcesHandler = ListResources,
            ReadResourceHandler = ReadResource,
            SubscribeToResourcesHandler = Subscribe,
            UnsubscribeFromResourcesHandler = Unsubscribe,
        };
        return resourcesCapability;
    }
    public static Task StartResourcePushLoop(
    IMcpServer mcpServer,
    ConcurrentDictionary<string, bool> subscriptions,
    CancellationToken token)
    {
        return Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                foreach (var uri in subscriptions.Keys.ToList())
                {
                    var value = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                    await mcpServer.SendNotificationAsync(
                        NotificationMethods.ResourceUpdatedNotification,
                        new
                        {
                            Uri = uri,
                            Contents = new[]
                            {
                            new TextResourceContents
                            {
                                Text = $"[Push] Value for {uri}: {value}",
                                MimeType = "text/plain",
                                Uri = uri
                            }
                            }
                        },
                        cancellationToken: token
                    );
                }

                await Task.Delay(TimeSpan.FromSeconds(2), token);
            }
        }, token);
    }

}
