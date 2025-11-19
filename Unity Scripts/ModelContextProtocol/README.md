# Unity EmbedIO Extensions for the MCP C# SDK

> **Unstable:** This package tracks the evolving [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) and is subject to breaking changes as the MCP package updates.

## About

This package enables Unity projects to run a fully functional [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) server using [EmbedIO](https://github.com/unosquare/embedio) as a lightweight HTTP server. It’s designed specifically for Unity (including **Unity 6**), since **ASP.NET Core is not fully compatible with the Unity runtime (Mono)**.

**Key features:**

* Native Unity integration for MCP servers.
* Uses EmbedIO for lightweight HTTP/SSE transport.
* Runs the HTTP server in a **separate thread** to avoid blocking the Unity main thread.
* Includes a `MainThreadDispatcher` for safe Unity object interaction (in `Dispatchers/`).
* **Automatic resource discovery**: uses a custom attribute (`McpServerResourceAttribute`) for automatic discovery of tools/resources.
* **UnityMainThreadLogger** for safe logging from background threads.
* Tools and resources are discovered by `McpAgentRouteRegistrar`, which searches the GameObject and its children.
* Supports two transport layers:

  * **SSE** (`/sse`)
  * **Streamable HTTP** (`/`)
* Tested on **Ubuntu** and **Windows** with **Unity 6**.
* Compatible with MCP `0.2.0-preview.1` and EmbedIO `3.5.2`.

> This is a community-driven project and not officially maintained by the MCP core team.

---

## 🚀 Installation & Getting Started

### 1. Add This Package to Your Unity Project

Simply **clone or copy this repository into your project’s `Assets` folder**:

```
Assets/
└── ModelContextProtocol/
    └── EmbedIO/
        ├── AspNetCore/
        ├── Configuration/
        ├── Controllers/
        ├── Dispatchers/
        ├── Helpers/
        ├── Tools/
        ├── UnityRegistrators/
        └── README.md
```

---

### 2. Install Dependencies

Using **[NuGet for Unity](https://github.com/GlitchEnzo/NuGetForUnity)**, install these versions:

* `EmbedIO` **3.5.2**
* `ModelContextProtocol` **0.2.0-preview\.1**
* `ModelContextProtocol.AspNetCore` **0.2.0-preview\.1**
* `Newtonsoft.Json` **13.0.3**

> Make sure to **enable "Show Prerelease Packages"** in NuGet for Unity’s settings to see preview versions.

---

### 3. Add Components to Your Scene

1. **Create a new GameObject** in your Unity scene.
2. **Attach the following components** to this GameObject:

   * `EmbedIOServerHost` (from `UnityRegistrators/`).
   * `McpAgentRouteRegistrar` (also in `UnityRegistrators/`).
   * Any tool scripts you want to expose (like `EchoToolWithInstance` in `Tools/`).

When you press Play, the MCP server starts automatically in a background thread and is available at:

* **SSE endpoint**: `http://localhost:8888/sse`
* **Streamable HTTP endpoint**: `http://localhost:8888/`

You can test your server using the [MCP Inspector tool](https://github.com/modelcontextprotocol/python-sdk/tree/main).

---

## 🧩 Usage & Examples

All tools and resources are automatically discovered by the `McpAgentRouteRegistrar` component. It looks at components on the same GameObject and its children.

Here’s an example of a simple tool script:

```csharp
using UnityEngine;
using ModelContextProtocol.Tools;

public class EchoToolWithInstance : MonoBehaviour
{
    [McpServerTool(Description = "Echoes the input message (instance)")]
    public string Echo(string message)
    {
        return $"Echo: {message}";
    }
}
```

* **Attach this script to the same GameObject** (or a child of it) where you have `EmbedIOServerHost` and `McpAgentRouteRegistrar`.
* The tool’s `[McpServerTool]` methods are automatically exposed to MCP clients when the server starts.
* You can test the endpoints with the [MCP Inspector tool](https://github.com/modelcontextprotocol/python-sdk/tree/main) or any MCP-compatible client.

---

## 🔗 Related Projects & Links

* [ModelContextProtocol C# SDK (NuGet)](https://www.nuget.org/packages/ModelContextProtocol)
* [ModelContextProtocol.AspNetCore (reference)](https://github.com/modelcontextprotocol/csharp-sdk/tree/main/src/ModelContextProtocol.AspNetCore)
* [Official Model Context Protocol Specification](https://spec.modelcontextprotocol.io/)
* [MCP Inspector (Python SDK/CLI tool)](https://github.com/modelcontextprotocol/python-sdk/tree/main)
