@description('The location of AKS and ACR resources.')
param location string = resourceGroup().location

@description('List of Deployment Settings used by Azure Kubernetes.')
param aksSettings object

@description('List of Deployment Settings used by the container registry.')
param acrSettings object

@description('List of Deployment Settings used by Log Analytics.')
param laSettings object

@description('Details of the Key Vault used by AKS for secrets')
param aksKeyVault object

module logAnalytics 'modules/loganalytics.bicep' = {
  name: 'logAnalytics(${laSettings.Name})'
  params:{
    Name: laSettings.Name
    location: location
    skuName: laSettings.skuName
    retentionInDays: laSettings.retentionInDays
  }
}

module aksCluster 'modules/aks.bicep' = {
  name: 'aksCluster(${aksSettings.Name})'
  params: {
    Name: aksSettings.Name
    location: location
    identityType: aksSettings.identityType
    kubernetesVersion: aksSettings.kubernetesVersion
    dnsPrefix: aksSettings.dnsPrefix
    aksNodeRGName: aksSettings.aksNodeRGName
    enableRBAC: aksSettings.enableRBAC
    enableAADRBAC: aksSettings.enableAADRBAC
    enableManagedAAD: aksSettings.enableManagedAAD
    adminGroupObjectIDs: aksSettings.adminGroupObjectIDs
    enablePrivateCluster: aksSettings.enablePrivateCluster
    enableAzurePolicy: aksSettings.enableAzurePolicy
    networkPlugin: aksSettings.networkPlugin
    networkPolicy: aksSettings.networkPolicy
    loadBalancerSku: aksSettings.loadBalancerSku
    enableOmsAgent: aksSettings.enableOmsAgent
    omsWorkspaceId: logAnalytics.outputs.resourceId
    agentPoolProfiles: aksSettings.agentPoolProfiles
  }
}


module registry 'modules/acr.bicep' = {
  name: 'registry(${acrSettings.Name})'
  params: {
    Name: acrSettings.Name
    location: location
    acrSkuName: acrSettings.acrSkuName
    adminUserEnabled: acrSettings.adminUserEnabled
    publicNetworkAccess: acrSettings.publicNetworkAccess
    networkRuleBypassOption: acrSettings.networkRuleBypassOption
    zoneRedundancy: acrSettings.zoneRedundancy
    anonPullEnabled: acrSettings.anonPullEnabled
    attachToAKS: acrSettings.attachToAKS
    aks_acr_guid: acrSettings.aks_acr_guid
    aksKubeletObjectId: aksCluster.outputs.kubeletObjectId
  }
}

module linkLAtoAKS 'modules/linkLAtoAKS.bicep' = {
  name: 'linkLAtoAKS(${laSettings.Name})'
  params: {
    location: location
    la_aks_guid: laSettings.la_aks_guid
    omsWorkspaceId: logAnalytics.outputs.resourceId
    omsAgentIdentObjectId: aksCluster.outputs.omsAgentIdentObjectID
    laWorkspaceName: laSettings.Name
  }
}

module MIOperatorNodeRG 'modules/rgroleassignments.bicep' = {
  name: 'MIOperatorNodeRG'
  scope: resourceGroup(aksSettings.aksNodeRGName)
  params:{
    principalId: aksCluster.outputs.kubeletObjectId
    roleName: 'mioperator'
    roleDefGuid: 'f1a07417-d97a-45cb-824c-7a7467783830'
  }
}

module MIOperatorKVRG 'modules/rgroleassignments.bicep' = {
  name: 'MIOperatorKVRG'
  scope: resourceGroup()
  params:{
    principalId: aksCluster.outputs.kubeletObjectId
    roleName: 'mioperator'
    roleDefGuid: 'f1a07417-d97a-45cb-824c-7a7467783830'
  }
}

module VMOperatorNodeRG 'modules/rgroleassignments.bicep' = {
  name: 'VMOperatorNodeRG'
  scope: resourceGroup(aksSettings.aksNodeRGName)
  params:{
    principalId: aksCluster.outputs.kubeletObjectId
    roleName: 'vmoperator'
    roleDefGuid: '9980e02c-c2be-4d73-94e8-173b1dc7cf3c'
  }
}

module keyVault 'modules/keyvault.bicep' = {
  name: 'aksKeyVault(${aksKeyVault.Name})'
  params: {
    Name: aksKeyVault.Name
    location: location
    accessPolicies: aksKeyVault.accessPolicies
    skuName: aksKeyVault.skuName
    enabledForTemplateDeployment: aksKeyVault.enabledForTemplateDeployment
  }
}

output controlPlaneFQDN string = aksCluster.outputs.controlPlaneFQDN
