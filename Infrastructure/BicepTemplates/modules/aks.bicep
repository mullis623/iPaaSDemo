@description('The location of AKS and ACR resources.')
param location string

param Name string

@description('The type of identity used for the managed cluster.')
param identityType string

@allowed([
  '1.20.7'
  '1.20.5'
  '1.19.11'
  '1.19.9'
  '1.18.19'
  '1.18.17'
])
@description('The version of Kubernetes.')
param kubernetesVersion string

@description('DNS prefix for the AKS cluster.')
param dnsPrefix string

@description('Whether to enable Kubernetes Role-Based Access Control.')
param enableRBAC bool = false

@description('Whether to enable Azure RBAC for Kubernetes authorization.')
param enableAADRBAC bool = false

@description('Whether to enable managed AAD.')
param enableManagedAAD bool = false

@description('AAD group object IDs that will have admin role of the cluster. - string array')
param adminGroupObjectIDs array = []

@description('Whether to create the cluster as a private cluster or not.')
param enablePrivateCluster bool = false

@description('Should Azure Policy be enabled as an Add on Profile.')
param enableAzurePolicy bool = false

@description('Should Log Analytics be enabled as an Add on Profile.')
param enableOmsAgent bool = false

@description('Log Analytics Workspace Resource ID - if enabledOmsAgent set to true.')
param omsWorkspaceId string = 'NA'

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

@description('Resource Group Name where AKS Node resources will be deployed')
param aksNodeRGName string

@description('Details of the Agent Pool(s) to create for this cluster.')
param agentPoolProfiles array

resource aksCluster 'Microsoft.ContainerService/managedClusters@2021-02-01' = {
  location: location
  name: Name
  identity: {
    type: identityType
  }
  properties: {
    kubernetesVersion: kubernetesVersion
    enableRBAC: enableRBAC
    dnsPrefix: dnsPrefix
    nodeResourceGroup: aksNodeRGName
    agentPoolProfiles: [ for agentPoolProfile in agentPoolProfiles: {
        name: agentPoolProfile.Name
        count: agentPoolProfile.nodeCount
        vmSize: agentPoolProfile.nodeSize
        osDiskSizeGB: agentPoolProfile.osDiskSizeGB    
        osType: agentPoolProfile.osType
        type: agentPoolProfile.agentPoolType
        mode: agentPoolProfile.agentPoolMode
        maxPods: agentPoolProfile.maxPods
        availabilityZones: agentPoolProfile.availabilityZones
    }]
    networkProfile: {
      loadBalancerSku: loadBalancerSku
      networkPlugin: networkPlugin
      networkPolicy: networkPolicy
    }
    aadProfile: {
      managed: enableManagedAAD
      enableAzureRBAC: enableAADRBAC
      adminGroupObjectIDs: adminGroupObjectIDs
    }
    apiServerAccessProfile: {
      enablePrivateCluster: enablePrivateCluster
    }
    addonProfiles: {
      azurepolicy: {
        enabled: enableAzurePolicy
      }
      omsAgent: {
        enabled: enableOmsAgent
        config: ((enableOmsAgent == true) ? {
          logAnalyticsWorkspaceResourceID: omsWorkspaceId
        } : {} )
      }
    }
  }
  tags: {}
}

output controlPlaneFQDN string = aksCluster.properties.fqdn
output kubeletObjectId string = aksCluster.properties.identityProfile.kubeletidentity.objectId
output omsAgentIdentObjectID string = aksCluster.properties.addonProfiles.omsAgent.identity.objectId
