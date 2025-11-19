using System.Collections.Generic;
using ModelContextProtocol.Server;
using UnityEngine;

public class McpObjectDefinitionProvider : MonoBehaviour
{
    [Tooltip("Logical id of this object for namespacing tools/resources/prompts.")]
    public string objectId = "gameobject";

    private IReadOnlyList<McpServerTool> tools;
    private IReadOnlyList<McpServerResource> resources;
    private IReadOnlyList<McpServerPrompt> prompts;

    public IReadOnlyList<McpServerTool> GetTools()
    {
        EnsureDiscovered();
        return tools;
    }

    public IReadOnlyList<McpServerResource> GetResources()
    {
        EnsureDiscovered();
        return resources;
    }

    public IReadOnlyList<McpServerPrompt> GetPrompts()
    {
        EnsureDiscovered();
        return prompts;
    }

    private void EnsureDiscovered()
    {
        if (tools != null && resources != null && prompts != null)
            return;

        string prefix = string.IsNullOrWhiteSpace(objectId) ? name : objectId;

        tools = UnityMcpDiscovery.DiscoverToolsInHierarchy(gameObject, prefix);
        resources = UnityMcpDiscovery.DiscoverResourcesInHierarchy(gameObject, prefix);
        prompts = UnityMcpDiscovery.DiscoverPromptsInHierarchy(gameObject, prefix);
    }

    private void OnValidate()
    {
        tools = null;
        resources = null;
        prompts = null;
    }

    private void OnTransformChildrenChanged()
    {
        tools = null;
        resources = null;
        prompts = null;
    }


}