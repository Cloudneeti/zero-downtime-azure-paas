[CmdletBinding()]
param
(
	#commands
	[Parameter(Mandatory=$true)]
	[ValidateSet("Deploy","Ping","Spin","remove")]
	[string]$Command,

	[Parameter(Mandatory=$false)]
	[string]$adminEmail,

	 #Any 5 length prefix starting with an alphabet.
    [Parameter(Mandatory = $false)]
    [string]$deploymentPrefix,

	 #package version
	 [Parameter(Mandatory = $false)]
	[ValidateSet("v1","v2")]
    [string]$packageVersion
)

if(!(Get-InstalledModule -Name 'AzureRM.Network' -ErrorAction SilentlyContinue))
{
	Write-Host 'Installing module AzureRM.Network'
	Install-Module 'AzureRM.Network'
}

$scriptRoot = Split-Path $MyInvocation.MyCommand.Path

switch($Command)
{

	Deploy
	{
		.\Deploy.ps1 -deploymentPrefix $deploymentPrefix -tenantId '2b781bdf-80f1-45f0-8806-e3ab1679a814' -tenantDomain 'pcidemoxoutlook.onmicrosoft.com' -subscriptionId '0e322896-86cf-4b55-95d9-0e6bdbec1c1c' -globalAdminUsername 'unmeshv@avyanconsulting.com' -location 'eastus' -deploymentPassword 'Hcbnt54%kQoNs62' -packageVersion $packageVersion
	}
	Ping{
		$parameters = Get-Content "$ScriptRoot/templates/webtest.parameters.json" | ConvertFrom-Json

		New-AzureRmResourceGroupDeployment -Name "CreateWebTest" -ResourceGroupName "$($parameters.parameters.prefix.value)-operations-rg" -TemplateParameterFile "$ScriptRoot/templates/webtest.parameters.json" -TemplateFile "$ScriptRoot/templates/resources/microsoft.appinsights/webtest.json"
	}
	spin
	{
		$resourceGroupName =  "$deploymentPrefix-networking-rg"

		$appGateway = Get-AzureRmApplicationGateway -ResourceGroupName $resourceGroupName -Name "$deploymentPrefix-zdt-agw"
		$backendPool = Get-AzureRmApplicationGatewayBackendAddressPool -ApplicationGateway $appGateway -Name 'appGatewayBackendPool'
		$backendPool = Set-AzureRmApplicationGatewayBackendAddressPool -ApplicationGateway $appGateway -Name 'appGatewayBackendPool' -BackendIPAddresses "$deploymentPrefix-webapp-v1.azurewebsites.net","$deploymentPrefix-webapp-v2.azurewebsites.net"
		
		Set-AzureRmApplicationGateway -ApplicationGateway $appGateway

	}
	remove
	{
		$resourceGroupName =  "$deploymentPrefix-workload-$packageVersion-rg"
		$operationsResourceGroup = "$deploymentPrefix-networking-rg"
		$sites=@("$deploymentPrefix-webapp-v2.azurewebsites.net")
		$appGateway = Get-AzureRmApplicationGateway -ResourceGroupName $operationsResourceGroup -Name "$deploymentPrefix-zdt-agw"
		$backendPool = Get-AzureRmApplicationGatewayBackendAddressPool -ApplicationGateway $appGateway -Name 'appGatewayBackendPool'
		$backendPool = Set-AzureRmApplicationGatewayBackendAddressPool -ApplicationGateway $appGateway -BackendIPAddresses $sites
		
		Set-AzureRmApplicationGateway -ApplicationGateway $appGateway

		Remove-AzureRmResourceGroup -Name $resourceGroupName
	}

}
