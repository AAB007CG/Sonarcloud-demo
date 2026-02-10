using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace D365PluginProject.Services
{
    /// <summary>
    /// Service class for validating opportunities before deletion.
    /// This class contains business logic isolated from the plugin's event pipeline context.
    /// Benefits: Independently testable, no plugin-specific dependencies, can be reused in other contexts.
    /// </summary>
    public class OpportunityValidationService
    {
        private readonly IOrganizationService _organizationService;

        /// <summary>
        /// Constructor for dependency injection of IOrganizationService.
        /// </summary>
        /// <param name="organizationService">Organization service for Dataverse queries</param>
        public OpportunityValidationService(IOrganizationService organizationService)
        {
            _organizationService = organizationService ?? throw new ArgumentNullException(nameof(organizationService));
        }

        /// <summary>
        /// Validates if an opportunity can be deleted based on business rules.
        /// 
        /// Business Rules:
        /// 1. Cannot delete opportunity with child quote records (quote parent = opportunity)
        /// 2. Cannot delete opportunity marked as "Won" (estatus = 3)
        /// 3. Cannot delete opportunity if related account has active contracts
        /// 
        /// </summary>
        /// <param name="opportunityId">The unique identifier of the opportunity</param>
        /// <returns>Validation result with success flag and error message</returns>
        public ValidationResult ValidateOpportunityDeletion(Guid opportunityId)
        {
            try
            {
                // Rule 1: Check if opportunity has any child quotes
                var hasChildQuotes = CheckForChildQuotes(opportunityId);
                if (hasChildQuotes)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Cannot delete opportunity with associated quote records. Please delete quotes first."
                    };
                }

                // Rule 2: Check if opportunity is marked as Won
                var isWonOpportunity = CheckIfOpportunityIsWon(opportunityId);
                if (isWonOpportunity)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Cannot delete a Won opportunity. Please re-open or archive instead."
                    };
                }

                // Rule 3: Check for related account with active contracts
                var accountHasActiveContracts = CheckForActiveContracts(opportunityId);
                if (accountHasActiveContracts)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Cannot delete opportunity. The related account has active contracts."
                    };
                }

                return new ValidationResult
                {
                    IsValid = true,
                    ErrorMessage = null
                };
            }
            catch (Exception ex)
            {
                // Log exception and return failure with details
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Validation failed with error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Checks if the opportunity has any child quote records.
        /// </summary>
        private bool CheckForChildQuotes(Guid opportunityId)
        {
            var query = new QueryExpression("quote")
            {
                ColumnSet = new ColumnSet(false),
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

        /// <summary>
        /// Checks if the opportunity is marked as Won (state = Won, status = 3).
        /// </summary>
        private bool CheckIfOpportunityIsWon(Guid opportunityId)
        {
            var query = new QueryExpression("opportunity")
            {
                ColumnSet = new ColumnSet("statecode", "statuscode"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("opportunityid", ConditionOperator.Equal, opportunityId),
                        new ConditionExpression("statuscode", ConditionOperator.Equal, 3) // Won
                    }
                }
            };

            var result = _organizationService.RetrieveMultiple(query);
            return result.Entities.Count > 0;
        }

        /// <summary>
        /// Checks if the related account has any active contracts.
        /// </summary>
        private bool CheckForActiveContracts(Guid opportunityId)
        {
            // First, retrieve the opportunity to get the accountid
            var oppQuery = new QueryExpression("opportunity")
            {
                ColumnSet = new ColumnSet("customerid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("opportunityid", ConditionOperator.Equal, opportunityId)
                    }
                }
            };

            var oppResult = _organizationService.RetrieveMultiple(oppQuery);
            if (oppResult.Entities.Count == 0)
            {
                return false; // Opportunity not found
            }

            var opportunity = oppResult.Entities[0];
            if (!opportunity.Contains("customerid") || opportunity["customerid"] == null)
            {
                return false; // No account associated
            }

            var accountRef = (EntityReference)opportunity["customerid"];
            Guid accountId = accountRef.Id;

            // Now check for active contracts on this account
            var contractQuery = new QueryExpression("contract")
            {
                ColumnSet = new ColumnSet(false),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("accountid", ConditionOperator.Equal, accountId),
                        new ConditionExpression("statecode", ConditionOperator.Equal, 0) // Active
                    }
                }
            };

            var contractResult = _organizationService.RetrieveMultiple(contractQuery);
            return contractResult.Entities.Count > 0;
        }
    }

    /// <summary>
    /// Simple validation result object for returning validation status and error messages.
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
    }
}
