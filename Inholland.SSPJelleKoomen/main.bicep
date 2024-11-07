resource storageAccount 'Microsoft.Storage/storageAccounts@2021-04-01' = {
  name: 'sspstorageaccount221'
  location: resourceGroup().location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
}

resource appServicePlan 'Microsoft.Web/serverfarms@2021-02-01' = {
  name: 'sspserviceplan112'
  location: resourceGroup().location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
}

resource functionApp 'Microsoft.Web/sites@2021-02-01' = {
  name: 'sspfunctionapp123'
  location: resourceGroup().location
  kind: 'functionapp'
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: storageAccount.properties.primaryEndpoints.blob
        }
      ]
    }
  }
}

resource startJobQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2021-04-01' = {
  name: '${storageAccount.name}/default/start-job-queue'
}

resource imageProcessingQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2021-04-01' = {
  name: '${storageAccount.name}/default/image-processing-queue'
}