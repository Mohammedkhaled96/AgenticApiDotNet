# Agentic API Framework: Architectural Deep Dive (v1.1)

[![CI](https://github.com/Mohammedkhaled96/AgenticApiDotNet/actions/workflows/ci.yml/badge.svg)](https://github.com/Mohammedkhaled96/AgenticApiDotNet/actions/workflows/ci.yml)
[![CodeQL](https://github.com/Mohammedkhaled96/AgenticApiDotNet/actions/workflows/codeql.yml/badge.svg)](https://github.com/Mohammedkhaled96/AgenticApiDotNet/actions/workflows/codeql.yml)
![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)
![Semantic Kernel](https://img.shields.io/badge/Semantic%20Kernel-1.31-0078D4)
![Ollama](https://img.shields.io/badge/Ollama-Llama%203.2-000000)
![MySQL](https://img.shields.io/badge/MySQL-EF%20Core%209-4479A1?logo=mysql&logoColor=white)

## 1. Executive Summary

### System Overview
The **Agentic API Framework** is a robust, autonomous system designed to bridge the gap between deterministic software engineering and probabilistic Large Language Model (LLM) interactions. It functions as a centralized User Management System that can be operated through traditional RESTful API endpoints or via a sophisticated natural language interface.

### Business Problem
Modern enterprises face a dichotomy: traditional software provides reliability and structure, while AI agents provide flexibility and ease of use. Integrating the two often leads to fragile systems. This framework solves that by treating API endpoints as **Autonomous Tools** that an AI can wield with precision.

### Value Proposition
This system leverages **Semantic Kernel** to interpret human intent and orchestrate system tools. It is specifically tuned for **Local AI (Ollama)**, ensuring data privacy, low latency, and zero-cost inference. The latest version features a "Clean-AI" architecture where the LLM is the primary reasoning engine, backed by hardened system instructions to ensure deterministic outcomes even in a probabilistic environment.

---

## 2. Architectural Philosophy

The project adheres strictly to **Clean Architecture** principles, enforcing a rigorous separation of concerns.

### Layered Organization

1.  **Presentation Layer (Controllers):** Handles HTTP ingress. Contains the `AgentController`, which is the gateway to the AI agent.
2.  **Application Layer (Services & DTOs):** Defines the core business logic (Register, Update, Search). Completely isolated from database technology or specific LLMs.
3.  **Domain Layer (Entities & Interfaces):** Enterprise business rules. Definites the `User` entity and persistence contracts.
4.  **Infrastructure Layer (Data & AI Bridge):** Implements EF Core persistence (MySQL/SQLite) and the Semantic Kernel bridge. It uses a **Stable OpenAI Bridge** to communicate with local Ollama instances.

---

## 3. The "Agentic" Workflow

The system treats API endpoints as "Tools" wielded by the **Llama 3.2** model.
1.  **Intent Recognition:** Analyzes text to determine CRUD intent.
2.  **Entity Extraction:** Extracts parameters like Name, Age, and Job Title.
3.  **SEARCH BEFORE ACTION Rule:** If a user request lacks a numeric ID (e.g., "Update Mohamed"), the agent autonomously searches the database first to find the ID before proceeding.
4.  **Tool Execution:** Maps intent to C# functions (`UserApiPlugin`) and executes them against the database.
5.  **TALK NORMALLY Rule:** The agent is instructed to communicate professionally, hiding internal tool names and technical jargon from the user.

---

## 4. AI Engine: Llama 3.2 via Ollama

### Model Strategy
The system is optimized for **`llama3.2:3b`**, a high-performance local model.

*   **Stable Integration:** While Ollama-specific libraries exist, this project uses the **Stable OpenAI Connector** strategy. By pointing the OpenAI connector to the local Ollama V1 endpoint, we achieve maximum stability and support for **Autonomous Function Calling**.
*   **Configuration:** Managed via `appsettings.json`.

```json
"AI": {
  "ModelId": "llama3.2:3b",
  "Endpoint": "http://localhost:11434"
}
```

---

## 5. Database Configuration

The application is configured for **MySQL** by default but includes an automatic fallback to **SQLite** for testing.

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=agentic_db;User=root;Password=your_password;"
}
```

### Automatic Migration
The system automatically applies migrations at startup. If the `Users` table already exists, the migration logic is designed to sync the schema or handle conflicts gracefully.

---

## 6. Security & Reliability

*   **Zero-Trust Parameters:** The AI is strictly limited to the tools provided in the `UserApiPlugin`.
*   **Natural Language Sanitization:** Hardened system prompts prevent the model from using placeholders (like `<insert ID>`) or hallucinating IDs.
*   **Local Inference:** All AI processing happens on-premise; no data is sent to external clouds.

---

## 7. Deep Dive: The Fallback Agent Service (`FallbackAgentService`)

In production environments, dependence on a single AI provider (like Ollama or OpenAI) introduces a single point of failure. The **Fallback Agent Service** is a deterministic, regex-based logic layer that activates automatically when the primary AI service is unavailable, unreachable, or returns an error.

### Why is this Critical?
This service ensures **Business Continuity**. Users can still perform critical operations (Registration, Retrieval, Updates, Deletions) using natural language, even if the "AI Brain" is offline. It acts as a safety net, maintaining the illusion of intelligence through advanced pattern matching.

### When is it Used?
The `AgentController` wraps the Semantic Kernel AI call in a `try-catch` block.
1.  **Primary Attempt:** The system attempts to send the user's prompt to the LLM (e.g., Llama 3.2).
2.  **Failure Detection:** If the LLM call throws an exception (Timeout, Connection Refused, Model not found).
3.  **Activation:** The controller catches the exception and immediately delegates the request to `_fallbackService.ExecuteFallbackLogic(prompt)`.

### How it Works (Technical Details)
The service parses natural language using C# `System.Text.RegularExpressions` to map intent and extract entities. It supports both **English** and **Arabic**.

#### 1. Registration Logic (Intent: "Register", "Create", "Add", "سجل", "انشئ")
It extracts three key entities using cascading Regex patterns:
*   **Name:**
    *   *English:* Looks for "name is X" or "named X".
    *   *Arabic:* Looks for "اسمه X" or "اسم X".
*   **Age:**
    *   *English:* Looks for "age is N" or "N years".
    *   *Arabic:* Looks for "عمره N" or "سن N".
    *   *Default:* Defaults to 25 if not found.
*   **Job Title:**
    *   *English:* Looks for "job X", "job is X", "works as X".
    *   *Arabic:* Looks for "وظيفته X", "يعمل X".

#### 2. Update Logic (Intent: "Update", "Change", "تعديل", "غير" + "ID")
*   **Constraint:** Requires an explicit ID (e.g., "id 5") to be present in the prompt.
*   **Extraction:** updates Name, Age, or Job if the corresponding keywords are found in the string.

#### 3. Query Logic (Intent: "Get", "Show", "List", "هات", "عرض")
*   **List All:** If keywords like "all", "users", "الكل" are found, it invokes `GetAllUsers`.
*   **Get Single:** If "id X" is found, it invokes `GetUserById`.

### Comparative Analysis: AI vs. Fallback

| Feature | Primary AI (Llama 3.2) | Fallback Agent (Regex) |
| :--- | :--- | :--- |
| **Technology** | Large Language Model (Probabilistic) | C# Regex (Deterministic) |
| **Availability** | Dependent on external service/GPU | Always Available (Embedded Code) |
| **Understanding** | Deep contextual understanding | Keyword & Pattern based |
| **Flexibility** | Can handle "Update the guy who is a driver" | Strict: "Update user id 5" |
| **Latency** | 100ms - 2000ms | < 5ms |
| **Response Style** | Natural, varied conversation | Structured, template-based |
| **Multi-turn** | Supports conversation history | Single-turn execution only |

### Example Responses

**Scenario 1: Register User**
*   **User Prompt:** "Register a user named Sarah age 28 job Doctor"
*   **AI Response:** "I have successfully registered Sarah. She is 28 years old and works as a Doctor. Her User ID is 12."
*   **Fallback Response:** `[Fallback Agent - AR/EN] I've registered the user successfully. (AI was offline, used logic). Details: User [ID: 12] Name: Sarah, Age: 28, Job: Doctor`

**Scenario 2: Unknown Command**
*   **User Prompt:** "What is the capital of France?"
*   **AI Response:** "I am an agent focused on User Management. I cannot answer geography questions, but I can help you register a user."
*   **Fallback Response:** `[Fallback Agent] I understood you want to do something, but since the AI brain (Ollama) is offline, I can only handle 'Register', 'Get Users', and 'Delete All' commands...`

---

## 8. Deployment & Quick Start

### Prerequisites
*   .NET 9.0 SDK
*   Ollama (installed and running)
*   MySQL Server

### Setup Steps
1.  **Pull the Model:**
    ```bash
    ollama pull llama3.2:3b
    ```
2.  **Configure Database:** Update `appsettings.json` with your MySQL password.
3.  **Build & Run:**
    ```bash
    dotnet build
    dotnet run
    ```

Access the **Swagger UI** at `http://localhost:5244/swagger` to begin interacting with your autonomous agent.

---

## 8b. Run Everything with Docker (recommended for teams)

One command starts the API, MySQL and Ollama together, with identical package
versions on every machine — no local MySQL or Ollama install, and no
"works on my machine" drift.

**Prerequisites:** Docker Desktop (or Docker Engine + Compose v2).

```bash
cp .env.example .env          # then set MYSQL_ROOT_PASSWORD
docker compose up -d --build  # API + MySQL + Ollama
docker compose --profile setup run --rm ollama-pull   # one-time model download
```

*   **Swagger:** `http://localhost:8080/swagger`
*   **Health:** `http://localhost:8080/health`
*   **Logs:** `docker compose logs -f api`
*   **Stop:** `docker compose down` (add `-v` to also delete the database and model volumes)

### How configuration works

No secret is baked into the image. Compose passes settings as environment
variables, where `__` maps to nested `appsettings.json` keys:

| Variable | Overrides |
|---|---|
| `ConnectionStrings__DefaultConnection` | database connection (points at the `mysql` service) |
| `AI__Endpoint` | `http://ollama:11434` |
| `AI__ModelId` | `llama3.2:3b` by default |

`.env` holds your real password and is git-ignored. The API waits for MySQL to
report healthy before starting, then applies EF Core migrations automatically.

### Running the tests in Docker

```bash
docker build --target test .
```

---

## 9. Testing & CI/CD

The `tests/AgenticApiDemo.Tests` project (xUnit) covers:

*   **`UserService`** — CRUD and filtering against an in-memory SQLite database.
*   **`FallbackAgentService`** — English and Arabic intent detection and entity extraction, using a fake `UserApi` Semantic Kernel plugin (no LLM or database required).

```bash
dotnet test
```

GitHub Actions builds and tests every push and pull request to `main`, CodeQL scans the code for security issues, and Dependabot keeps NuGet packages and actions up to date.