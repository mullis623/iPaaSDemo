targetScope='subscription'

param Name string
param Location string

resource newRG 'Microsoft.Resources/resourceGroups@2021-01-01' = {
  name: Name
  location: Location
}
