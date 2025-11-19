using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using ModelContextProtocol.Server;
using System.Threading;
using System.ComponentModel;

[McpServerResourceType]
public class ListOfAgents : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private string agentEndpoints;
    private List<GameObject> agents;

    public IMcpServer mcpServer; // for future use when restarting agents

    public void Start()
    {
        FindAgents();
    }

    [McpServerResource(
        uri: "resource://ListOfAgents/current",
        name: "ListOfAgents",
        description: "Available agents.",
        mimeType: "application/json")]
    public string GetAgentEndpoints()
    {
        try
        {
            if (agentEndpoints == null) { FindAgents(); }
            return agentEndpoints;
        }
        catch (System.Exception ex)
        {
            MainThreadLogger.LogError($"[FlightSensors] Error: {ex}");
            throw;
        }
    }

    public List<GameObject> GetAgents()
    {
        if (agents == null) { FindAgents(); }
        return agents;

    }

    private void FindAgents()
    {
        List<string> allRoutes = new List<string>();
        agents = new List<GameObject>();

        McpAgentRouteRegistrar[] agentScripts = FindObjectsOfType<McpAgentRouteRegistrar>();

        foreach (var agent in agentScripts)
        {
            if (agent.gameObject == gameObject) { continue; } // exclude manager registrar
            agents.Add(agent.gameObject);


            if (agent.serverRoutePrefix != null)
            {
                allRoutes.Add(agent.serverRoutePrefix);
            }
            List<string> endpoints = allRoutes.Distinct().ToList();
            var data = new
            {
                endpoints = allRoutes.Distinct().ToList()
            };
            agentEndpoints = JsonConvert.SerializeObject(data);
        }
    }

}