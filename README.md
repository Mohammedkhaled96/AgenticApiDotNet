# Agentic API Framework: Architectural Deep Dive

## 1. Executive Summary

### System Overview
The **Agentic API Framework** is a robust, hybrid-intelligence system designed to bridge the gap between deterministic software engineering and probabilistic Large Language Model (LLM) interactions. It functions as a centralized User Management System that can be operated through traditional RESTful API endpoints or via a natural language interface.

### Business Problem
Modern enterprises face a dichotomy: traditional software provides reliability and structure, while AI agents provide flexibility and ease of use. Integrating the two often leads to fragile systems that break when the AI provider is offline or hallucinates. Furthermore, users often struggle with complex API syntax, preferring natural language commands.

### Value Proposition
This system resolves these friction points by implementing a **Dual-Brain Architecture**. It primarily leverages a Semantic Kernel to interpret human intent and orchestrate system tools. Crucially, it features a sophisticated **Deterministic Fallback Engine** that takes over instantaneously if the AI layer becomes unavailable. This ensures high availability and business continuity, allowing users to perform complex CRUD operations via natural language ("Update user 10's name to Sarah") regardless of the AI model's status.

---

## 2. Architectural Philosophy

The project adheres strictly to **Clean Architecture** principles, enforcing a rigorous separation of concerns to ensure maintainability, testability, and scalability.

### Layered Organization

1.  **Presentation Layer (Controllers):**
    *   **Role:** The entry point for HTTP requests.
    *   **Philosophy:** "Thin Controllers." This layer contains no business logic. Its sole responsibility is to accept requests, validate the schema against Data Transfer Objects (DTOs), and delegate execution to the Service Layer. It handles the translation of internal success/failure states into appropriate HTTP status codes (200 OK, 404 Not Found, etc.).

2.  **Application Layer (Services & DTOs):**
    *   **Role:** The heart of the business logic.
    *   **Philosophy:** Use Case Orchestration. This layer defines *what* the system can do (Register, Update, Search). It accepts DTOs (sanitized inputs) and returns Domain Entities. It is completely isolated from the database technology or the specific AI model being used.

3.  **Domain Layer (Entities & Interfaces):**
    *   **Role:** The enterprise business rules.
    *   **Philosophy:** Persistence Ignorance. This layer defines the core business objects (e.g., the `User` entity) and the contracts (Interfaces) that the rest of the system must obey. It has no external dependencies.

4.  **Infrastructure Layer (Data Access & Plugins):**
    *   **Role:** The mechanism of operation.
    *   **Philosophy:** Pluggable Adapters. This layer implements the interfaces defined in the Domain/Application layers. It handles the specific SQL commands (via Entity Framework Core), connects to external AI services (Ollama/OpenAI), and bridges the Semantic Kernel with internal services via Plugins.

---

## 3. Deep Business Logic & Domain Concepts

### Core Entities
*   **The User:** The primary business asset. It is tracked not just by static data (Name, Age, Job Title) but by its lifecycle events (Creation Timestamp, Modification Timestamp). The system enforces logical constraints, such as age limits and mandatory identification fields.

### The "Agentic" Workflow
The system treats API endpoints as "Tools" that can be wielded by an AI agent.
1.  **Intent Recognition:** The system analyzes a text string to determine if the user wants to *Create*, *Read*, *Update*, or *Delete* a record.
2.  **Entity Extraction:** It identifies specific parameters within the text, distinguishing between a User's Name, their Age, and their ID.
3.  **Tool Execution:** It maps the intent to a specific C# function (Plugin) and executes it against the database.
4.  **Response Humanization:** Raw JSON data returned by the database is intercepted and reformatted into natural, conversational text before being returned to the user.

### The Deterministic Fallback Logic (The "Invisible Safety Net")
This is the system's critical reliability feature. When the primary AI brain is unreachable (e.g., network timeout, service outage):
1.  **Automatic Switchover:** The system detects the failure signature (e.g., HTTP connection refusal).
2.  **Regex Heuristics:** A specialized service activates, employing complex Regular Expression patterns to parse the user's prompt.
3.  **Command Execution:** It mimics the AI's decision-making process, routing the parsed data to the exact same Plugins the AI would have used.
4.  **Transparency:** The response is tagged as `[Fallback Agent]`, informing the user that the request was handled logically rather than intelligently, ensuring trust and observability.

---

## 4. Endpoint & Feature Anatomy

### A. The "Converse" Endpoint (Intelligent Orchestrator)
*   **Intent:** To allow users to manage the database using natural language sentences (e.g., "Delete all users," "Register a new HR manager named Alice").
*   **The Process:**
    1.  **Reception:** Accepts a prompt string.
    2.  **AI Attempt:** Tries to send the prompt to the Semantic Kernel.
    3.  **Function Calling:** If online, the AI decides which internal function (Tool) to call.
    4.  **Failure Handling (Deep Logic):** If the AI fails, the **Fallback Service** intercepts the request.
        *   It scans for keywords (Arabic/English) to determine the operation type.
        *   It parses the string to extract arguments (e.g., finding digits after "ID", strings after "Name").
        *   It invokes the `UserApiPlugin` directly.
    5.  **Formatting:** The raw output (usually a JSON object or array) is passed through a **Response Formatter**. This formatter detects if the output is a list or a single object and converts it into a readable string (e.g., `User [ID: 10] Name: X...`).
*   **Outcome:** The user receives a conversational confirmation of their action, regardless of backend system health.

### B. User Management Endpoints (CRUD)
*   **Registration:**
    *   **Process:** Validates input boundaries (e.g., Age 1-150). Timestamps the creation event. Persists to the database.
    *   **Outcome:** A new persistent record is created; a 201 Created status is returned.
*   **Update:**
    *   **Process:** Checks for existence by ID. If found, applies *partial* updates (only non-null fields are modified). Updates the `UpdatedAt` timestamp.
    *   **Outcome:** Data integrity is maintained; the audit trail is updated.
*   **Delete (Single & Batch):**
    *   **Process:** Locates the entity. Removes it from the persistence context. commits the transaction.
    *   **Outcome:** Permanent removal of data.

---

## 5. Data Flow & Orchestration

### Request Lifecycle
1.  **Entry:** A request enters via the `AgentController` or `UserController`.
2.  **Validation:** DTO attributes validate the shape and content of the data.
3.  **Service Delegation:** The Controller passes control to the `IUserService` or `IFallbackAgentService`.
4.  **Persistence:** The Service uses `AppDbContext` to translate business objects into SQL queries.
5.  **Output:** The result bubbles back up. If coming from the Agent flow, it passes through the `FormatAgentResponse` filter to ensure the JSON structure is flattened into text.

### Infrastructure Orchestration
*   **Database Agnosticism:** The system checks the connection string at startup. If it detects MySQL markers, it spins up a MySQL provider. Otherwise, it defaults to a self-contained SQLite instance. This happens automatically without code changes.
*   **Plugin Architecture:** The `UserApiPlugin` acts as a bridge. It is injected into the Semantic Kernel as a "Toolbox." This means the AI does not query the database directly; it asks the Plugin to do it, ensuring business rules in the Service layer are never bypassed.

---

## 6. Security & Reliability Strategy

### Reliability (Resilience)
*   **Retry Policies:** The Database Context is configured with automatic retry logic to handle transient failures (e.g., temporary network blips).
*   **Graceful Degradation:** The entire system is built around the concept that "The AI *will* fail." The Fallback Agent ensures that core business functions (CRUD) remain accessible via the chat interface even during a total AI outage.

### Security
*   **Input Sanitization:** All inputs are bound to strongly-typed DTOs to prevent over-posting attacks.
*   **Audit Logging:** Major actions (Creation, Deletion, Fallback activation) are logged via Serilog. This provides a forensic trail of what the system did and *why* (e.g., "Fallback detected 'Register' command").
*   **Layered Validation:** Data is validated at the Controller level (Syntax) and the Service level (Semantics/Existence), ensuring no invalid data reaches the core.

---

## 7. The AI Engine (Ollama & Llama 3.1)

### Model Architecture
This project is configured to utilize **Llama 3.1**, a state-of-the-art open-source Large Language Model, hosted locally via **Ollama**.

*   **Why Local AI?** By running the model locally, the system achieves zero-latency data privacy (no data leaves the infrastructure), significantly reduced operational costs (no per-token API fees), and full control over the inference environment.
*   **Integration:** The system uses the **Microsoft Semantic Kernel** to interface with the local Ollama endpoint (`http://localhost:11434/v1`). The kernel treats Llama 3.1 not just as a text generator, but as a **reasoning engine** capable of selecting the correct plugin tool based on semantic intent.

### Configuration
While `llama3.1` is the default, the architecture is model-agnostic. The `appsettings.json` configuration allows for seamless hot-swapping to other models (e.g., `mistral`, `gemma`) or even remote endpoints (Azure OpenAI) by modifying the `AI:ModelId` and `AI:Endpoint` values.

---

## 8. API Usage Patterns (Examples)

The following examples illustrate the interaction with the `Converse` endpoint, demonstrating how natural language is translated into structured business actions.

### Scenario A: Registering a User via Chat
**Intent:** The user wants to add a new employee to the system without knowing the specific JSON schema for registration.

**Request (`POST /api/Agent/converse`):**
```json
{
  "prompt": "Please register a new senior engineer named Robert Ford, he is 45 years old."
}
```

**System Response:**
```json
{
  "success": true,
  "message": "[Fallback Agent - AR/EN] User [ID: 15] Name: Robert Ford, Age: 45, Job: senior engineer",
  "executionTimeMs": 1250
}
```
*Note: The system extracted the entities, executed the persistence logic, and returned a formatted confirmation.*

### Scenario B: Complex Update Request
**Intent:** Updating specific fields of an existing record using a command.

**Request (`POST /api/Agent/converse`):**
```json
{
  "prompt": "Update user id 15 make his job title Director of Narrative"
}
```

**System Response:**
```json
{
  "success": true,
  "message": "[Fallback Agent - AR/EN] User [ID: 15] Name: Robert Ford, Age: 45, Job: Director of Narrative",
  "executionTimeMs": 980
}
```

---

## 9. Quick Start Guide (Deployment)

Follow these steps to deploy the Agentic API Framework in a local development environment.

### Prerequisites
*   .NET 9.0 SDK
*   Ollama (for AI orchestration)
*   MySQL Server (Optional; falls back to SQLite automatically)

### Step 1: Clone the Repository
Retrieve the source code from the repository.
### Step 2: AI Environment Setup
This project requires a local AI instance to function in "Intelligent Mode."
1.  **Download Ollama:** Visit [ollama.com](https://ollama.com) and install the version for your OS.
2.  **Pull the Model:** Open your terminal and run the following command to download the Llama 3.1 model.
    ```bash
    ollama pull llama3.1
    ```
3.  **Verify Execution:** Ensure Ollama is running (default port 11434) by visiting `http://localhost:11434` in your browser.
    *   *Note: If you do not wish to install Ollama, the project will automatically default to the **Fallback Agent**, ensuring full functionality via the rule-based engine described in Section 3.*

### Step 3: Configuration
Open the `appsettings.json` file.
*   **Database:** If using MySQL, update the `ConnectionStrings:DefaultConnection` with your server credentials (User/Password).
    ```json
    "ConnectionStrings": {
      "DefaultConnection": "Server=localhost;Database=agentic_db;User=YOUR_USER;Password=YOUR_PASSWORD;"
    }
    ```
*   **AI Model:** Verify the endpoint matches your Ollama setup.
    ```json
    "AI": {
      "ModelId": "llama3.1",
      "Endpoint": "http://localhost:11434/v1"
    }
    ```

### Step 4: Build and Run
Execute the application. The system will automatically apply migrations and create the database on the first run.

```bash
dotnet build
dotnet run
```

Access the Swagger documentation at `http://localhost:5244/swagger` to begin interacting with the API.
