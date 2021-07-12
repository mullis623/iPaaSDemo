@description('The name of the workspace.')
param laWorkspaceName string

@description('Azure Region where workspace will be created.')
param location string

@description('Guid for the Cluster Metric Publisher Role on Log Analytics')
param la_aks_guid string

@description('ResourceId of Log Analytics Cluster to use for AKS Container Insights')
param omsWorkspaceId string

@description('Identity Guid for the AKS OMS Agent')
param omsAgentIdentObjectId string


var monitoringMetricsPublisherRole = '/subscriptions/${subscription().subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/3913510d-42f4-4e42-8a64-420c390055eb'

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2020-10-01' existing = {
  name: laWorkspaceName
}

resource clusterMetricPublisherRole 'Microsoft.Authorization/roleAssignments@2021-04-01-preview' = {
  name: la_aks_guid
  scope: logAnalytics
  properties: {
    principalId: omsAgentIdentObjectId
    principalType: 'ServicePrincipal'
    roleDefinitionId: monitoringMetricsPublisherRole
  }
}

resource containerInsights 'Microsoft.OperationsManagement/solutions@2015-11-01-preview' = {
  name: 'ContainerInsights(${laWorkspaceName})'
  location: location
  properties: {
    workspaceResourceId: omsWorkspaceId
  }
  plan: {
    name: 'ContainerInsights(${laWorkspaceName})'
    publisher: 'Microsoft'
    product: 'OMSGallery/ContainerInsights'
    promotionCode: ''
  }
}
