# Copilot Instructions: POC Learning Project

## Project Overview
This is a **multi-language POC** demonstrating AI-guided code review and testing practices. The workspace contains:
- **Python:** Code review examples (`agent/test_code.py`) with logic errors and security vulnerabilities
- **C# / D365:** Dataverse plugin development (`plugins/AccountDeletePlugin.cs`) with FakeXrmEasy unit tests
- **AI Agents:** Specialized GitHub agents in `.github/agents/` for code review and D365 testing workflows

## Architecture & Key Components

### Python Codebase (`agent/`)
- **Entry Point:** `test_code.py` — Intentionally buggy code for demonstrating review patterns
- **Patterns to Know:**
  - Pythonic loop improvements: Replace `range(len(list))` manual indexing with `zip()` and `enumerate()`
  - Security vulnerabilities: Never hardcode secrets (use `os.getenv()` instead)
  - Common logic errors: Index mismatches, off-by-one errors, unvalidated assumptions

### D365 / Dataverse Plugin Codebase (`plugins/`)
- **Plugin Logic:** `AccountDeletePlugin.cs` — Pre-delete validation plugin with deliberate bugs (for testing demonstration)
- **Test Suite:** `AccountDeletePluginTests.cs` — FakeXrmEasy unit tests covering success and failure scenarios
- **Key Patterns:**
  - **Bug #1:** Incorrect type cast—Delete message passes `EntityReference`, not `Entity`
  - **Bug #2:** Missing query filter—checks ALL projects in system instead of filtering by account ID
  - **Test Structure:** Three A's (Arrange, Act, Assert) with proper virtual Dataverse initialization

### AI Agent Profiles (`.github/agents/`)
Two specialized agents guide development workflows:

1. **Code_Reviewer_Agent** (`code_reviewer_agent.agent.md`)
   - Audits for logic errors, security vulnerabilities, and code quality
   - Outputs: "Findings & Fixes" with Before/After code snippets
   - Severity grades: Critical, Warning, Nitpick

2. **Unit_Testing_Agent** (`code_unit_testing_agent.agent.md`)
   - Generates FakeXrmEasy tests for D365 plugins
   - Focuses on virtual Dataverse simulation and isolated mocking
   - Detects tightly coupled code that needs refactoring for testability

## Critical Developer Workflows

### Code Review Workflow
```
Input: Python file or code snippet
→ Invoke Code_Reviewer_Agent
→ Get: Static analysis, security audit, refactoring suggestions
→ Apply: Before/After code improvements
```
**Example:** `test_code.py` was reviewed to identify:
- Hardcoded API key (security risk) → Use `os.getenv()`
- Manual indexing with `range(len())` → Use `zip()` for safe pairing
- Unvalidated list index access → Naturally handled by `zip()` stopping at shortest

### D365 Plugin Testing Workflow
```
Input: C# IPlugin implementation
→ Invoke Unit_Testing_Agent
→ Get: FakeXrmEasy test boilerplate with virtual Dataverse setup
→ Execute: Test both happy-path and error scenarios
```
**Key Testing Pattern (from `AccountDeletePluginTests.cs`):**
```csharp
// 1. SETUP: Initialize virtual Dataverse
var context = new XrmFakedContext();
context.Initialize(new[] { account, project1, project2 });

// 2. MOCK: Execution context with correct message type
var fakeContext = new XrmFakedPluginExecutionContext()
{
    MessageName = "Delete",
    InputParameters = new ParameterCollection { { "Target", entityReference } }
};

// 3. EXECUTE: Run plugin and assert
fakeContext.ExecutePluginWithConfigurations(plugin, context, null, null);
```

## Project-Specific Conventions

### Python Code Review Standards
- **PEP 8 Compliance:** Check formatting, naming conventions, docstrings
- **Security First:** Flag hardcoded secrets, unvalidated inputs, unsafe operations
- **Pythonic Idioms:** Prefer iterators (`zip()`, `enumerate()`) over manual indexing
- **Documentation:** Require docstrings for functions with complex logic

### D365 Plugin Development Standards
- **Message Type Awareness:** Delete → `EntityReference` | Update/Create → `Entity`
- **Filter by Context:** Always filter queries by relevant entity IDs, never query all records
- **FakeXrmEasy Version:** Default to v2.x syntax (supports modern Dataverse behaviors)
- **Test Coverage:** Minimum two scenarios per plugin (success + failure)
- **Testability:** Suggest service layer extraction for complex business logic

### File Organization
```
plugins/
  ├── [PluginName].cs          # IPlugin implementation
  └── [PluginName]Tests.cs     # FakeXrmEasy test suite

agent/
  └── [functionality].py       # Python modules for review demonstration
```

## Integration Points & Dependencies

### External Dependencies
- **FakeXrmEasy:** NuGet package for mocking Dataverse (installed in test projects)
- **Microsoft.Xrm.Sdk:** D365 plugin framework
- **MSTest / NUnit / xUnit:** Unit testing frameworks (MSTest used in project)

### Cross-Component Communication
- **Plugin → Organization Service:** Uses `IOrganizationService` for RetrieveMultiple queries
- **Test Context → Plugin:** `XrmFakedPluginExecutionContext` simulates event pipeline
- **Agent Framework:** GitHub agents inspect `.github/agents/*.agent.md` for specialized instructions

## Quick Reference: Common Tasks

| Task | Where to Look | Key Pattern |
|------|----------------|------------|
| Review Python code | See `agent/test_code.py` | Use `zip()`, avoid hardcoded secrets |
| Create D365 plugin test | See `plugins/AccountDeletePluginTests.cs` | Initialize context, mock execution, assert results |
| Fix tight coupling | Reference "Logic Isolation" in agent profiles | Extract business logic to service classes |
| Add security check | Use `os.getenv()` for secrets, not hardcoding | Validate inputs before processing |
| Extend plugin tests | Copy XrmFakedPluginExecutionContext pattern | Add new test methods for additional scenarios |

## Known Limitations & Future Improvements

This POC intentionally includes bugs in `AccountDeletePlugin.cs` and `test_code.py` to demonstrate how code review and testing workflows identify and fix issues. In production:
- Always use `EntityReference` for Delete message Target (not Entity cast)
- Always filter queries by relevant context (account ID, opportunity ID, etc.)
- Extract complex plugin logic into testable service classes
