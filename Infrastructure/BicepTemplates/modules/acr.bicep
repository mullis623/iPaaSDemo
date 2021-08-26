param location string

@allowed([
  'Basic'
  'Standard'
  'Premium'
])
@description('The sku name for the container registry.')
param acrSkuName string

param Name string

param adminUserEnabled bool = true

param publicNetworkAccess string

param networkRuleBypassOption string

param zoneRedundancy string

param anonPullEnabled bool

param attachToAKS bool = false

param aks_acr_guid string = 'null'

param aksKubeletObjectId string = 'null'

var acrPullDefId = '/subscriptions/${subscription().subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/7f951dda-4ed3-4680-a7ca-43fe172d538d'

resource registry 'Microsoft.ContainerRegistry/registries@2020-11-01-preview' = {
  name: Name
  location: location
  sku: {
    name: acrSkuName
  }
  properties: {
    adminUserEnabled: adminUserEnabled
    publicNetworkAccess: publicNetworkAccess
    networkRuleBypassOptions: networkRuleBypassOption
    zoneRedundancy: zoneRedundancy
    anonymousPullEnabled: anonPullEnabled
  }
}

resource attachACR 'Microsoft.Authorization/roleAssignments@2021-04-01-preview' = if(attachToAKS) {
  name: aks_acr_guid
  scope: registry
  properties: {
    principalId: aksKubeletObjectId
    principalType: 'ServicePrincipal'
    roleDefinitionId: acrPullDefId
  }
}


output acrName string = registry.name
