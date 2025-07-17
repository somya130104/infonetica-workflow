//This was created by Somya Bhadada as a part of Infonetica Assignment - 2025.
// Assumes IDs are unique and case-sensitive

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// In-memory storage
var workflowDefinitions = new ConcurrentDictionary<string, WorkflowDefinition>();
var workflowInstances = new ConcurrentDictionary<string, WorkflowInstance>();

// API to create a new workflow definition
app.MapPost("/workflows", async (HttpContext context) =>
{
    var definition = await JsonSerializer.DeserializeAsync<WorkflowDefinition>(context.Request.Body);

    if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
        return Results.BadRequest("Invalid workflow definition");

    int initialStatesCount = definition.States.Count(s => s.IsInitial);

    if (initialStatesCount == 0)
        return Results.BadRequest("No initial state defined");

    if (initialStatesCount > 1)
        return Results.BadRequest("Only one initial state allowed");

    if (workflowDefinitions.ContainsKey(definition.Id))
        return Results.BadRequest("Workflow ID already exists");

    workflowDefinitions[definition.Id] = definition;
    return Results.Ok("Workflow created");
});

// API to get a workflow definition
app.MapGet("/workflows/{id}", (string id) =>
{
    if (!workflowDefinitions.TryGetValue(id, out var definition))
        return Results.NotFound("Workflow not found");
    return Results.Ok(definition);
});

// API to start a new workflow instance
app.MapPost("/instances", (string workflowId) =>
{
    if (!workflowDefinitions.TryGetValue(workflowId, out var definition))
        return Results.NotFound("Workflow not found");

    var initialState = definition.States.First(s => s.IsInitial);
    var instance = new WorkflowInstance
    {
        Id = Guid.NewGuid().ToString(),
        DefinitionId = workflowId,
        CurrentState = initialState.Id,
        History = new List<StateHistory>()
    };

    workflowInstances[instance.Id] = instance;
    return Results.Ok(instance);
});

// API to perform an action on a workflow instance
app.MapPost("/instances/{id}/actions", async (string id, HttpContext context) =>
{
    if (!workflowInstances.TryGetValue(id, out var instance))
        return Results.NotFound("Instance not found");

    var payload = await JsonSerializer.DeserializeAsync<PerformActionPayload>(context.Request.Body);

    if (payload == null || string.IsNullOrWhiteSpace(payload.ActionId))
        return Results.BadRequest("Missing or invalid actionId");

    var definition = workflowDefinitions[instance.DefinitionId];
    var action = definition.Actions.FirstOrDefault(a => a.Id == payload.ActionId);

    if (action == null || !action.Enabled)
        return Results.BadRequest("Invalid or disabled action");

    if (!action.FromStates.Contains(instance.CurrentState))
        return Results.BadRequest("Action not valid from current state");

    var toState = definition.States.FirstOrDefault(s => s.Id == action.ToState);
    if (toState == null || !toState.Enabled)
        return Results.BadRequest("Target state invalid or disabled");

    if (definition.States.First(s => s.Id == instance.CurrentState).IsFinal)
        return Results.BadRequest("Workflow is already in final state");

    instance.CurrentState = toState.Id;
    instance.History.Add(new StateHistory { ActionId = action.Id, Timestamp = DateTime.UtcNow });

    return Results.Ok(instance);
});

// API to get the current state and history of an instance
app.MapGet("/instances/{id}", (string id) =>
{
    if (!workflowInstances.TryGetValue(id, out var instance))
        return Results.NotFound("Instance not found");
    return Results.Ok(instance);
});

app.Run();


// MODELS

record State
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("isInitial")]
    public bool IsInitial { get; set; }

    [JsonPropertyName("isFinal")]
    public bool IsFinal { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

record Action
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("fromStates")]
    public List<string> FromStates { get; set; } = new();

    [JsonPropertyName("toState")]
    public string ToState { get; set; } = "";
}

record WorkflowDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("states")]
    public List<State> States { get; set; } = new();

    [JsonPropertyName("actions")]
    public List<Action> Actions { get; set; } = new();
}

record WorkflowInstance
{
    public string Id { get; set; } = "";
    public string DefinitionId { get; set; } = "";
    public string CurrentState { get; set; } = "";
    public List<StateHistory> History { get; set; } = new();
}

record StateHistory
{
    public string ActionId { get; set; } = "";
    public DateTime Timestamp { get; set; }
}

record PerformActionPayload
{
    [JsonPropertyName("actionId")]
    public string ActionId { get; set; } = "";
}
