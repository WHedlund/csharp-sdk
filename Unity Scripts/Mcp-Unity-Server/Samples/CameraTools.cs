using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using UnityEngine;

public class CameraTools : MonoBehaviour
{
    [McpServerTool, Description("Grabs a frame from this camera and returns it as an image content block.")]
    public ImageContentBlock CaptureFrame(
        //[Description("Image format, e.g. png or jpeg. Defaults to png.")] string format = "png"
        )
    {
        // This is just an example stub; you will fill in real logic later.
        Debug.Log($"CaptureFrame called on {name}"); // with format {format}");

        // TODO: capture from this camera and return the real bytes.
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.black);
        texture.Apply();

        var bytes = texture.EncodeToPNG();
        Destroy(texture);

        return ImageContentBlock.FromBytes(bytes, "image/png");
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
