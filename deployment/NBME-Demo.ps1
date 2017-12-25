[CmdletBinding()]
param
(
	#commands
	[Parameter(Mandatory=$true)]
	[ValidateSet("Deploy","Spin","remove")]
	[string]$Command,

	[Parameter(Mandatory=$true)]
	[string]$adminEmail,

	 #Any 5 length prefix starting with an alphabet.
    [Parameter(Mandatory = $true)]
    [string]$deploymentPrefix,

	 #package version
	 [Parameter(Mandatory = $true)]
	[ValidateSet("v1","v2")]
    [string]$packageVersion
)

if(!(Get-InstalledModule -Name 'AzureRM.Network' -ErrorAction SilentlyContinue))
{
	Write-Host 'Installing module AzureRM.Network'
	Install-Module 'AzureRM.Network'
}

switch($Command)
{

	Deploy
	{
		.\Deploy.ps1 -deploymentPrefix $deploymentPrefix -tenantId '2b781bdf-80f1-45f0-8806-e3ab1679a814' -tenantDomain 'pcidemoxoutlook.onmicrosoft.com' -subscriptionId '0e322896-86cf-4b55-95d9-0e6bdbec1c1c' -globalAdminUsername 'unmeshv@avyanconsulting.com' -location 'eastus' -deploymentPassword 'Hcbnt54%kQoNs62' -packageVersion $packageVersion
	}
	spin
	{
		$resourceGroupName =  "$deploymentPrefix-workload-$packageVersion"
		$webApps = "$deploymentPrefix-webapp-v1.azurewebsites.net","$deploymentPrefix-webapp-v2.azurewebsites.net"
		$appGateway = Get-AzureRmApplicationGateway -ResourceGroupName $resourceGroupName -Name "$deploymentPrefix-zdt-agw"
		$backendPool = Get-AzureRmApplicationGatewayBackendAddressPool -ApplicationGateway $appGateway -Name 'appGatewayBackendPool'
		$backendPool = Set-AzureRmApplicationGatewayBackendAddressPool -ApplicationGateway $appGateway -BackendIPAddresses ([String]::Join($webApps))
		
		Set-AzureRmApplicationGateway -ApplicationGateway $appGateway

	}
	remove
	{
		$resourceGroupName =  "$deploymentPrefix-workload-$packageVersion"
		$webApp2="$deploymentPrefix-webapp-v2.azurewebsites.net"
		$appGateway = Get-AzureRmApplicationGateway -ResourceGroupName $resourceGroupName -Name "$deploymentPrefix-zdt-agw"
		$backendPool = Get-AzureRmApplicationGatewayBackendAddressPool -ApplicationGateway $appGateway -Name 'appGatewayBackendPool'
		$backendPool = Set-AzureRmApplicationGatewayBackendAddressPool -ApplicationGateway $appGateway -BackendIPAddresses $webApp2
		
		Set-AzureRmApplicationGateway -ApplicationGateway $appGateway

		Remove-AzureRmResourceGroup -Name $resourceGroupName
	}

}
