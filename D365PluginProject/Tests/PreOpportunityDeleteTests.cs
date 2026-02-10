using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using FakeXrmEasy;
using FakeXrmEasy.PluginContext;
using D365PluginProject.Plugins;
using D365PluginProject.Services;

namespace D365PluginProject.Tests
{
    [TestClass]
    public class PreOpportunityDeleteTests
    {
        private XrmFakedContext _context;
        private IOrganizationService _service;
        private Guid _opportunityId;
        private Guid _accountId;
        private Guid _userId;

        [TestInitialize]
        public void Setup()
        {
            // Initialize FakeXrmEasy virtual Dataverse context
            _context = new XrmFakedContext();
            _service = _context.GetOrganizationService();

            // Create test data GUIDs
            _opportunityId = new Guid("11111111-1111-1111-1111-111111111111");
            _accountId = new Guid("22222222-2222-2222-2222-222222222222");
            _userId = new Guid("99999999-9999-9999-9999-999999999999");
        }

        /// <summary>
        /// Test Strategy: Happy Path - Opportunity with no quotes, not won, and no related contracts can be deleted.
        /// Expected: Plugin executes successfully without throwing exception.
        /// </summary>
        [TestMethod]
        public void PreOpportunityDelete_ValidOpportunity_SucceedsWithoutException()
        {
            // ===== ARRANGE: Create valid opportunity with no violations =====
            var account = new Entity("account")
            {
                Id = _accountId,
                ["name"] = "Test Account - No Contracts"
            };

            var opportunity = new Entity("opportunity")
            {
                Id = _opportunityId,
                ["name"] = "Test Opportunity",
                ["customerid"] = new EntityReference("account", _accountId),
                ["statuscode"] = 2  // Open (not Won = 3)
            };

            _context.Initialize(new[] { account, opportunity });

            var plugin = new PreOpportunityDelete();
            var fakeContext = new XrmFakedPluginExecutionContext()
            {
                MessageName = "Delete",
                Stage = 20,  // Pre-operation
                Depth = 1,
                UserId = _userId,
                InputParameters = new ParameterCollection
                {
                    { "Target", new EntityReference("opportunity", _opportunityId) }
                }
            };

            // ===== ACT: Execute the plugin =====
            fakeContext.ExecutePluginWithConfigurations(plugin, _context, null, null);

            // ===== ASSERT: Plugin completed without exception =====
            // If we reach here, the plugin succeeded
            Assert.IsTrue(true, "Plugin executed successfully for valid opportunity");
        }

        /// <summary>
        /// Test Strategy: Error Path - Opportunity with child quote records cannot be deleted.
        /// Expected: InvalidPluginExecutionException with specific error message.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException), "Cannot delete opportunity with associated quote records")]
        public void PreOpportunityDelete_OpportunityHasQuotes_ThrowsException()
        {
            // ===== ARRANGE: Create opportunity with child quote =====
            var opportunity = new Entity("opportunity")
            {
                Id = _opportunityId,
                ["name"] = "Opportunity With Quote",
                ["statuscode"] = 2  // Not won
            };

            var quote = new Entity("quote")
            {
                Id = new Guid("33333333-3333-3333-3333-333333333333"),
                ["name"] = "Quote 1",
                ["opportunityid"] = new EntityReference("opportunity", _opportunityId)
            };

            _context.Initialize(new[] { opportunity, quote });

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

            // ===== ACT: Execute the plugin =====
            try
            {
                fakeContext.ExecutePluginWithConfigurations(plugin, _context, null, null);
            }
            catch (InvalidPluginExecutionException ex)
            {
                Assert.IsTrue(ex.Message.Contains("quote records"));
                throw;  // Re-throw to satisfy [ExpectedException]
            }
        }

        /// <summary>
        /// Test Strategy: Error Path - Won opportunity cannot be deleted.
        /// Expected: InvalidPluginExecutionException with specific error message.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException), "Cannot delete a Won opportunity")]
        public void PreOpportunityDelete_OpportunityIsWon_ThrowsException()
        {
            // ===== ARRANGE: Create won opportunity =====
            var opportunity = new Entity("opportunity")
            {
                Id = _opportunityId,
                ["name"] = "Won Opportunity",
                ["statuscode"] = 3,  // Won status
                ["statecode"] = 1    // Won state
            };

            _context.Initialize(new[] { opportunity });

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

            // ===== ACT: Execute the plugin =====
            try
            {
                fakeContext.ExecutePluginWithConfigurations(plugin, _context, null, null);
            }
            catch (InvalidPluginExecutionException ex)
            {
                Assert.IsTrue(ex.Message.Contains("Won"));
                throw;  // Re-throw to satisfy [ExpectedException]
            }
        }

        /// <summary>
        /// Test Strategy: Error Path - Opportunity with related account having active contracts cannot be deleted.
        /// Expected: InvalidPluginExecutionException with specific error message.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException), "active contracts")]
        public void PreOpportunityDelete_RelatedAccountHasActiveContracts_ThrowsException()
        {
            // ===== ARRANGE: Create opportunity with related account, account has active contract =====
            var account = new Entity("account")
            {
                Id = _accountId,
                ["name"] = "Account With Active Contract"
            };

            var opportunity = new Entity("opportunity")
            {
                Id = _opportunityId,
                ["name"] = "Opportunity Linked to Account",
                ["customerid"] = new EntityReference("account", _accountId),
                ["statuscode"] = 2  // Not won
            };

            var contract = new Entity("contract")
            {
                Id = new Guid("44444444-4444-4444-4444-444444444444"),
                ["title"] = "Active Contract",
                ["accountid"] = new EntityReference("account", _accountId),
                ["statecode"] = 0  // Active
            };

            _context.Initialize(new[] { account, opportunity, contract });

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

            // ===== ACT: Execute the plugin =====
            try
            {
                fakeContext.ExecutePluginWithConfigurations(plugin, _context, null, null);
            }
            catch (InvalidPluginExecutionException ex)
            {
                Assert.IsTrue(ex.Message.Contains("contracts"));
                throw;  // Re-throw to satisfy [ExpectedException]
            }
        }

        /// <summary>
        /// Test Strategy: Edge Case - Non-Delete message should not trigger validation.
        /// Expected: Plugin should exit early without validation.
        /// </summary>
        [TestMethod]
        public void PreOpportunityDelete_NonDeleteMessage_ExitsWithoutValidation()
        {
            // ===== ARRANGE: Create opportunity (even if it violates rules) =====
            var opportunity = new Entity("opportunity")
            {
                Id = _opportunityId,
                ["name"] = "Opportunity With Quote",
                ["statuscode"] = 2
            };

            var quote = new Entity("quote")
            {
                Id = new Guid("55555555-5555-5555-5555-555555555555"),
                ["opportunityid"] = new EntityReference("opportunity", _opportunityId)
            };

            _context.Initialize(new[] { opportunity, quote });

            var plugin = new PreOpportunityDelete();
            var fakeContext = new XrmFakedPluginExecutionContext()
            {
                MessageName = "Update",  // Not a Delete message
                Stage = 20,
                Depth = 1,
                UserId = _userId,
                InputParameters = new ParameterCollection
                {
                    { "Target", new EntityReference("opportunity", _opportunityId) }
                }
            };

            // ===== ACT: Execute the plugin =====
            fakeContext.ExecutePluginWithConfigurations(plugin, _context, null, null);

            // ===== ASSERT: Plugin should not throw exception even though opportunity has quotes =====
            Assert.IsTrue(true, "Plugin exited early without validation for Update message");
        }

        /// <summary>
        /// Test Strategy: Edge Case - Opportunity without related account (contract check should skip).
        /// Expected: Plugin succeeds even though contract check logic executes.
        /// </summary>
        [TestMethod]
        public void PreOpportunityDelete_OpportunityWithoutAccount_Succeeds()
        {
            // ===== ARRANGE: Create opportunity with no account =====
            var opportunity = new Entity("opportunity")
            {
                Id = _opportunityId,
                ["name"] = "Opportunity Without Account",
                ["statuscode"] = 2  // Not won
                // No customerid
            };

            _context.Initialize(new[] { opportunity });

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

            // ===== ACT: Execute the plugin =====
            fakeContext.ExecutePluginWithConfigurations(plugin, _context, null, null);

            // ===== ASSERT: Plugin succeeded =====
            Assert.IsTrue(true, "Plugin handled opportunity without account correctly");
        }

        /// <summary>
        /// Test Strategy: Service Layer Isolation - Validate OpportunityValidationService independently.
        /// Expected: Service returns correct validation results based on business rules.
        /// </summary>
        [TestMethod]
        public void OpportunityValidationService_MultipleValidationRules_ReturnsCorrectResults()
        {
            // ===== ARRANGE: Set up context with multiple test scenarios =====
            var account = new Entity("account") { Id = _accountId, ["name"] = "Test" };
            
            // Opportunity 1: No issues
            var opp1 = new Entity("opportunity")
            {
                Id = new Guid("10000000-0000-0000-0000-000000000001"),
                ["statuscode"] = 2,
                ["customerid"] = new EntityReference("account", _accountId)
            };

            // Opportunity 2: Has quotes
            var opp2 = new Entity("opportunity")
            {
                Id = new Guid("10000000-0000-0000-0000-000000000002"),
                ["statuscode"] = 2
            };
            var quote2 = new Entity("quote")
            {
                Id = new Guid("20000000-0000-0000-0000-000000000002"),
                ["opportunityid"] = new EntityReference("opportunity", opp2.Id)
            };

            _context.Initialize(new[] { account, opp1, opp2, quote2 });

            var validationService = new OpportunityValidationService(_service);

            // ===== ACT & ASSERT: Test multiple scenarios =====
            
            // Valid opportunity
            var result1 = validationService.ValidateOpportunityDeletion(opp1.Id);
            Assert.IsTrue(result1.IsValid, "Opportunity 1 should be valid for deletion");

            // Opportunity with quotes
            var result2 = validationService.ValidateOpportunityDeletion(opp2.Id);
            Assert.IsFalse(result2.IsValid, "Opportunity 2 should be invalid due to quotes");
            Assert.IsTrue(result2.ErrorMessage.Contains("quote"), "Error message should mention quotes");
        }
    }
}
