# Plugin Registration Guide

## Plugin: PreOpportunityDelete

### Overview
Pre-operation validation plugin that prevents deletion of opportunity records based on business rules.

### Event Configuration

| Property | Value |
|----------|-------|
| **Entity** | opportunity |
| **Message** | Delete |
| **Stage** | Pre-operation (20) |
| **Execution Order** | 1 (default) |
| **Deployment** | Server |

### Business Rules Enforced

1. **Rule 1: No Child Quotes**
   - Cannot delete if opportunity has associated quote records
   - Rationale: Quotes are legally binding; must be closed before opportunity deletion
   - Error Message: "Cannot delete opportunity with associated quote records. Please delete quotes first."

2. **Rule 2: Not a Won Opportunity**
   - Cannot delete if opportunity status is "Won"
   - Rationale: Won opportunities should be archived, not deleted (audit trail preservation)
   - Error Message: "Cannot delete a Won opportunity. Please re-open or archive instead."

3. **Rule 3: Related Account Has No Active Contracts**
   - Cannot delete if the related account has active (non-expired) contracts
   - Rationale: Active contracts imply ongoing business with account; deleting related opportunities could cause confusion
   - Error Message: "Cannot delete opportunity. The related account has active contracts."

### Pre-Image Configuration

**Stage:** Pre-operation  
**Image Type:** Not required (Delete message passes EntityReference, not full entity)  

### Post-Image Configuration

**Stage:** Not applicable (plugin throws error before deletion)

### Dependencies

| Component | Purpose |
|-----------|---------|
| `OpportunityValidationService.cs` | Business logic for all validation rules |
| `IOrganizationService` | Dataverse queries for validation checks |
| `ITracingService` | Debug logging for troubleshooting |

### Testing

Run the test suite `PreOpportunityDeleteTests.cs` to verify:

```bash
dotnet test
```

Test Coverage:
- ✓ Happy Path: Valid opportunity deletion allowed
- ✓ Error Path: Opportunity with quotes blocked
- ✓ Error Path: Won opportunity blocked
- ✓ Error Path: Related account with active contracts blocked
- ✓ Edge Case: Non-Delete message exits early
- ✓ Edge Case: Opportunity without account handled correctly
- ✓ Service Layer Isolation: Validation service works independently

### Deployment Steps

1. **Build the Plugin Assembly**
   ```bash
   dotnet build --configuration Release
   ```

2. **Register Assembly in Dataverse**
   - Open **Plugin Registration Tool** (PRT)
   - Connect to target Dataverse environment
   - Click "Register" → "Register New Assembly"
   - Select compiled DLL from `bin/Release/net462/` (or appropriate framework)
   - Choose deployment: **Server**

3. **Register the Plugin Step**
   - Expand the registered assembly
   - Click "Register New Step"
   - **Configuration:**
     - Message: `Delete`
     - Entity: `opportunity`
     - Stage: `Pre-operation (20)`
     - Execution Mode: `Synchronous`
     - Execution Order: `1`
   - Click "Register New Step"

4. **Test the Plugin**
   - Deploy to development environment first
   - Create test opportunities with various scenarios:
     - Opportunity with quotes
     - Won opportunity
     - Opportunity with related account having contracts
   - Attempt to delete each
   - Verify appropriate error messages appear

5. **Monitor Plugin Traces**
   - Open **Plugin Trace Log** in model-driven app (Settings → Diagnostics)
   - Look for `PreOpportunityDelete` executions
   - Check for validation failures or unexpected exceptions

### Troubleshooting

| Symptom | Cause | Solution |
|---------|-------|----------|
| Plugin not firing | Step not registered or entity name incorrect | Verify registration in PRT; ensure entity is "opportunity" |
| "Target parameter is missing" error | InputParameters malformed | Check plugin code; this is defensive check |
| "Unexpected error" message | Unhandled exception in validation service | Check Plugin Trace Log in Dataverse for details |
| Performance issues | Queries retrieving many records | Optimize QueryExpression filters; add indexes in Dataverse |

### Future Enhancements

1. **Async Plugin:** Consider Post-operation async for audit logging instead of just blocking
2. **Configuration:** Store business rules in entity configuration instead of hardcoding
3. **Multi-entity Support:** Extend to handle Account deletion also checking for related opportunities
4. **Service Layer Refactoring:** Extract database queries into separate repository pattern

### Related Plugins

If implementing comprehensive opportunity management:
- `PreOpportunityCancelledUpdate` — Validate before canceling active opportunities
- `PostOpportunityClose` — Audit log when opportunity is closed (won or lost)
- `PreAccountDelete` — Would also check for related opportunities

### Support & Maintenance

**Plugin Owner:** [Your Team Name]  
**Last Updated:** February 10, 2026  
**Deployment Environments:** Dev, Test, Production  
**Monitoring:** Check Plugin Trace Log for failures
