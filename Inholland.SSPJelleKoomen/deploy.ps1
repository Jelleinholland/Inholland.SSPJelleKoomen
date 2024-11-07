param (
    [string]$resourceGroupName = "ssprecourcegroup",
    [string]$location = "WestEurope",
    [string]$templateFile = "main.bicep"
)

# Create the resource group if it doesn't exist
az group create --name $resourceGroupName --location $location

# Clean up previous build outputs
if (Test-Path ./publish) {
    Remove-Item -Recurse -Force ./publish
}
if (Test-Path ./publish.zip) {
    Remove-Item -Force ./publish.zip
}
dotnet build
dotnet publish -o ./publish

# Create a zip file of the published output, overwriting if it already exists
Compress-Archive -Path ./publish/* -DestinationPath ./publish.zip -Force

# Deploy the resources using Bicep
az deployment group create --resource-group $resourceGroupName --template-file $templateFile

# Set application settings for the Function App
az functionapp config appsettings set --name sspfunctionapp123 --resource-group $resourceGroupName --settings "AzureWebJobsStorage=<your_azure_storage_connection_string>" "UnsplashApiKey=<your_unsplash_api_key>"

# Deploy the function app
az functionapp deployment source config-zip -g $resourceGroupName -n sspfunctionapp123 --src ./publish.zip