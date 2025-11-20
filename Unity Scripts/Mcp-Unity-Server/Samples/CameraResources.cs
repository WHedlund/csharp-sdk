using System.ComponentModel;
using ModelContextProtocol.Server;
using UnityEngine;

public class CameraResources : MonoBehaviour
{
    [McpServerResource(
    Name = "CameraUsageGuidelines",
    MimeType = "text/markdown")]
    [Description("Instructions for how the AI should use this camera.")]
    public string GetGuidelines()
    {
        return
        "Camere Resource Text: Use this camera only for debugging. " +
        "Do not assume world scale; ask the user for context when needed.";
    }

    [McpServerResource(
    Name = "CameraUsageGuidelinesTemplate",
    UriTemplate = "unity://camera/{id}/guidelines",
    MimeType = "text/markdown")]
    [Description("Instructions for how the AI should use this camera.")]
    public string GetGuidelinesWithTemplate(string id = "123")
    {
        Debug.Log($"GetGuidelinesWithTemplate called with id: {id}");
        return
        "Camere Resource Text: Use this camera only for debugging. " +
        "Do not assume world scale; ask the user for context when needed.";
    }
}