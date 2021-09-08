@description('Details required for VM Disk Encryption')
param DiskEncryptionDetails object

@description('Admin Password for the Virtual Machines.')
@secure()
param adminPassword string

@description('Location for all resources.')
param location string = resourceGroup().location

@description('Details of the Virtual Machines required by the BizTalk')
param VirtualMachines array

@description('Details of the Virtual Machine Virtual Network')
param VM_VnetDetails object

module vm 'modules/virtual-machine.bicep' = [for currentVM in VirtualMachines: {
  name: '${currentVM.name}-Deploy'
  params: {
    name: currentVM.name
    adminUsername: currentVM.adminUsername
    adminPassword: adminPassword
    location: location
    osType: currentVM.osType
    size: currentVM.size
    sku: currentVM.sku
    publisher: currentVM.publisher
    offer: currentVM.offer
    osDiskSizeGB: currentVM.osDiskSizeGB
    osDiskType: currentVM.osDiskType
    dataDisks: currentVM.dataDisks
    certDetails: currentVM.certs
    encryptionOperation: DiskEncryptionDetails.encryptionOperation
    keyEncryptionAlgorithm: DiskEncryptionDetails.keyEncryptionAlgorithm
    keyVaultURL: DiskEncryptionDetails.keyVaultURL
    keyVaultResourceId: DiskEncryptionDetails.keyVaultResourceId
    volumeType: DiskEncryptionDetails.volumeType
    vnetRG_Name: VM_VnetDetails.RGName
    vnetName: VM_VnetDetails.vnetName
    subnetName: VM_VnetDetails.subnetName
  }
}]
