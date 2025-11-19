using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[Serializable]
public class ServiceRegistration
{
    public string routePrefix;

    [NonSerialized]
    public Dictionary<object, List<MethodInfo>> serviceDict;

    public readonly EndpointSessionState SessionState = new();

    public ServiceRegistration(string routePrefix, Dictionary<object, List<MethodInfo>> serviceDict)
    {
        this.routePrefix = routePrefix;
        this.serviceDict = serviceDict;
    }
}
