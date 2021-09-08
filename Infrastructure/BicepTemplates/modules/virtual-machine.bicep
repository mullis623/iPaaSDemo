@description('Name of the Virtual Machine.')
param name string

@description('Admin Username for the Virtual Machine')
param adminUsername string

@description('Admin Password for the Virtual Machine')
@secure()
param adminPassword string

@description('Location for the Virtual Machine.')
param location string

@allowed([
  'Windows'
  'Linux'
])
@description('OS to be used by the Virtual Machine.')
param osType string

@description('Size of the Virtual Machine.')
param size string

@description('Sku used by the Virtual Machine.')
param sku string

@description('Publisher used by the Virtual Machine.')
param publisher string

@description('Offer used by the Virtual Machine.')
param offer string

@description('Size of OS Disk for the Virtual Machine.')
param osDiskSizeGB int
param osDiskType string
param dataDisks array
param certDetails object

param encryptionOperation string
param keyEncryptionAlgorithm string
param keyVaultURL string
param keyVaultResourceId string
param volumeType string

param licenseType string = ''
param patchMode string = 'Manual'
param enableAutomaticUpdates bool = false
param enableHotpatching bool = false
param provisionVMAgent bool = true
param disablePasswordAuth bool = false
//param keyPath string = ''
//param keyData string = ''

@description('Resource Group Name for VNet used by the Virtual Machine.')
param vnetRG_Name string

@description('Name of VNet used by the Virtual Machine.')
param vnetName string

@description('Subnet used by the Virtual Machine.')
param subnetName string

var subnetId = resourceId(vnetRG_Name, 'Microsoft.Network/virtualNetworks/subnets', vnetName, subnetName)

var secrets = [
  {
    sourceVault: {
      id: resourceId(certDetails.KV_RGName, 'Microsoft.KeyVault/vaults', certDetails.KV_Name)
    }
    vaultCertificates: [
      {
        certificateUrl: reference(resourceId(certDetails.KV_RGName, 'Microsoft.KeyVault/vaults/secrets', certDetails.KV_Name, certDetails.Name), '2018-02-14', 'Full').Properties.secretUriWithVersion
        certificateStore: 'My'
      }
    ]
  }
]

var windowsConfig = {
                      enableAutomaticUpdates: enableAutomaticUpdates
                      provisionVMAgent: provisionVMAgent
                      patchSettings: {
                        enableHotpatching: enableHotpatching
                        patchMode: patchMode
                      }
                    }

var linuxConfig = {
                    disablePasswordAuthentication: disablePasswordAuth
                    provisionVMAgent: provisionVMAgent
                  }
                      

resource networkInterface 'Microsoft.Network/networkInterfaces@2020-07-01' = {
  name: '${name}-nic'
  location: location
  properties: {
    ipConfigurations: [
      {
        name: '${name}-ipconfig1'
        properties: {
          subnet: {
            id: subnetId
          }
          privateIPAllocationMethod: 'Dynamic'
        }
      }
    ]
  }
  dependsOn: []
}

resource virtualMachine 'Microsoft.Compute/virtualMachines@2020-12-01' = {
  name: name
  location: location
  properties: {
    hardwareProfile: {
      vmSize: size
    }
    storageProfile: {
      osDisk: {
        createOption: 'FromImage'
        managedDisk: {
          storageAccountType: osDiskType
        }
        diskSizeGB: osDiskSizeGB
      }
      imageReference: {
        publisher: publisher
        offer: offer
        sku: sku
        version: 'latest'
      }
      dataDisks: [for (item,i) in dataDisks: {
        lun: item.lun
        createOption: item.createOption
        caching: item.caching
        writeAcceleratorEnabled: item.writeAcceleratorEnabled
        diskSizeGB: item.diskSizeGB
        managedDisk: {
          storageAccountType: item.sku
        }        
      }]
    }
    networkProfile: {
      networkInterfaces: [
        {
          id: networkInterface.id
        }
      ]
    }
    osProfile: {
      computerName: name
      adminUsername: adminUsername
      adminPassword: adminPassword
      windowsConfiguration: ((osType == 'Windows') ? windowsConfig : json('null'))
      linuxConfiguration: ((osType == 'Linux') ? linuxConfig : json('null'))
      secrets: (empty(certDetails) ? [] : secrets)
    }
    licenseType: ((length(licenseType) > 0) ? licenseType : json('null'))
    diagnosticsProfile: {
      bootDiagnostics: {
        enabled: true
      }
    }
  }
}

resource diskencryption 'Microsoft.Compute/virtualMachines/extensions@2020-12-01' = if(osType == 'Windows') {
  parent: virtualMachine
  name: ((osType == 'Windows') ? 'AzureDiskEncryption' : 'AzureDiskEncryptionForLinux')
  location: location
  properties: {
    publisher: 'Microsoft.Azure.Security'
    type: ((osType == 'Windows') ? 'AzureDiskEncryption' : 'AzureDiskEncryptionForLinux')
    typeHandlerVersion: ((osType == 'Windows') ? '2.2' : '1.1')
    autoUpgradeMinorVersion: true
    settings: {
      EncryptionOperation: encryptionOperation
      KeyEncryptionAlgorithm: keyEncryptionAlgorithm
      KeyVaultURL: keyVaultURL
      KeyVaultResourceId: keyVaultResourceId
      VolumeType: volumeType
    }
  }
}

output adminUsername string = adminUsername
