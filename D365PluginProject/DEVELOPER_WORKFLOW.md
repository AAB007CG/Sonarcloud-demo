# Developer Workflow Guide: Extending the D365 Plugin Project

This guide walks developers through adding new features to the `PreOpportunityDelete` plugin using the established patterns.

---

## Workflow 1: Add a New Validation Rule

**Scenario:** You want to add a new business rule: "Cannot delete an opportunity if it has related invoice records"

### Step 1: Add Validation Method to Service Layer

Edit `Services/OpportunityValidationService.cs` and add a new private method:

```csharp
/// <summary>
/// Checks if the opportunity has related invoice records.
/// </summary>
private bool CheckForRelatedInvoices(Guid opportunityId)
{
    var query = new QueryExpression("invoice")
    {
        ColumnSet = new ColumnSet(false),
        Criteria = new FilterExpression
        {
            Conditions =
            {
                new ConditionExpression("opportunityid", ConditionOperator.Equal, opportunityId)
            }
        }
    };

    var result = _organizationService.RetrieveMultiple(query);
    return result.Entities.Count > 0;
}
```

### Step 2: Integrate Rule into ValidateOpportunityDeletion()

Update the main validation method:

```csharp
public ValidationResult ValidateOpportunityDeletion(Guid opportunityId)
{
    // ... existing rule checks ...

    // NEW RULE: Check for related invoices
    var hasRelatedInvoices = CheckForRelatedInvoices(opportunityId);
    if (hasRelatedInvoices)
    {
        return new ValidationResult
        {
            IsValid = false,
            ErrorMessage = "Cannot delete opportunity with related invoice records."
        };
    }

    return new ValidationResult { IsValid = true, ErrorMessage = null };
}
```

### Step 3: Add Unit Test

Edit `Tests/PreOpportunityDeleteTests.cs` and add a new test method:

```csharp
[TestMethod]
[ExpectedException(typeof(InvalidPluginExecutionException), "related invoice")]
public void PreOpportunityDelete_OpportunityHasInvoices_ThrowsException()
{
    // ===== ARRANGE =====
    var opportunity = new Entity("opportunity")
    {
        Id = _opportunityId,
        ["name"] = "Opportunity With Invoices",
        ["statuscode"] = 2  // Not won
    };

    var invoice = new Entity("invoice")
    {
        Id = new Guid("66666666-6666-6666-6666-666666666666"),
        ["name"] = "Invoice 1",
        ["opportunityid"] = new EntityReference("opportunity", _opportunityId)
    };

    _context.Initialize(new[] { opportunity, invoice });

    var plugin = new PreOpportunityDelete();
    var fakeContext = new XrmFakedPluginExecutionContext()
    {
        MessageName = "Delete",
        Stage = 20,
        Depth = 1,
        UserId = _userId,
        InputParameters = new ParameterCollection
        {
            { "Target", new EntityReference("opportunity", _opportunityId) }
        }
    };

    // ===== ACT =====
    try
    {
        fakeContext.ExecutePluginWithConfigurations(plugin, _context, null, null);
    }
    catch (InvalidPluginExecutionException ex)
    {
        Assert.IsTrue(ex.Message.Contains("invoice"));
        throw;
    }
}
```

### Step 4: Test Your Changes

```bash
# From Tests directory
dotnet test --filter "PreOpportunityDelete_OpportunityHasInvoices"

# Run all tests to ensure nothing broke
dotnet test
```

### Step 5: Update Documentation

Edit `PLUGIN_REGISTRATION.md` and add the new rule to the **Business Rules Enforced** section:

```markdown
4. **Rule 4: No Related Invoices**
   - Cannot delete if opportunity has associated invoice records
   - Rationale: Invoices are financial records; must be canceled/archived before opportunity deletion
   - Error Message: "Cannot delete opportunity with related invoice records."
```

---

## Workflow 2: Change Query Filtering (Optimization)

**Scenario:** Query performance is slow; you want to optimize the "check for child quotes" query by adding a specific column filter.

### Step 1: Modify Query in Service Layer

Edit `Services/OpportunityValidationService.cs`, method `CheckForChildQuotes()`:

**Before:**
```csharp
private bool CheckForChildQuotes(Guid opportunityId)
{
    var query = new QueryExpression("quote")
    {
        ColumnSet = new ColumnSet(false),  // No columns needed; just check existence
        Criteria = new FilterExpression
        {
            Conditions =
            {
                new ConditionExpression("quoteid", ConditionOperator.NotNull),
                new ConditionExpression("opportunityid", ConditionOperator.Equal, opportunityId)
            }
        }
    };

    var result = _organizationService.RetrieveMultiple(query);
    return result.Entities.Count > 0;
}
```

**After (optimized with additional filter):**
```csharp
private bool CheckForChildQuotes(Guid opportunityId)
{
    var query = new QueryExpression("quote")
    {
        ColumnSet = new ColumnSet(false),
        Criteria = new FilterExpression
        {
            Conditions =
            {
                new ConditionExpression("opportunityid", ConditionOperator.Equal, opportunityId),
                new ConditionExpression("statecode", ConditionOperator.In, 0)  // Only active quotes
            }
        },
        PageInfo = new PagingInfo { Count = 1, PageNumber = 1 }  // Only need 1 result to know it exists
    };

    var result = _organizationService.RetrieveMultiple(query);
    return result.Entities.Count > 0;
}
```

### Step 2: Update Tests to Reflect Change

Edit `Tests/PreOpportunityDeleteTests.cs`. The existing test may need adjustment if your filter is now more restrictive:

**Before:** Test creates any quote  
**After:** Test must create an *active* quote to trigger the validation

Edit the `PreOpportunityDelete_OpportunityHasQuotes_ThrowsException()` test:

```csharp
var quote = new Entity("quote")
{
    Id = new Guid("33333333-3333-3333-3333-333333333333"),
    ["name"] = "Quote 1",
    ["opportunityid"] = new EntityReference("opportunity", _opportunityId),
    ["statecode"] = 0  // Active (was missing before)
};
```

### Step 3: Run Tests

```bash
dotnet test

# Should still pass because quote is now active
```

### Step 4: Verify in Dataverse

Deploy the updated plugin and test with both:
- Active quotes → Should block deletion
- Inactive quotes → Should allow deletion

---

## Workflow 3: Refactor to Service Layer (Reduce Plugin Complexity)

**Scenario:** Plugin logic is growing complex; you want to extract more logic into the service layer.

### Step 1: Identify What to Extract

Look at `Plugins/PreOpportunityDelete.cs`. Currently, the plugin:
- Validates message type
- Validates input parameters  
- Calls validation service
- Throws exception on failure

**What to extract:** Message validation logic

### Step 2: Create Plugin Configuration Class

Create new file: `Services/PluginValidationConfig.cs`

```csharp
namespace D365PluginProject.Services
{
    public class PluginValidationConfig
    {
        public bool ValidateMessageType(string messageName)
        {
            return messageName == "Delete";
        }

        public bool ValidateInputParameters(ParameterCollection inputParameters)
        {
            return inputParameters.Contains("Target") && 
                   inputParameters["Target"] is EntityReference;
        }
    }
}
```

### Step 3: Update Plugin to Use Config

Edit `Plugins/PreOpportunityDelete.cs`:

```csharp
public void Execute(IServiceProvider serviceProvider)
{
    var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
    var factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
    var service = factory.CreateOrganizationService(context.UserId);
    var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

    // Use validation config
    var config = new PluginValidationConfig();
    if (!config.ValidateMessageType(context.MessageName))
        return;

    if (!config.ValidateInputParameters(context.InputParameters))
        return;

    var targetRef = (EntityReference)context.InputParameters["Target"];
    var validationService = new OpportunityValidationService(service);
    var validationResult = validationService.ValidateOpportunityDeletion(targetRef.Id);

    if (!validationResult.IsValid)
        throw new InvalidPluginExecutionException(validationResult.ErrorMessage);
}
```

### Step 4: Test the Refactoring

```bash
dotnet test

# All existing tests should still pass without modification
```

---

## Workflow 4: Implement Logging Enhancement

**Scenario:** You want to log all validation checks to a custom entity for audit purposes.

### Step 1: Create Logging Service

Create: `Services/OpportunityValidationLogService.cs`

```csharp
public class OpportunityValidationLogService
{
    private readonly IOrganizationService _organizationService;

    public OpportunityValidationLogService(IOrganizationService organizationService)
    {
        _organizationService = organizationService;
    }

    public void LogValidationAttempt(Guid opportunityId, string ruleName, bool passed)
    {
        var logRecord = new Entity("custom_validationlog")
        {
            ["custom_opportunityid"] = new EntityReference("opportunity", opportunityId),
            ["custom_rule"] = ruleName,
            ["custom_passed"] = passed,
            ["custom_timestamp"] = DateTime.UtcNow
        };

        _organizationService.Create(logRecord);
    }
}
```

### Step 2: Inject into Validation Service

Edit `OpportunityValidationService.cs`:

```csharp
public class OpportunityValidationService
{
    private readonly IOrganizationService _organizationService;
    private readonly OpportunityValidationLogService _logService;

    public OpportunityValidationService(IOrganizationService organizationService, OpportunityValidationLogService logService = null)
    {
        _organizationService = organizationService;
        _logService = logService;  // Optional for backwards compatibility
    }

    public ValidationResult ValidateOpportunityDeletion(Guid opportunityId)
    {
        var hasQuotes = CheckForChildQuotes(opportunityId);
        if (hasQuotes)
        {
            _logService?.LogValidationAttempt(opportunityId, "CheckForChildQuotes", false);
            return new ValidationResult { ... };
        }

        _logService?.LogValidationAttempt(opportunityId, "CheckForChildQuotes", true);
        // ... continue with other rules ...
    }
}
```

### Step 3: Update Plugin to Use Logging Service

Edit `Plugins/PreOpportunityDelete.cs`:

```csharp
var validationService = new OpportunityValidationService(
    service, 
    new OpportunityValidationLogService(service)
);
```

### Step 4: Add Tests

Edit `Tests/PreOpportunityDeleteTests.cs`:

```csharp
[TestMethod]
public void OpportunityValidationService_LoggingEnabled_CreatesLogRecords()
{
    // Arrange
    var opportunity = /* ... setup ... */;
    _context.Initialize(new[] { opportunity });
    
    var validationService = new OpportunityValidationService(
        _service,
        new OpportunityValidationLogService(_service)
    );

    // Act
    var result = validationService.ValidateOpportunityDeletion(opportunity.Id);

    // Assert
    var logs = _context.CreateQuery("custom_validationlog").ToList();
    Assert.IsTrue(logs.Count > 0, "At least one log record should be created");
}
```

---

## Testing Strategy Summary

| Test Type | Tool | Where | When to Use |
|-----------|------|-------|-------------|
| **Unit** | FakeXrmEasy | `Tests/` | Test service layer independently |
| **Integration** | FakeXrmEasy | `Tests/` | Test plugin + service layer together |
| **E2E** | Dataverse Sandbox | Manual | Test in actual environment before production |

---

## Common Patterns

### Pattern 1: Service Dependency Injection
```csharp
// ✓ Good: Constructor injection
public OpportunityValidationService(IOrganizationService service)
{
    _organizationService = service;
}

// ✗ Avoid: Static methods or hardcoded lookups
```

### Pattern 2: Query Filtering
```csharp
// ✓ Good: Filter by specific context
var query = new QueryExpression("quote")
{
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("opportunityid", ConditionOperator.Equal, opportunityId)
        }
    }
};

// ✗ Avoid: Querying all records and filtering in code
var allQuotes = service.RetrieveMultiple(new QueryExpression("quote"));
foreach (var quote in allQuotes.Entities) { ... }
```

### Pattern 3: Error Messages
```csharp
// ✓ Good: Specific, actionable error message
throw new InvalidPluginExecutionException(
    "Cannot delete opportunity with active quotes. Please close quotes first.");

// ✗ Avoid: Generic errors
throw new InvalidPluginExecutionException("Validation failed");
```

---

## Troubleshooting Development Issues

| Issue | Solution |
|-------|----------|
| Test compiles but fails at runtime | Check NuGet package versions match across projects |
| FakeXrmEasy not recognizing custom entities | Ensure you `Initialize()` context with entities before querying |
| Plugin works in tests but fails in Dataverse | Check column names, entity relationships, security role permissions |
| Slow queries in Dataverse | Add filters to QueryExpression; avoid retrieving unnecessary columns |

---

**Last Updated:** February 10, 2026  
**Version:** 1.0
