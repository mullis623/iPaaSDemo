@minLength(1)
param adminEmail string

@minLength(1)
param adminName string

@allowed([
  'Developer'
  'Standard'
  'Premium'
])
@description('The pricing tier of this API Management service')
param sku string = 'Developer'

@description('The instance size of this API Management service.')
param skuCount int = 1
param apiManagementName string
param appInsightsKey string

var apiManagementServiceName_var = 'api-${apiManagementName}'

resource apiManagementServiceName 'Microsoft.ApiManagement/service@2020-06-01-preview' = {
  name: apiManagementServiceName_var
  location: resourceGroup().location
  tags: {}
  sku: {
    name: sku
    capacity: skuCount
  }
  properties: {
    publisherEmail: adminEmail
    publisherName: adminName
  }
}

resource apiManagementServiceName_iPaaS_Demo_Functions 'Microsoft.ApiManagement/service/apis@2020-06-01-preview' = {
  name: '${apiManagementServiceName.name}/iPaaS-Demo-Functions'
  properties: {
    format: 'openapi-link'
    value: 'https://raw.githubusercontent.com/mullis623/iPaaSDemo/main/Infrastructure/ArmTemplates/apim.openapi.yaml'
    path: 'iPaaS-Demo-Functions'
    displayName: 'iPaaS-Demo-Functions'
    protocols: [
      'https'
    ]
  }
}

resource apiManagementServiceName_iPaaS_Demo_Functions_new_request_policy 'Microsoft.ApiManagement/service/apis/operations/policies@2020-06-01-preview' = {
  name: '${apiManagementServiceName_var}/iPaaS-Demo-Functions/new-request/policy'
  properties: {
    value: '<policies>\r\n  <inbound>\r\n    <base />\r\n    <return-response>\r\n      <set-status code="200" reason="OK" />\r\n      <set-header name="Content-Type" exists-action="override">\r\n        <value>application/json</value>\r\n      </set-header>\r\n      <set-body>@("{\\"ServiceTicketNo\\": \\"" + System.Guid.NewGuid().ToString() + "\\", \\"status\\": \\"OK\\"}")</set-body>\r\n    </return-response>\r\n  </inbound>\r\n  <backend>\r\n    <base />\r\n  </backend>\r\n  <outbound>\r\n    <base />\r\n  </outbound>\r\n  <on-error>\r\n    <base />\r\n  </on-error>\r\n</policies>'
    format: 'xml'
  }
  dependsOn: [
    apiManagementServiceName_iPaaS_Demo_Functions
    apiManagementServiceName
  ]
}

resource apiManagementServiceName_iPaaS_Demo_Functions_get_request_policy 'Microsoft.ApiManagement/service/apis/operations/policies@2020-06-01-preview' = {
  name: '${apiManagementServiceName_var}/iPaaS-Demo-Functions/get-request/policy'
  properties: {
    value: '<policies>\r\n  <inbound>\r\n    <base />\r\n    <mock-response status-code="200" content-type="application/json" />\r\n  </inbound>\r\n  <backend>\r\n    <base />\r\n  </backend>\r\n  <outbound>\r\n    <base />\r\n  </outbound>\r\n  <on-error>\r\n    <base />\r\n  </on-error>\r\n</policies>'
    format: 'xml'
  }
  dependsOn: [
    apiManagementServiceName_iPaaS_Demo_Functions
    apiManagementServiceName
  ]
}

resource apiManagementServiceName_logger 'Microsoft.ApiManagement/service/loggers@2020-06-01-preview' = {
  name: '${apiManagementServiceName.name}/logger'
  properties: {
    loggerType: 'applicationInsights'
    description: 'Application Insights integration'
    credentials: {
      instrumentationKey: appInsightsKey
    }
    isBuffered: true
  }
}

resource apiManagementServiceName_applicationinsights 'Microsoft.ApiManagement/service/diagnostics@2020-06-01-preview' = {
  name: '${apiManagementServiceName.name}/applicationinsights'
  properties: {
    alwaysLog: 'allErrors'
    httpCorrelationProtocol: 'Legacy'
    logClientIp: true
    sampling: {
      samplingType: 'fixed'
      percentage: 100
    }
    loggerId: '/loggers/logger'
  }
  dependsOn: [
    apiManagementServiceName_logger
  ]
}

output url string = 'https://${apiManagementServiceName_var}.azure-api.net/iPaaS-Demo-Functions/'
output key string = reference(resourceId('Microsoft.ApiManagement/service/subscriptions', apiManagementServiceName_var, 'master'), '2019-01-01').primaryKey
