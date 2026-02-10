using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using FakeXrmEasy;
using FakeXrmEasy.PluginContext;

namespace D365Plugins.Tests
{
    [TestClass]
    public class PreAccountDeleteTests
    {
        private XrmFakedContext _context;
        private IOrganizationService _service;

        [TestInitialize]
        public void Setup()
        {
            // Initialize FakeXrmEasy context with support for msdyn_project and account entities
            _context = new XrmFakedContext();
            _service = _context.GetOrganizationService();
        }

        /// <summary>
        /// Test Strategy: Verify that PreAccountDelete throws an exception when an account has active projects.
        /// 
        /// Scenario: Account with ID = {guid1} has 2 associated msdyn_project records.
        /// Expected: InvalidPluginExecutionException with message "Cannot delete account with active projects."
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException), "Cannot delete account with active projects.")]
        public void Execute_AccountHasProjects_ThrowsException()
        {
            // ===== SETUP: Initialize the virtual Dataverse =====
            Guid accountId = new Guid("12345678-1111-1111-1111-111111111111");
            Guid projectId1 = new Guid("87654321-2222-2222-2222-222222222222");
            Guid projectId2 = new Guid("87654321-3333-3333-3333-333333333333");
            Guid userId = new Guid("11111111-0000-0000-0000-000000000000");

            // Create a dummy account entity
            Entity account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account with Projects"
            };
            _context.Initialize(new[] { account });

            // Create 2 project records associated with the account
            Entity project1 = new Entity("msdyn_project")
            {
                Id = projectId1,
                ["msdyn_account"] = new EntityReference("account", accountId),
                ["msdyn_projectname"] = "Project 1"
            };

            Entity project2 = new Entity("msdyn_project")
            {
                Id = projectId2,
                ["msdyn_account"] = new EntityReference("account", accountId),
                ["msdyn_projectname"] = "Project 2"
            };

            // Add projects to the context
            _service.Create(project1);
            _service.Create(project2);

            // ===== SETUP: Mock the Plugin Execution Context =====
            var fakedPlugin = new PreAccountDelete();

            // Create a Delete message context for the account
            var fakeContext = new XrmFakedPluginExecutionContext()
            {
                MessageName = "Delete",
                Stage = 20,  // Pre-operation
                Depth = 1,
                UserId = userId,
                InputParameters = new ParameterCollection
                {
                    // BUG NOTE: The plugin casts Target as Entity, but Delete message passes EntityReference
                    // This test uses EntityReference (correct behavior) and exposes the plugin's cast bug
                    { "Target", new EntityReference("account", accountId) }
                }
            };

            // ===== EXECUTE: Run the plugin =====
            try
            {
                fakeContext.ExecutePluginWithConfigurations(fakedPlugin, _context, null, null);
            }
            catch (InvalidPluginExecutionException ex)
            {
                Assert.IsTrue(ex.Message.Contains("Cannot delete account with active projects"));
                throw;  // Re-throw to satisfy [ExpectedException]
            }
        }

        /// <summary>
        /// Test Strategy: Verify that PreAccountDelete succeeds (no exception) when an account has NO projects.
        /// 
        /// Scenario: Account with ID = {guid3} has no associated msdyn_project records.
        /// Expected: Plugin executes successfully without throwing an exception.
        /// </summary>
        [TestMethod]
        public void Execute_AccountHasNoProjects_Succeeds()
        {
            // ===== SETUP: Initialize the virtual Dataverse =====
            Guid accountId = new Guid("12345678-4444-4444-4444-444444444444");
            Guid userId = new Guid("11111111-0000-0000-0000-000000000000");

            // Create a dummy account entity (with NO projects)
            Entity account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account Without Projects"
            };
            _context.Initialize(new[] { account });

            // ===== SETUP: Mock the Plugin Execution Context =====
            var fakedPlugin = new PreAccountDelete();

            // Create a Delete message context for the account
            var fakeContext = new XrmFakedPluginExecutionContext()
            {
                MessageName = "Delete",
                Stage = 20,  // Pre-operation
                Depth = 1,
                UserId = userId,
                InputParameters = new ParameterCollection
                {
                    { "Target", new EntityReference("account", accountId) }
                }
            };

            // ===== EXECUTE: Run the plugin =====
            fakeContext.ExecutePluginWithConfigurations(fakedPlugin, _context, null, null);

            // ===== ASSERT: Verify the account still exists in the context (deletion happens later in post-op) =====
            Entity retrievedAccount = _context.GetOrganizationService().Retrieve("account", accountId, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            Assert.IsNotNull(retrievedAccount, "Account should still exist after pre-delete plugin succeeds");
            Assert.AreEqual("Test Account Without Projects", retrievedAccount["name"]);
        }

        /// <summary>
        /// Test Strategy: Demonstrate the plugin's BUG #1 - Incorrect Type Cast
        /// 
        /// The Delete message passes EntityReference as Target, not Entity.
        /// The plugin tries to cast it as Entity, which will throw InvalidCastException.
        /// This test documents the bug and its impact.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidCastException))]
        public void Execute_DeleteMessageWithEntityReference_CastExceptionBug()
        {
            // ===== SETUP: Initialize the virtual Dataverse =====
            Guid accountId = new Guid("12345678-5555-5555-5555-555555555555");
            Guid userId = new Guid("11111111-0000-0000-0000-000000000000");

            Entity account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Account to Expose Cast Bug"
            };
            _context.Initialize(new[] { account });

            var fakedPlugin = new PreAccountDelete();

            // Delete message correctly passes EntityReference, not Entity
            var fakeContext = new XrmFakedPluginExecutionContext()
            {
                MessageName = "Delete",
                Stage = 20,
                Depth = 1,
                UserId = userId,
                InputParameters = new ParameterCollection
                {
                    { "Target", new EntityReference("account", accountId) }  // Correct format for Delete
                }
            };

            // ===== EXECUTE: This will throw InvalidCastException due to BUG #1 =====
            fakeContext.ExecutePluginWithConfigurations(fakedPlugin, _context, null, null);
        }
    }
}
