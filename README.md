# Agentic API Framework: Architectural Deep Dive (v1.1)

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

## 7. API Usage Patterns (Examples)

### Scenario: Autonomous Registration & Search
**User:** "Register a user named Mohamed Khaled, age 30, Software Engineer."
**Agent Response:** *"The user 'Mohamed Khaled' has been successfully registered with ID 4. He is 30 years old and works as a Software Engineer."*

**User:** "Now change his job title to Technical Lead."
**Agent Action:**
1.  Calls `GetAllUsers` to find Mohamed's ID.
2.  Finds ID 4.
3.  Calls `UpdateUser(id: 4, jobTitle: "Technical Lead")`.
**Agent Response:** *"I have updated Mohamed Khaled's job title to Technical Lead."*

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
