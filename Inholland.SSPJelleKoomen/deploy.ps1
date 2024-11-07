param (
    [string]$resourceGroupName = "ssprecourcegroup",
    [string]$location = "WestEurope",
    [string]$templateFile = "main.bicep"
)

# Create the resource group if it doesn't exist
az group create --name $resourceGroupName --location $location

# Build the project
dotnet build

# Publish the project
dotnet publish -o ./publish

# Create a zip file of the published output
Compress-Archive -Path ./publish/* -DestinationPath ./publish.zip

# Deploy the resources using Bicep
az deployment group create --resource-group $resourceGroupName --template-file $templateFile

# Deploy the function app
az functionapp deployment source config-zip -g $resourceGroupName -n sspfunctionapp123 --src ./publish.zip