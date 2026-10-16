# Deploy McpServer to Azure Container Apps

Only McpServer is containerized. McpClient/Web Agent is unchanged. The existing
stateless Streamable HTTP transport, tools, repositories, SQL Server provider,
and migrations remain in place. These instructions create Azure resources;
they do not run as part of the application or Docker build.

## Container configuration

| Setting | Value |
| --- | --- |
| Default container port | `8080`, HTTP, all interfaces |
| MCP endpoint | `/mcp` |
| Health endpoint | `/health`, HTTP 200 with `{"status":"Healthy"}` |
| Required configuration | `ConnectionStrings__AzureSql`, supplied using an ACA secret reference |
| Runtime environment | `ASPNETCORE_ENVIRONMENT=Production` (set in the image) |
| Native ASP.NET port setting | `ASPNETCORE_HTTP_PORTS=8080` (image default, overridable) |

Explicit `--urls` / `ASPNETCORE_URLS` takes precedence. Without an explicit URL,
the server uses `CONTAINER_APP_PORT` (provided by ACA), then `PORT` if present,
binding `0.0.0.0` on that port. Otherwise it respects ASP.NET's configured HTTP/HTTPS
ports; only local runs without any port configuration fall back to port 5000.
Hosting ports must be integers from 1 through 65535. If you override a URL, use an
all-interface binding such as `http://0.0.0.0:8080`, and match ACA's target port.
`EXPOSE 8080` is image metadata, not a fixed runtime port.

ACA provides `CONTAINER_APP_PORT` as documented in
[environment variables](https://learn.microsoft.com/en-us/azure/container-apps/environment-variables).

`/health` is a process health check, not a SQL connectivity or schema check.
Startup requires a configured connection string, but health and tool discovery
do not query the database. Azure SQL must already be reachable and have the
existing schema/migrations applied. No new startup migrations or SQL behavior
are introduced.

## Build and test locally

Run from the repository root with Docker Desktop using Linux containers:

```powershell
dotnet build McpServer/McpServer.csproj -c Release
docker build --platform linux/amd64 -t mcpserver:aca -f McpServer/Dockerfile McpServer
```

The **build context must be `McpServer`**. Its `.dockerignore` allows only the
project, source directories, and non-secret `appsettings.json`. User secrets,
environment files, local settings, build output, and McpClient are excluded.
The runtime stage contains published output only and runs as the .NET image's
non-root application user. Never add a real connection string to these files.

For a local test, supply `ConnectionStrings__AzureSql` in the current process
environment using your secure configuration mechanism; do not store it in a
tracked file or paste its value into shell history:

```powershell
docker run --rm --name mcpserver-local -p 8080:8080 -e ConnectionStrings__AzureSql mcpserver:aca
# In another terminal:
Invoke-RestMethod http://localhost:8080/health
```

The local MCP URL is `http://localhost:8080/mcp`. Docker copies the named
environment variable from the current shell into the container at runtime.
For another port, add `-e PORT=9090` and replace the mapping with `-p 9090:9090`.

## Azure prerequisites

Use a current Azure CLI with the Container Apps extension. Your operator account
needs resource creation, registry push, and role assignment permissions.
These commands use Azure CLI **from PowerShell**, not the Azure PowerShell cmdlets.

Have an existing Azure SQL database and an RBAC-enabled Azure Key Vault. In Key
Vault's portal, create a secret named `azure-sql` containing the existing SQL
connection string. Use TLS (`Encrypt=True;TrustServerCertificate=False`) and
least-privilege database access. Record the vault name, resource group, and secret
URI; no secret value is needed in the commands below.

Allow the ACA environment to reach SQL and Key Vault through your chosen firewall,
VNet, and private endpoint configuration. The commands below create a standard
ACA environment, not a private-network deployment. If your database/vault requires
private access, create or select a VNet-integrated ACA environment first and reuse
it rather than running the environment creation command. Avoid broadly enabling
SQL's "Allow Azure services" setting as a substitute for scoped network access.

## Exact Azure CLI steps

Run from the repository root. Replace the placeholders; ACR names are globally
unique lowercase alphanumeric names. The example deploys **internal ingress**
because the server currently has no authentication.

```powershell
az login
az account set --subscription "<subscription-id>"
az extension add --name containerapp --upgrade
az provider register --namespace Microsoft.App --wait
az provider register --namespace Microsoft.OperationalInsights --wait
az provider register --namespace Microsoft.ContainerRegistry --wait
az provider register --namespace Microsoft.ManagedIdentity --wait

$resourceGroup = "rg-mcp"
$location = "eastus"
$registry = "<globally-unique-acr-name>"
$environment = "mcp-environment"
$appName = "mcp-server"
$identityName = "mcp-server-identity"
$vaultName = "<existing-vault-name>"
$vaultResourceGroup = "<existing-vault-resource-group>"
$sqlSecretUri = "https://${vaultName}.vault.azure.net/secrets/azure-sql"
$tag = "v1" # Use a new immutable tag for every deployment.
$targetPort = 8080

az group create --name $resourceGroup --location $location --output none
az acr create --name $registry --resource-group $resourceGroup --sku Basic --role-assignment-mode rbac --admin-enabled false --output none
az acr config authentication-as-arm update --registry $registry --status enabled --output none
az containerapp env create --name $environment --resource-group $resourceGroup --location $location --output none
az identity create --name $identityName --resource-group $resourceGroup --output none

$registryId = az acr show --name $registry --query id --output tsv
$registryHost = az acr show --name $registry --query loginServer --output tsv
$identityId = az identity show --name $identityName --resource-group $resourceGroup --query id --output tsv
$principalId = az identity show --name $identityName --resource-group $resourceGroup --query principalId --output tsv
$vaultId = az keyvault show --name $vaultName --resource-group $vaultResourceGroup --query id --output tsv

az role assignment create --assignee-object-id $principalId --assignee-principal-type ServicePrincipal --role AcrPull --scope $registryId --output none
az role assignment create --assignee-object-id $principalId --assignee-principal-type ServicePrincipal --role "Key Vault Secrets User" --scope $vaultId --output none

# Build and push locally. Requires Docker and registry push permissions.
az acr login --name $registry
$image = "${registryHost}/mcpserver:${tag}"
docker build --platform linux/amd64 -t $image -f McpServer/Dockerfile McpServer
docker push $image

# Allow time for new role assignments to propagate before creating the app.
az containerapp create `
  --name $appName --resource-group $resourceGroup --environment $environment `
  --image $image --user-assigned $identityId `
  --registry-server $registryHost --registry-identity $identityId `
  --ingress internal --transport http --target-port $targetPort --allow-insecure false `
  --cpu 0.5 --memory 1.0Gi --min-replicas 1 --max-replicas 3 `
  --secrets "azure-sql=keyvaultref:${sqlSecretUri},identityref:${identityId}" `
  --env-vars "ConnectionStrings__AzureSql=secretref:azure-sql" "ASPNETCORE_ENVIRONMENT=Production" "ASPNETCORE_HTTP_PORTS=${targetPort}" `
  --output none

$fqdn = az containerapp show --name $appName --resource-group $resourceGroup --query properties.configuration.ingress.fqdn --output tsv
"Health URL: https://${fqdn}/health"
"MCP URL: https://${fqdn}/mcp"
az containerapp logs show --name $appName --resource-group $resourceGroup --type console --tail 50
```

Stop on any failed command. If role propagation causes a registry or Key Vault
authorization error, wait and retry the failed deployment step. The identity
authenticates registry pulls and Key Vault access; it does not change SQL authentication.
The connection string continues to determine SQL authentication.

Without local Docker, replace the local login/build/push commands with:

```powershell
$image = "${registryHost}/mcpserver:${tag}"
az acr build --registry $registry --image "mcpserver:${tag}" --platform linux/amd64 --file Dockerfile McpServer
```

This uses [managed identity image pulls](https://learn.microsoft.com/en-us/azure/container-apps/managed-identity-image-pull)
and [Key Vault secret references](https://learn.microsoft.com/en-us/azure/container-apps/manage-secrets),
so registry passwords and SQL secret values are absent from the deployment commands.

## Health probes and verification

In the Azure portal, open the app, then **Revisions and replicas > Create new
revision**, select the container, and configure HTTP health probes on `/health`
using the target port (`8080` here). Use a startup probe (period 5 seconds, failure
threshold 30), liveness probe (period 10 seconds, failure threshold 3), and readiness
probe (period 5 seconds, failure threshold 3). Save and activate the revision.
Use a larger startup allowance if measured startup needs it. See
[ACA health probes](https://learn.microsoft.com/en-us/azure/container-apps/health-probes).

From another app in the same ACA environment, request `https://<fqdn>/health`, then
connect an MCP client to `https://<fqdn>/mcp`, list tools, and call `get_greeting`.
Verify SQL-backed tools separately against the configured database. Internal
ingress cannot be tested directly from your laptop over the public internet.

To deploy an update, build/push a new tag and run:

```powershell
az containerapp update --name $appName --resource-group $resourceGroup --image $image --output none
```

## Before public exposure

The current tools return customer and order data without authentication or
per-user authorization. HTTPS alone does not protect that data. Configure
MCP-compatible token authentication and authorization at the app or a gateway,
plus request limits and abuse protection, before exposing ingress. Ensure a
gateway cannot be bypassed by calling the ACA hostname directly. This work does
not add authentication or modify the Web Agent to send tokens.

Only after those controls are in place, enable external ingress:

```powershell
az containerapp ingress enable --name $appName --resource-group $resourceGroup --type external --target-port $targetPort --transport http
```

ACA terminates public HTTPS; HTTP inside the container is intentional. Keep
insecure ingress disabled. Restrict database/vault access, use least-privilege
identities, rotate secrets, and avoid logging secrets or customer data. Restrict
accepted hosts/origins as appropriate to the gateway and MCP clients; do not add
wildcard CORS. Preserve MCP headers and streamed responses in any proxy, and account
for ingress timeouts. `/health` reports no secrets or customer data.
