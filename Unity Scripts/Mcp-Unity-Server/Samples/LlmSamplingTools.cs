using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using UnityEngine;

public class LlmSamplingTools : MonoBehaviour
{
    [McpServerTool, Description("Samples from an LLM using MCP's sampling feature")]
    public async Task<string> SampleLLM(
        McpServer server,
        [Description("The prompt to send to the LLM")] string prompt,
        [Description("Maximum number of tokens to generate")] int maxTokens,
        CancellationToken cancellationToken)
    {
        var request = new CreateMessageRequestParams
        {
            Messages = new List<SamplingMessage>
            {
                new SamplingMessage
                {
                    Role = ModelContextProtocol.Protocol.Role.User,
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = prompt }
                    }
                }
            },
            MaxTokens = maxTokens,
            Temperature = 0.7f
        };

        try
        {
            var result = await server.SampleAsync(request, cancellationToken).ConfigureAwait(false);
            var text = result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text ?? "Empty";
            Debug.LogWarning($"[MCP] SampleLLM result: {result.Content.Count} content blocks");
            Debug.LogWarning($"[MCP] SampleLLM text: {text}");
            return text;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[MCP FATAL] {ex}");
            return $"Error: {ex.Message}";
        }
    }
}
