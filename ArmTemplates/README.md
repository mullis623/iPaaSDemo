# Overview
Directory contains a linked ARM template to deploy everything except for API Management (WIP). 
Linked ARM templates cannot be deployed locally -- the linked templates must be hosted on a public URL. 
We can either use GitHub directly (if/when the repo isn't private) or Azure Storage. Right now the templates are hosted in my own Azure Storage and are publicly accessible.

# Deployment
Few options:
1. GitHub Actions with a separate Environment for each contributor
2. Deploy via az cli or PowerShell
3. Click the button

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fmullis623%2FiPaaSDemo%2Fmain%2FArmTemplates%2FdeployTemplate.json)
<a href="http://armviz.io/#/?load=https%3A%2F%2Fpythondjangodemo.blob.core.windows.net%2Fazuretemplates%2FdeployTemplate.json" target="_blank">
    <img src="https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/1-CONTRIBUTION-GUIDE/images/visualizebutton.png"/>
</a>


# Parameters
Template takes three parameters:
1. Environment name (i.e. dev, test, prod)
2. Resource prefix (i.e. your initials or something unique)
3. Subscription ID

Environment and resource prefix are used as part of the infrastructure name in order to generate unique names.

# Manual Setup
1. Configure the Office 365 API Connector for the Logic App
2. Configure the `CustomVisionIteration`, `CustomVisionPredictionKey`, and `CustomVisionRootUrl` App Settings in Function App to use Shaun's Cognitive Services.
