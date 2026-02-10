using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace D365Plugins
{
    public class PreAccountDelete : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            // BUG 1: It doesn't check if "Target" is an EntityReference (Standard for Delete message)
            Entity account = (Entity)context.InputParameters["Target"]; 

            QueryExpression query = new QueryExpression("msdyn_project");
            query.ColumnSet = new ColumnSet(false);
            // BUG 2: It doesn't filter by the Account ID, so it checks ALL projects in the system!
            
            EntityCollection projects = service.RetrieveMultiple(query);

            if (projects.Entities.Count > 0)
            {
                throw new InvalidPluginExecutionException("Cannot delete account with active projects.");
            }
        }
    }
}