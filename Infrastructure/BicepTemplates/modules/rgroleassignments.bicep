param principalId string
param roleName string
param roleDefGuid string

resource MIOperatorRole 'Microsoft.Authorization/roleAssignments@2021-04-01-preview' = {
  name: '${guid(resourceGroup().name, roleName)}'
  properties: {
    principalId: principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: '/subscriptions/${subscription().subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/${roleDefGuid}'
  }
}
