using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using ModelContextProtocol.Server;

public class McpAgentRouteRegistrar : MonoBehaviour
{
    public string serverRoutePrefix = "GeneratedFromGameObjectName";
    private Dictionary<object, List<MethodInfo>> _cachedServiceDict;
    private EmbedIOServerHost server;

    void Awake()
    {
        server = FindObjectOfType<EmbedIOServerHost>();
        if (server != null)
            Debug.Log("[Init] Found EmbedIOServerHost automatically.");
        else
            Debug.LogWarning("[Init] EmbedIOServerHost not found in scene.");

        if (_cachedServiceDict == null)
        {
            serverRoutePrefix = gameObject.name;
            _cachedServiceDict = FindTaggedInstancesAndMethods(gameObject);
            server.RegisterService($"/{serverRoutePrefix}", _cachedServiceDict, logFunction: AddNewToolCallInfo);
            Debug.Log("[MCP] Service dictionary created.");
        }
        else
        {
            Debug.Log("[MCP] Using cached service dictionary.");
        }
    }

    static Dictionary<object, List<MethodInfo>> FindTaggedInstancesAndMethods(GameObject root)
    {
        var result = new Dictionary<object, List<MethodInfo>>();
        var components = root.GetComponentsInChildren<MonoBehaviour>(true);

        foreach (var comp in components)
        {
            if (comp == null) continue;
            var type = comp.GetType();
            var methodList = new List<MethodInfo>();

            if (type.GetCustomAttribute<McpServerToolTypeAttribute>() != null
                || type.GetCustomAttribute<McpServerResourceTypeAttribute>() != null)
            {
                foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (method.GetCustomAttribute<McpServerToolAttribute>() != null ||
                        method.GetCustomAttribute<McpServerResourceAttribute>() != null)
                    {
                        methodList.Add(method);
                    }
                }
            }

            if (methodList.Count > 0)
            {
                result[comp] = methodList;
            }
        }

        return result;
    }

    public string latest_tool_call = "";
    public void AddNewToolCallInfo(string call_info)
    {
        latest_tool_call += call_info;
    }
}
