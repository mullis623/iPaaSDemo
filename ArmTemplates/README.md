# Overview
Directory contains a linked ARM template to deploy everything except for API Management (WIP). 
Linked ARM templates cannot be deployed locally -- the linked templates must be hosted on a public URL. 
We can either use GitHub directly (if/when the repo isn't private) or Azure Storage. Right now the templates are hosted in my own Azure Storage and are publicly accessible.

# Deployment
Few options:
1. GitHub Actions with a separate Environment for each contributor
2. Deploy via az cli or PowerShell

# Parameters
Template takes three parameters:
1. Environment name (i.e. dev, test, prod)
2. Resource prefix (i.e. your initials or something unique)
3. Subscription ID

Environment and resource prefix are used as part of the infrastructure name in order to generate unique names.

## az cli sample
`az deployment group create --name 2020-01-15T0839 --resource-group rg-ipaas-dev --template-file deployTemplate.json`
