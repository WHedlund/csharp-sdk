using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using UnityEngine;

public class CameraResources : MonoBehaviour
{
    [McpServerResource(
    Name = "CameraUsageGuidelines",
    MimeType = "text/markdown")]
    [Description("Instructions for how the AI should use this camera.")]
    public TextResourceContents GetGuidelines()
    {
        return new TextResourceContents
        {
            Text =
            "Camere Resource Text: Use this camera only for debugging. " +
            "Do not assume world scale; ask the user for context when needed.",
            MimeType = "text/markdown"
        };
    }

    [McpServerResource(
    Name = "CameraUsageGuidelinesTemplate",
    UriTemplate = "unity://camera/{id}/guidelines",
    MimeType = "text/markdown")]
    [Description("Instructions for how the AI should use this camera.")]
    public TextResourceContents GetGuidelinesWithTemplate(string id = "123")
    {
        Debug.Log($"GetGuidelinesWithTemplate called with id: {id}");
        return new TextResourceContents
        {
            Text =
            "Camere Resource Text: Use this camera only for debugging. " +
            "Do not assume world scale; ask the user for context when needed.",
            MimeType = "text/markdown"
        };
    }
}
