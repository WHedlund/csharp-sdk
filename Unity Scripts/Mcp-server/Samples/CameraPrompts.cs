using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using UnityEngine;

public class CameraPrompts : MonoBehaviour
{
    [McpServerPrompt(Name = "DescribeCameraView")]
    [Description("Prompt the model to describe what it sees from this camera.")]
    public ChatMessage BuildDescribePrompt()
    {
        return new ChatMessage(
        ChatRole.User,
        "Describe what is visible from the current camera view in a concise way.");
    }
}