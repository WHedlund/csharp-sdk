using System;
using ModelContextProtocol.Protocol;

#nullable enable



/* In a newer update ModelContextProtocol.Server added an internal definitino for Resource Attributes. We have as of 0.2.0-preview.1 not adapted this.*/



/// <summary>
/// Marks a class as a container for MCP server resources.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class McpServerResourceTypeAttribute : Attribute
{
}

/// <summary>
/// Marks a method as exposing a resource to the MCP server. 
/// This attribute wraps a <see cref="Resource"/> definition and is used at runtime to register the resource.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class McpServerResourceAttribute : Attribute
{
    /// <summary>
    /// Gets the resource definition associated with this method.
    /// </summary>
    public Resource Resource { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="McpServerResourceAttribute"/> class with required metadata.
    /// </summary>
    /// <param name="uri">The unique URI identifying the resource.</param>
    /// <param name="name">A human-readable name for the resource.</param>
    /// <param name="description">A description of what the resource represents.</param>
    /// <param name="mimeType">The MIME type of the resource content (e.g., "application/json").</param>
    public McpServerResourceAttribute(string uri, string name, string description, string mimeType)
    {
        Resource = new Resource
        {
            Uri = uri,
            Name = name,
            Description = description,
            MimeType = mimeType
        };
    }

    /// <summary>
    /// Gets or sets the size of the resource in bytes, if known.
    /// </summary>
    public long? Size
    {
        get => Resource.Size;
    }

    /// <summary>
    /// Gets or sets additional annotations for the resource, such as intended audience or priority.
    /// </summary>
    public Annotations? Annotations
    {
        get => Resource.Annotations;
    }

    /// <summary>
    /// Returns the underlying <see cref="Resource"/> instance.
    /// </summary>
    public Resource ToResource() => Resource;
}
