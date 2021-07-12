param accessPolicies array
param skuName string = 'Standard'
param enabledForDeployment bool = false
param enabledForDiskEncryption bool = false
param enabledForTemplateDeployment bool = false
param enableSoftDelete bool = true
param softDeleteRetentionInDays int = 90
param location string
param Name string


resource symbolicname 'Microsoft.KeyVault/vaults@2019-09-01' = {
  name: Name
  location: location
  tags: {}
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: skuName
    }
    accessPolicies: [ for accessPolicy in accessPolicies: {
        tenantId: subscription().tenantId
        objectId: accessPolicy.objectId
        permissions: {
          keys: accessPolicy.keys
          secrets: accessPolicy.secrets
          certificates: accessPolicy.certs
        }
      }]
    enabledForDeployment: enabledForDeployment
    enabledForDiskEncryption: enabledForDiskEncryption
    enabledForTemplateDeployment: enabledForTemplateDeployment
    enableSoftDelete: enableSoftDelete
    softDeleteRetentionInDays: softDeleteRetentionInDays
    
  }
}
