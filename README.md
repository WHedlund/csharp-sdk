# Building MCP for Unity (netstandard2.1 Compatibility)

This document describes how to build a Unity‑compatible version of the ModelContextProtocol (MCP) libraries. It covers feature downgrades, dependency handling, and the final workflow to produce a complete set of DLLs that work inside Unity.

**See `Unity Scripts/Mcp-library` and `Unity Scripts/Mcp-server` for compiled library and unity scripts including Http Transport** 

---

## Overview

Unity currently supports a subset of modern C# and .NET features. The original MCP SDK uses features not available in Unity's runtime (for example, `required` members and advanced compiler features). To make the SDK compatible, a downgrade process is required.

This document outlines the full workflow used to:

* Detect and remove incompatible language features
* Add a `netstandard2.1` build target
* Relax warnings and nullability for Unity builds
* Ensure all dependencies are copied into the output directory
* Package the results for Unity

All final Unity‑ready builds are located in:

```
artifacts/bin/ModelContextProtocol/Release/netstandard2.1/
artifacts/bin/ModelContextProtocol.Core/Release/netstandard2.1/
```

Copy all DLLs except System.Threading.Tasks.Extensions from these directories into your Unity project.

---

# 1. Downgrade Incompatible Language Features

A script was created to scan for incompatible features (primarily `required` and `init`). Running the downgrade script updates affected files to use Unity‑compatible C# patterns.

Example steps:

* Create downgrade script
* Run `downgradeScript.ps1`
* Verify all incompatible language features are removed

## Update project targets
Update build targets from netstandard 2.0 to 2.1
This is done in this repository `./Directory.Build.props`, `./src/ModelContextProtocol/ModelContextProtocol.csproj`, `./src/ModelContextProtocol.Core/ModelContextProtocol.Core.csproj`

## Netstandard 2.1 implementations
Some things from `\src\Common\Polyfills\System\Collections\Generic\CollectionExtensions.cs` are implemented by netstandard 2.1 so we modify/remove some paths to ignored these. including CollectionExtensions. Modifications are made in: 
`./src/ModelContextProtocol/ModelContextProtocol.csproj`
```
<ItemGroup Condition="$([MSBuild]::IsTargetFrameworkCompatible('$(TargetFramework)', 'netstandard2.1'))">
  <Compile Remove="..\Common\Polyfills\System\Collections\Generic\CollectionExtensions.cs" />
</ItemGroup>
```
`./src/ModelContextProtocol.Core/ModelContextProtocol.Core.csproj` 
```
<ItemGroup Condition="$([MSBuild]::IsTargetFrameworkCompatible('$(TargetFramework)', 'netstandard2.1'))">
  <Compile Remove="..\Common\Polyfills\System\Diagnostics\CodeAnalysis\NullableAttributes.cs" />
</ItemGroup>
```
`\src\Common\Polyfills\System\Collections\Generic\CollectionExtensions.cs`
```
#if NETSTANDARD2_1
namespace System.Collections.Generic;

internal static class CollectionExtensions
{
    // netstandard2.1 already has GetValueOrDefault; we only need the aggregator overload
    public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source) =>
        System.Linq.Enumerable.ToDictionary(source, kv => kv.Key, kv => kv.Value);
}
#elif !NET```
# 2. Build Commands for Unity

Use the following commands to produce Unity‑compatible netstandard2.1 builds. These commands relax warnings, disable nullable enforcement, and ensure all dependencies are copied into the output directory.

## Build
```
dotnet build src/ModelContextProtocol/ModelContextProtocol.csproj \
  -c Release \
  -p:TargetFramework=netstandard2.1 \
  -p:CopyLocalLockFileAssemblies=true \
  -p:TreatWarningsAsErrors=false \
  -p:NoWarn=9999

```

---

# 3. Output Locations

Final Unity‑ready DLLs (netstandard2.1) appear in:

```
artifacts/bin/ModelContextProtocol.Core/Release/netstandard2.1/
artifacts/bin/ModelContextProtocol/Release/netstandard2.1/
```

Copy **all** files from both directories into (for example):

```
Assets/Plugins/MCP/
```

Unity will automatically import and compile the libraries.

---

# 4. Notes

* The ASP.NET server components do not run inside Unity.
* The Unity build uses downgraded compiler features and may differ from the server version.

---

## End of Document

# MCP C# SDK

[![NuGet version](https://img.shields.io/nuget/v/ModelContextProtocol.svg)](https://www.nuget.org/packages/ModelContextProtocol)

The official C# SDK for the [Model Context Protocol](https://modelcontextprotocol.io/), enabling .NET applications, services, and libraries to implement and interact with MCP clients and servers. Please visit the [API documentation](https://modelcontextprotocol.github.io/csharp-sdk/api/ModelContextProtocol.html) for more details on available functionality.

## Packages

This SDK consists of three main packages:

- **[ModelContextProtocol.Core](https://www.nuget.org/packages/ModelContextProtocol.Core)** [![NuGet version](https://img.shields.io/nuget/v/ModelContextProtocol.Core.svg)](https://www.nuget.org/packages/ModelContextProtocol.Core) - For projects that only need to use the client or low-level server APIs and want the minimum number of dependencies.

- **[ModelContextProtocol](https://www.nuget.org/packages/ModelContextProtocol)** [![NuGet version](https://img.shields.io/nuget/v/ModelContextProtocol.svg)](https://www.nuget.org/packages/ModelContextProtocol) - The main package with hosting and dependency injection extensions. References `ModelContextProtocol.Core`. This is the right fit for most projects that don't need HTTP server capabilities.

- **[ModelContextProtocol.AspNetCore](https://www.nuget.org/packages/ModelContextProtocol.AspNetCore)** [![NuGet version](https://img.shields.io/nuget/v/ModelContextProtocol.AspNetCore.svg)](https://www.nuget.org/packages/ModelContextProtocol.AspNetCore) - The library for HTTP-based MCP servers. References `ModelContextProtocol`.

## Getting Started

To get started, see the [Getting Started](https://modelcontextprotocol.github.io/csharp-sdk/concepts/getting-started.html) guide in the conceptual documentation for installation instructions, package-selection guidance, and complete examples for both clients and servers.

You can also browse the [samples](samples) directory and the [API documentation](https://modelcontextprotocol.github.io/csharp-sdk/api/ModelContextProtocol.html) for more details on available functionality.

## About MCP

The Model Context Protocol (MCP) is an open protocol that standardizes how applications provide context to Large Language Models (LLMs). It enables secure integration between LLMs and various data sources and tools.

For more information about MCP:

- [Official MCP Documentation](https://modelcontextprotocol.io/)
- [MCP C# SDK Documentation](https://modelcontextprotocol.github.io/csharp-sdk/)
- [Protocol Specification](https://modelcontextprotocol.io/specification/)
- [GitHub Organization](https://github.com/modelcontextprotocol)

## License

This project is licensed under the [Apache License 2.0](LICENSE).
