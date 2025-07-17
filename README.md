# Configurable Workflow Engine (State Machine API)

This is a minimal backend service built using .NET, which allows clients to:

* Define workflows as configurable state machines (states and actions)
* Start workflow instances
* Execute actions to transition between states with proper validation
* Retrieve the current state and history of any instance

## Getting Started

### Prerequisites

* [.NET 8 or .NET 9 SDK](https://dotnet.microsoft.com/download)
* [Visual Studio Code](https://code.visualstudio.com/)
* (Optional) Postman or curl for testing the API

### Running the Application

To start the application, use the following command:

```bash
dotnet run
```

By default, the application will run at:

```
http://localhost:5057
```

## API Endpoints

| Method | Endpoint                    | Description                                      |
| ------ | --------------------------- | ------------------------------------------------ |
| POST   | `/workflows`                | Create a new workflow definition                 |
| GET    | `/workflows/{id}`           | Retrieve a workflow definition by ID             |
| POST   | `/instances?workflowId=...` | Start a new workflow instance                    |
| POST   | `/instances/{id}/actions`   | Perform an action on an instance                 |
| GET    | `/instances/{id}`           | Get the current state and history of an instance |

## Example Inputs (Postman-friendly)

### Create a Workflow (drivingCar)

```http
POST /workflows

{
  "id": "drivingCar",
  "states": [
    { "id": "parked", "isInitial": true, "isFinal": false, "enabled": true },
    { "id": "engineOn", "isInitial": false, "isFinal": false, "enabled": true },
    { "id": "moving", "isInitial": false, "isFinal": false, "enabled": true },
    { "id": "stopped", "isInitial": false, "isFinal": true, "enabled": true }
  ],
  "actions": [
    { "id": "startEngine", "enabled": true, "fromStates": ["parked"], "toState": "engineOn" },
    { "id": "drive", "enabled": true, "fromStates": ["engineOn"], "toState": "moving" },
    { "id": "brake", "enabled": true, "fromStates": ["moving"], "toState": "stopped" }
  ]
}
```

### Start a Workflow Instance

```http
POST /instances?workflowId=drivingCar
```

### Perform an Action

```http
POST /instances/{id}/actions

{
  "actionId": "startEngine"
}
```

### Get Instance Status

```http
GET /instances/{id}
```

## Validations

* A workflow must contain exactly one initial state
* Actions must meet the following conditions:

  * Must be enabled
  * Must be valid from the current state
  * Must transition to a valid and enabled target state
* Final states do not allow further transitions
* An action ID must be provided in the request payload

## Assumptions

* IDs for states, actions, and workflows must be unique and case-sensitive
* Data is stored in memory and will reset on application restart
* No Swagger or frontend UI is included; use Postman or curl to test the API

## Author

Somya Bhadada – 2025 Infonetica Internship Assignment
