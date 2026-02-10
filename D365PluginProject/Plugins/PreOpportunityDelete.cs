using System;
using Microsoft.Xrm.Sdk;
using D365PluginProject.Services;

namespace D365PluginProject.Plugins
{
    /// <summary>
    /// PreOpportunityDelete Plugin
    /// 
    /// Executes on: Delete event of opportunity entity (Pre-operation stage)
    /// Purpose: Validate business rules before opportunity deletion is allowed
    /// 
    /// Key Pattern: Thin plugin class that delegates business logic to service layer.
    /// This allows the validation logic to be tested independently from the plugin event pipeline.
    /// 
    /// Business Rules Enforced:
    /// 1. Cannot delete opportunity with child quote records
    /// 2. Cannot delete opportunity marked as "Won"
    /// 3. Cannot delete opportunity if related account has active contracts
    /// </summary>
    public class PreOpportunityDelete : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            try
            {
                // Extract services from the service provider
                var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
                var factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                var service = factory.CreateOrganizationService(context.UserId);
                var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

                tracingService?.TracingLog("PreOpportunityDelete plugin started. Message: {0}, Stage: {1}", 
                    context.MessageName, context.Stage);

                // Validate this is a Delete message with an opportunity target
                if (context.MessageName != "Delete")
                {
                    tracingService?.TracingLog("Message is not 'Delete'. Exiting plugin.", null);
                    return;
                }

                // Extract the target entity reference from InputParameters
                // Note: Delete message passes EntityReference, not Entity
                if (!context.InputParameters.Contains("Target") || !(context.InputParameters["Target"] is EntityReference))
                {
                    tracingService?.TracingLog("Target parameter is missing or not an EntityReference. Exiting plugin.", null);
                    return;
                }

                var targetRef = (EntityReference)context.InputParameters["Target"];
                Guid opportunityId = targetRef.Id;

                tracingService?.TracingLog("Validating opportunity deletion for ID: {0}", opportunityId.ToString());

                // Call service layer to validate business rules
                var validationService = new OpportunityValidationService(service);
                var validationResult = validationService.ValidateOpportunityDeletion(opportunityId);

                if (!validationResult.IsValid)
                {
                    tracingService?.TracingLog("Validation failed: {0}", validationResult.ErrorMessage);
                    throw new InvalidPluginExecutionException(validationResult.ErrorMessage);
                }

                tracingService?.TracingLog("Opportunity deletion validation passed. Allowing deletion to proceed.", null);
            }
            catch (Exception ex)
            {
                // Log the exception for troubleshooting
                var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
                tracingService?.TracingLog("Plugin exception: {0}\nStack Trace: {1}", ex.Message, ex.StackTrace);

                // Re-throw if it's our validation exception; otherwise wrap it
                if (ex is InvalidPluginExecutionException)
                {
                    throw;
                }

                throw new InvalidPluginExecutionException("An unexpected error occurred during opportunity deletion validation.", ex);
            }
        }
    }
}

/// <summary>
/// Extension method for cleaner tracing. 
/// This pattern allows: tracingService?.TracingLog("Message {0}", param)
/// </summary>
internal static class TracingServiceExtensions
{
    public static void TracingLog(this ITracingService tracingService, string format, params object[] args)
    {
        if (tracingService != null)
        {
            try
            {
                tracingService.Trace(string.Format(format, args));
            }
            catch
            {
                // Swallow tracing errors to avoid plugin failures due to logging issues
            }
        }
    }
}
