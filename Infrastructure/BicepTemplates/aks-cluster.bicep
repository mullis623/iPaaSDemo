@description('The location of AKS and ACR resources.')
param location string = resourceGroup().location

@minValue(0)
@maxValue(1023)
@description('Disk size (in GiB) to provision for each of the agent pool nodes. This value ranges from 0 to 1023. Specifying 0 will apply the default disk size for that agentVMSize.')
param osDiskSizeGB int = 0

@description('List of Deployment Settings used by Azure Kubernetes.')
param aksSettings object

@allowed([
  '1.20.5'
  '1.20.2'
  '1.19.9'
  '1.19.7'
  '1.18.17'
  '1.18.14'
])
@description('The version of Kubernetes.')
param kubernetesVersion string

@allowed([
  'azure'
  'kubenet'
])
@description('Network plugin used for building Kubernetes network.')
param networkPlugin string = 'kubenet'

@allowed([
  'azure'
  'calico'
])
@description('Network policy used for building Kubernetes network.')
param networkPolicy string = 'calico'

@allowed([
  'basic'
  'standard'
])
@description('Load Balancer Sku to use for Kubernetes network.')
param loadBalancerSku string = 'standard'

@description('List of Deployment Settings used by the container registry.')
param acrSettings object

@allowed([
  'Basic'
  'Standard'
  'Premium'
])
@description('The sku name for the container registry.')
param acrSkuName string

@description('The unique id used in the role assignment of the kubernetes service to the container registry service. It is recommended to use the default value.')
param aks_acr_guid string = newGuid()

@description('The unique id used in the role assignment of the Log Analytics Workspace to read AKS Metrics. It is recommended to use the default value.')
param la_aks_guid string = newGuid()

@description('The name of the resource group the container registry is associated with.')
param acrResourceGroup string = resourceGroup().name

@description('The name of the Log Analytics Workspace used by Container Insights.')
param laWorkspaceName string

resource registry 'Microsoft.ContainerRegistry/registries@2020-11-01-preview' = {
  name: acrSettings.Name
  location: location
  sku: {
    name: acrSkuName
  }
  properties: {
    adminUserEnabled: acrSettings.adminUserEnabled
    publicNetworkAccess: acrSettings.publicNetworkAccess
    networkRuleBypassOptions: acrSettings.networkRuleBypassOption
    zoneRedundancy: acrSettings.zoneRedundancy
    anonymousPullEnabled: acrSettings.anonPullEnabled
  }
}

resource aksCluster 'Microsoft.ContainerService/managedClusters@2021-02-01' = {
  location: location
  name: aksSettings.Name
  properties: {
    kubernetesVersion: kubernetesVersion
    enableRBAC: aksSettings.enableRBAC
    dnsPrefix: aksSettings.dnsPrefix
    agentPoolProfiles: [
      {
        name: 'agentpool'
        osDiskSizeGB: osDiskSizeGB
        count: aksSettings.nodeCount
        vmSize: aksSettings.nodeSize
        osType: 'Linux'
        type: aksSettings.agentPoolType
        mode: aksSettings.agentPoolMode
        maxPods: 110
        availabilityZones: [
          '1'
          '2'
        ]
      }
    ]
    networkProfile: {
      loadBalancerSku: loadBalancerSku
      networkPlugin: networkPlugin
      networkPolicy: networkPolicy
    }
    aadProfile: {
      managed: true
      adminGroupObjectIDs: aksSettings.adminGroupObjectIDs
    }
    apiServerAccessProfile: {
      enablePrivateCluster: aksSettings.enablePrivateCluster
    }
    addonProfiles: {
      httpApplicationRouting: {
        enabled: aksSettings.enableHttpApplicationRouting
      }
      azurepolicy: {
        enabled: aksSettings.enableAzurePolicy
      }
      omsAgent: {
        enabled: aksSettings.enableOmsAgent
        config: {
          logAnalyticsWorkspaceResourceID: aksSettings.omsWorkspaceId
        }
      }
    }
  }
  tags: {}
  identity: {
    type: 'SystemAssigned'
  }
}

resource attachACR 'Microsoft.Authorization/roleAssignments@2021-04-01-preview' = {
  name: aks_acr_guid
  scope: registry
  properties: {
    principalId: aksCluster.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: '/subscriptions/${subscription().subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/b24988ac-6180-42a0-ab88-20f7382dd24c'
  }
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2020-10-01' existing = {
  name: laWorkspaceName
}

resource clusterMetricPublisherRole 'Microsoft.Authorization/roleAssignments@2021-04-01-preview' ={
  name: la_aks_guid
  scope: logAnalytics
  properties: {
    principalId: aksCluster.properties.addonProfiles.omsAgent.identity.objectId
    principalType: 'ServicePrincipal'
    roleDefinitionId: '/subscriptions/${subscription().subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/3913510d-42f4-4e42-8a64-420c390055eb'
  }
}

resource containerInsights 'Microsoft.OperationsManagement/solutions@2015-11-01-preview' = {
  name: 'ContainerInsights(${laWorkspaceName})'
  location: location
  properties: {
    workspaceResourceId: aksSettings.omsWorkspaceId
  }
  plan: {
    name: 'ContainerInsights(${laWorkspaceName})'
    publisher: 'Microsoft'
    product: 'OMSGallery/ContainerInsights'
    promotionCode: ''
  }
}

output controlPlaneFQDN string = aksCluster.properties.fqdn
