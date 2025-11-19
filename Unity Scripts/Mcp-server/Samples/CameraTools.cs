using System.ComponentModel;
using ModelContextProtocol.Server;
using UnityEngine;

public class CameraTools : MonoBehaviour
{
    [McpServerTool, Description("Grabs a frame from this camera and returns it as a png or jpeg, base64 encoded.")]
    public string CaptureFrame(
        //[Description("Image format, e.g. png or jpeg. Defaults to png.")] string format = "png"
        )
    {
        // This is just an example stub; you will fill in real logic later.
        Debug.Log($"CaptureFrame called on {name}"); // with format {format}");

        // TODO: capture from this camera, encode to base64 and return.
        return "not-implemented";
    }

    [McpServerTool, Description("Moves this camera to a new world position.")]
    public void MoveCamera(
        [Description("X coordinate")] float x,
        [Description("Y coordinate")] float y,
        [Description("Z coordinate")] float z)
    {
        transform.position = new Vector3(x, y, z);
    }
}
