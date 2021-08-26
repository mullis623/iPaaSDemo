@description('The name of the workspace.')
param Name string

@description('Azure Region where workspace will be created.')
param location string

@allowed([
  'Free'
  'Standard'
  'Premium'
  'PerNode'
  'PerGB2018'
  'Standalone'
  'CapacityReservation'
])
@description('The name of the SKU.')
param skuName string = 'PerGB2018'

@minValue(30)
@maxValue(730)
@description('The workspace data retention in days.')
param retentionInDays int = 30

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2020-08-01' = {
  name: Name
  tags: {}
  location: location
  properties: {
    sku: {
      name: skuName
    }
    retentionInDays: retentionInDays
  }
}

output resourceId string = logAnalytics.id
