[CmdletBinding()]
param
(
	#commands
	[Parameter(Mandatory=$true)]
	[ValidateSet("Deploy","Spin","Remove")]
	[string]$Command,

	 #package version
	 [Parameter(Mandatory = $true)]
	[ValidateSet("v1","v2")]
    [string]$packageVersion,

	 #Any 5 length prefix starting with an alphabet.
    [Parameter(Mandatory = $true)]
    [string]$deploymentPrefix,

	#global admin email
	[Parameter(Mandatory=$true)]
	[string]$globalAdminEmail,

	#tenant id
	[Parameter(Mandatory=$true)]
	[string]$tenantId,

	#subscription id
	[Parameter(Mandatory=$true)]
	[string]$subscriptionId,

	#tenant domain
	[Parameter(Mandatory=$true)]
	[string]$tenantDomain,

	#deployment password
	[Parameter(Mandatory=$true)]
	[string]$deploymentPassword,

	#location
	[Parameter(Mandatory=$false)]
	[string]$location='eastus'

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
		.\Deploy.ps1 -deploymentPrefix $deploymentPrefix -tenantId $tenantId -tenantDomain $tenantDomain -subscriptionId $subscriptionId -globalAdminUsername $globalAdminEmail -location $location -deploymentPassword $deploymentPassword -packageVersion $packageVersion
	}
	Spin
	{
		Write-Host "`nAdding Application version v2 to the Application Gateway Backend pool." -ForegroundColor Yellow 
	
		if (((Get-AzureRmContext).Subscription.Id -eq $null) -or ((Get-AzureRmContext).Subscription.Id -ne $subscriptionId)) {
			Login-AzureRmAccount -SubscriptionId $subscriptionId -TenantId $tenantId
		}
		$resourceGroupName =  "$deploymentPrefix-networking-rg"
		$appGateway = Get-AzureRmApplicationGateway -ResourceGroupName $resourceGroupName -Name "$deploymentPrefix-zdt-agw"
		$backendPool = Get-AzureRmApplicationGatewayBackendAddressPool -ApplicationGateway $appGateway -Name 'appGatewayBackendPool'
		$backendPool = Set-AzureRmApplicationGatewayBackendAddressPool -ApplicationGateway $appGateway -Name 'appGatewayBackendPool' -BackendIPAddresses "$deploymentPrefix-webapp-v1.azurewebsites.net","$deploymentPrefix-webapp-v2.azurewebsites.net"
		
		Set-AzureRmApplicationGateway -ApplicationGateway $appGateway

		Write-Host "`nSuccessfully added Application v2 in rotation" -ForegroundColor Yellow 

		Write-Host "`nDue to sticky sessions (cookie based affinity), Current users of application will continue experiencing version 1" -ForegroundColor Yellow 
		
		Write-Host "`nAnd new users navigating to the application will experience version 2" -ForegroundColor Yellow 
	}
	Remove
	{
		Write-Host "`nRemoving Application version v2 from Application Gateway Backend pool" -ForegroundColor Yellow 

		if (((Get-AzureRmContext).Subscription.Id -eq $null) -or ((Get-AzureRmContext).Subscription.Id -ne $subscriptionId)) {
			Login-AzureRmAccount -SubscriptionId $subscriptionId -TenantId $tenantId
		}
		$resourceGroupName =  "$deploymentPrefix-workload-$packageVersion-rg"
		$operationsResourceGroup = "$deploymentPrefix-networking-rg"
		$sites=@("$deploymentPrefix-webapp-v2.azurewebsites.net")
		$appGateway = Get-AzureRmApplicationGateway -ResourceGroupName $operationsResourceGroup -Name "$deploymentPrefix-zdt-agw"
		$backendPool = Get-AzureRmApplicationGatewayBackendAddressPool -ApplicationGateway $appGateway -Name 'appGatewayBackendPool'
		$backendPool = Set-AzureRmApplicationGatewayBackendAddressPool -ApplicationGateway $appGateway -BackendIPAddresses $sites -Name 'appGatewayBackendPool'
		Set-AzureRmApplicationGateway -ApplicationGateway $appGateway

		Write-Host "`nTraffic will now be routed to v2. Users of v1 will now start seeing v2." -ForegroundColor Yellow 

		Write-Host "`nRemoving Application version v1 resource group" -ForegroundColor Yellow 
		Remove-AzureRmResourceGroup -Name $resourceGroupName -Force

		Write-Host "`nSuccessfully removed Application version v1" -ForegroundColor Yellow 

		Write-Host "`nAll users will now experience application verion v2" -ForegroundColor Yellow 
		
	}
}
