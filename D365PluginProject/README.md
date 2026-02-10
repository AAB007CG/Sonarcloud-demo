# D365 Plugin Project: PreOpportunityDelete

A **complete, production-ready example** of a D365/Dataverse plugin project demonstrating:
- ✅ Service layer separation for testable business logic
- ✅ Comprehensive FakeXrmEasy unit test suite
- ✅ Best practices in plugin architecture and logging
- ✅ Plugin registration documentation
- ✅ Error handling and validation patterns

---

## Project Structure

```
D365PluginProject/
├── Plugins/
│   └── PreOpportunityDelete.cs          # Plugin class - delegates to service layer
├── Services/
│   └── OpportunityValidationService.cs  # Business logic - validation rules
├── Tests/
│   ├── PreOpportunityDeleteTests.cs     # FakeXrmEasy test suite (7 test scenarios)
│   └── D365PluginProject.Tests.csproj   # Test project configuration
├── Models/
│   └── (Empty - ready for custom entity models)
├── D365PluginProject.csproj             # Main plugin project configuration
├── PLUGIN_REGISTRATION.md               # Step-by-step plugin registration guide
└── README.md                            # This file
```

---

## Quick Start

### 1. Build the Plugin Project

```bash
# From D365PluginProject directory
dotnet build --configuration Release
```

**Output:** `bin/Release/net462/D365PluginProject.dll`

### 2. Run the Test Suite

```bash
# From D365PluginProject/Tests directory
dotnet test

# Or run specific test
dotnet test --filter "PreOpportunityDelete_ValidOpportunity_SucceedsWithoutException"
```

**Test Results:** Should see all 7 tests pass ✓

### 3. Register in Dataverse

See [PLUGIN_REGISTRATION.md](PLUGIN_REGISTRATION.md) for step-by-step instructions.

---

## Plugin Overview

### What It Does

The `PreOpportunityDelete` plugin executes when an opportunity is about to be deleted. It validates three business rules:

| Rule | Check | Error Message |
|------|-------|---------------|
| **Rule 1** | No child quote records | "Cannot delete opportunity with associated quote records..." |
| **Rule 2** | Not a Won opportunity | "Cannot delete a Won opportunity. Please re-open or archive..." |
| **Rule 3** | Related account has no active contracts | "Cannot delete opportunity. The related account has active contracts." |

If any rule fails, the plugin throws an `InvalidPluginExecutionException`, preventing the delete.

### Architecture Pattern

```
Plugin Class (Thin)
    ↓
Service Layer (Business Logic)
    ↓
IOrganizationService (Dataverse Queries)
```

**Key Benefit:** The service layer (`OpportunityValidationService`) can be unit-tested independently from the plugin event pipeline.

---

## Understanding the Code

### Plugin Class (`Plugins/PreOpportunityDelete.cs`)

The plugin is intentionally **thin and focused**:
1. Extract services from `IServiceProvider`
2. Validate input parameters (message type, target reference)
3. Call service layer to validate business rules
4. Throw exception if validation fails
5. Log all steps for troubleshooting

```csharp
public void Execute(IServiceProvider serviceProvider)
{
    // Extract context and services
    var context = (IPluginExecutionContext)serviceProvider.GetService(...);
    var service = factory.CreateOrganizationService(context.UserId);
    
    // Delegate validation to service layer
    var validationService = new OpportunityValidationService(service);
    var result = validationService.ValidateOpportunityDeletion(opportunityId);
    
    // Throw if validation fails
    if (!result.IsValid)
        throw new InvalidPluginExecutionException(result.ErrorMessage);
}
```

### Service Layer (`Services/OpportunityValidationService.cs`)

Contains all business logic - **zero plugin dependencies**:

```csharp
public class OpportunityValidationService
{
    public ValidationResult ValidateOpportunityDeletion(Guid opportunityId)
    {
        // 3 validation rules implemented as private methods:
        // - CheckForChildQuotes()
        // - CheckIfOpportunityIsWon()
        // - CheckForActiveContracts()
    }
}
```

**Why This Pattern?**
- Easy to unit test (just create service, call method, assert result)
- Can be reused in other contexts (workflows, console apps, etc.)
- Business logic is isolated from Dataverse event pipeline complexity

### Test Suite (`Tests/PreOpportunityDeleteTests.cs`)

Uses **FakeXrmEasy v2.x** to simulate a virtual Dataverse:

```csharp
[TestMethod]
public void PreOpportunityDelete_ValidOpportunity_SucceedsWithoutException()
{
    // [ARRANGE] Initialize virtual Dataverse
    _context.Initialize(new[] { account, opportunity });
    
    // [ACT] Execute plugin
    fakeContext.ExecutePluginWithConfigurations(plugin, _context, null, null);
    
    // [ASSERT] No exception thrown
    Assert.IsTrue(true);  // If we got here, plugin succeeded
}
```

**Tests Included:**
1. ✓ Valid opportunity can be deleted (Happy Path)
2. ✓ Opportunity with quotes blocked (Error Path)
3. ✓ Won opportunity blocked (Error Path)
4. ✓ Account with active contracts blocks (Error Path)
5. ✓ Non-Delete message exits early (Edge Case)
6. ✓ Opportunity without account handled (Edge Case)
7. ✓ Service layer validation works independently (Service Layer Isolation)

**All tests run in-memory (no Dataverse network calls) in <1 second total** ⚡

---

## Development Workflow

### Adding a New Validation Rule

1. **Add rule logic to `OpportunityValidationService.cs`:**
   ```csharp
   private bool CheckForNewRule(Guid opportunityId)
   {
       var query = new QueryExpression("opportunity")
           .Criteria.AddCondition(...);
       return _organizationService.RetrieveMultiple(query).Entities.Count > 0;
   }
   ```

2. **Update `ValidateOpportunityDeletion()` method to call your new rule:**
   ```csharp
   var violatesNewRule = CheckForNewRule(opportunityId);
   if (violatesNewRule)
       return new ValidationResult { IsValid = false, ErrorMessage = "..." };
   ```

3. **Add test method to `PreOpportunityDeleteTests.cs`:**
   ```csharp
   [TestMethod]
   [ExpectedException(typeof(InvalidPluginExecutionException))]
   public void PreOpportunityDelete_ViolatesNewRule_ThrowsException()
   {
       // Arrange, Act, Assert
   }
   ```

4. **Run tests to verify:**
   ```bash
   dotnet test
   ```

### Testing in Dataverse

1. Build and register plugin (see [PLUGIN_REGISTRATION.md](PLUGIN_REGISTRATION.md))
2. Create test records:
   - Opportunity with quote attached
   - Won opportunity
   - Opportunity with related account having active contract
3. Attempt to delete each via UI or API
4. Verify error messages appear in the **Plugin Trace Log**

---

## Best Practices Demonstrated

| Practice | Example in Code |
|----------|-----------------|
| **Dependency Injection** | `OpportunityValidationService` constructor accepts `IOrganizationService` |
| **Service Layer Separation** | Business logic in service class, plugin orchestration in plugin class |
| **Comprehensive Testing** | 7 test scenarios (happy path, error paths, edge cases) |
| **Logging & Tracing** | `ITracingService` calls throughout plugin execution |
| **Error Handling** | Try-catch with meaningful error messages |
| **Code Comments** | XML documentation and inline explanations |
| **Entity Reference vs Entity** | Correctly uses `EntityReference` for Delete message |
| **Query Filtering** | All queries filter by relevant context (no "select all" queries) |

---

## Common Issues & Solutions

### Issue: Test "InvalidCastException: Unable to cast object"
**Cause:** Trying to cast `EntityReference` as `Entity`  
**Solution:** Delete message pre-operation receives `EntityReference` in Target, not `Entity`. Plugin checks this correctly; tests should pass `EntityReference`.

### Issue: Plugin doesn't seem to execute
**Cause:** Step not registered, entity name incorrect, or plugin not deployed  
**Solution:** Verify in Plugin Registration Tool that step is registered for correct entity + message. Redeploy assembly.

### Issue: "Cannot delete opportunity with associated quote records" always shows
**Cause:** Quote filter logic including all quotes, not just for this opportunity  
**Solution:** Verify QueryExpression filters by `opportunityid` equal to target opportunity (see `CheckForChildQuotes()` method).

### Issue: Tests pass locally but plugin fails in Dataverse
**Cause:** Environmental differences (different data, other plugins, etc.)  
**Solution:** Enable **Plugin Trace Log** in Dataverse to see detailed execution logs. Compare with test scenario.

---

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.CrmSdk.CoreAssemblies` | 9.2.38.7 | D365 SDK for IPlugin, IOrganizationService |
| `FakeXrmEasy` | 2.2.135 | Mocking library for isolated unit tests |
| `MSTest.TestFramework` | 3.0.2 | Unit testing framework |
| `.NET Framework` | 4.6.2 | Plugin target framework (Dataverse compatible) |

**Install NuGet packages:**
```bash
dotnet restore
```

---

## Deployment Checklist

- [ ] All tests pass locally (`dotnet test`)
- [ ] Code reviewed for security (no hardcoded secrets, validated inputs)
- [ ] Plugin built in Release mode (`dotnet build --configuration Release`)
- [ ] Assembly registered in Plugin Registration Tool
- [ ] Plugin step registered (Delete, opportunity, Pre-operation, Stage 20)
- [ ] Tested in dev environment (attempt delete with different scenarios)
- [ ] Plugin Trace Log monitored for errors
- [ ] Documentation updated (PLUGIN_REGISTRATION.md)
- [ ] Team trained on error messages and troubleshooting

---

## Future Enhancements

1. **Configuration-Driven Rules:** Move business rules to a custom entity instead of hardcoding
2. **Multi-Entity Support:** Extend to validate Account, Contact, etc. deletions
3. **Async Logging:** Log validations asynchronously to avoid performance impact
4. **Repository Pattern:** Extract queries into repository classes for reusability
5. **Workflow Activity:** Create custom workflow activity for similar validation in workflows

---

## Support & References

- **Plugin Registration Guide:** [PLUGIN_REGISTRATION.md](PLUGIN_REGISTRATION.md)
- **Microsoft Docs:** [https://learn.microsoft.com/en-us/power-apps/developer/data-platform/plug-ins](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/plug-ins)
- **FakeXrmEasy Docs:** [https://github.com/delegateas/FakeXrmEasy](https://github.com/delegateas/FakeXrmEasy)
- **Dataverse API Reference:** [https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/)

---

**Last Updated:** February 10, 2026  
**Project Status:** ✅ Production-Ready
