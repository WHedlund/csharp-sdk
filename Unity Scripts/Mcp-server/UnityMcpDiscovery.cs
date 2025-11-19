using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using ModelContextProtocol.Server;
using UnityEngine;

public static class UnityMcpDiscovery
{
    public static IReadOnlyList<McpServerTool> DiscoverToolsInHierarchy(GameObject root, string idPrefix = null)
    {
        if (root == null) throw new ArgumentNullException(nameof(root));

        var tools = new List<McpServerTool>();
        var components = root.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);

        foreach (var component in components)
        {
            var type = component.GetType();
            var methods = type.GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            foreach (var method in methods)
            {
                var toolAttr = method.GetCustomAttribute<McpServerToolAttribute>();
                if (toolAttr == null)
                    continue;

                try
                {
                    var descriptionAttr = method.GetCustomAttribute<DescriptionAttribute>();
                    string description = descriptionAttr?.Description ?? $"Tool on {type.Name}.{method.Name}";

                    string baseName = toolAttr.Name ?? method.Name;
                    string finalName = string.IsNullOrEmpty(idPrefix)
                        ? baseName
                        : idPrefix + "." + baseName;

                    var options = new McpServerToolCreateOptions
                    {
                        Name = finalName,
                        Description = description
                    };

                    var tool = McpServerTool.Create(method, component, options);
                    tools.Add(tool);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to create MCP tool for {type.Name}.{method.Name} on {component.name}: {ex}");
                }
            }
        }

        return tools;
    }

    public static IReadOnlyList<McpServerResource> DiscoverResourcesInHierarchy(GameObject root, string idPrefix = null)
    {
        if (root == null) throw new ArgumentNullException(nameof(root));

        var resources = new List<McpServerResource>();
        var components = root.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);

        foreach (var component in components)
        {
            var type = component.GetType();
            var methods = type.GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            foreach (var method in methods)
            {
                var resAttr = method.GetCustomAttribute<McpServerResourceAttribute>();
                if (resAttr == null)
                    continue;

                try
                {
                    var descriptionAttr = method.GetCustomAttribute<DescriptionAttribute>();
                    string description = descriptionAttr?.Description ?? $"Resource on {type.Name}.{method.Name}";

                    string baseName = resAttr.Name ?? method.Name;
                    string finalName = string.IsNullOrEmpty(idPrefix)
                        ? baseName
                        : idPrefix + "." + baseName;

                    string uriTemplate = string.IsNullOrEmpty(resAttr.UriTemplate)
                        ? $"unity://{finalName}"
                        : resAttr.UriTemplate;

                    string mimeType = string.IsNullOrEmpty(resAttr.MimeType)
                        ? "text/plain"
                        : resAttr.MimeType;

                    var options = new McpServerResourceCreateOptions
                    {
                        Name = finalName,
                        UriTemplate = uriTemplate,
                        MimeType = mimeType,
                        Description = description
                    };

                    // Basic pattern: parameterless method returning string
                    // Assumes resources never needs parameters
                    Func<string> func = () =>
                    {
                        var result = method.Invoke(component, null);
                        return result?.ToString() ?? string.Empty;
                    };

                    var resource = McpServerResource.Create(func, options);
                    resources.Add(resource);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to create MCP resource for {type.Name}.{method.Name} on {component.name}: {ex}");
                }
            }
        }

        return resources;
    }

    public static IReadOnlyList<McpServerPrompt> DiscoverPromptsInHierarchy(GameObject root, string idPrefix = null)
    {
        if (root == null) throw new ArgumentNullException(nameof(root));

        var prompts = new List<McpServerPrompt>();
        var components = root.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);

        foreach (var component in components)
        {
            var type = component.GetType();
            var methods = type.GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            foreach (var method in methods)
            {
                var promptAttr = method.GetCustomAttribute<McpServerPromptAttribute>();
                if (promptAttr == null)
                    continue;

                // For now require parameterless methods
                if (method.GetParameters().Length != 0)
                {
                    Debug.LogWarning($"Skipping prompt {type.Name}.{method.Name} on {component.name} because it has parameters. We currently only support parameterless prompts.");
                    continue;
                }

                try
                {
                    throw new NotImplementedException("MCP Prompt creation from Unity components is not yet implemented.");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to create MCP prompt definition for {type.Name}.{method.Name} on {component.name}: {ex}");
                }
            }
        }

        return prompts;
    }

}