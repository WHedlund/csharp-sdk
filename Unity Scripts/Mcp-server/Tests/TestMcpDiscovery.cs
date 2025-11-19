using System.Collections.Generic;
using ModelContextProtocol.Server;
using UnityEngine;

public class TestMcpDiscovery : MonoBehaviour
{
    public McpObjectDefinitionProvider provider;

    void Start()
    {
        if (provider == null)
        {
            Debug.LogError("Please assign a McpObjectDefinitionProvider in the inspector.");
            return;
        }

        var tools = provider.GetTools();
        var resources = provider.GetResources();
        var prompts = provider.GetPrompts();

        Debug.Log("===== OBJECT MCP DISCOVERY TEST =====");
        Debug.Log("Provider: " + provider.name);

        Debug.Log("Tools found: " + tools.Count);
        foreach (var tool in tools)
        {
            Debug.Log("Tool: " + tool.ToString() + " / InputSchema: " + tool.ProtocolTool.InputSchema);
        }

        Debug.Log("Resources found: " + resources.Count);
        foreach (var res in resources)
        {
            Debug.Log("Resource: " + res.ToString());
            //Debug.Log("Resource: " + res.ProtocolResource.Name + " / UriTemplate: " + res.ProtocolResource.Uri + " / MimeType: " + res.ProtocolResource.MimeType);
        }

        Debug.Log("Prompts found: " + prompts.Count);
        foreach (var pr in prompts)
        {
            Debug.Log("Prompt: " + pr.ProtocolPrompt.Name + " / Title: " + pr.ProtocolPrompt.Title);
        }

        Debug.Log("===== END TEST =====");
    }


}
