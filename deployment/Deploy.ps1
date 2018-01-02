[CmdletBinding()]
param
(
    #Any 5 length prefix starting with an alphabet.
    [Parameter(Mandatory = $true, 
    ParameterSetName = "Deployment", 
    Position = 1)]
    [Parameter(Mandatory = $true, 
    ParameterSetName = "CleanUp", 
    Position = 1)]
    [Alias("prefix")]
    [ValidateLength(1,5)]
    [ValidatePattern("[a-z][a-z0-9]")]
    [string]$deploymentPrefix,

    #Azure AD Tenant Id.
    [Parameter(Mandatory = $true,
    ParameterSetName = "Deployment",
    Position = 2)]
    [Parameter(Mandatory = $true, 
    ParameterSetName = "CleanUp", 
    Position = 2)]
    [guid]$tenantId,

    #Azure Subscription Id.
    [Parameter(Mandatory = $true,
    ParameterSetName = "Deployment",
    Position = 3)]
    [Parameter(Mandatory = $true, 
    ParameterSetName = "CleanUp", 
    Position = 3)]
	[Alias("subId")]
    [guid]$subscriptionId,

    #Azure Tenant Domain name.
    [Parameter(Mandatory = $true,
    ParameterSetName = "Deployment",
    Position = 4)]
    [Parameter(Mandatory = $true, 
    ParameterSetName = "CleanUp", 
    Position = 4)]
    [Alias("domain")]
    [ValidatePattern("[.]")]
    [string]$tenantDomain,

    #Subcription GlobalAdministrator Username.
    [Parameter(Mandatory = $true,
    ParameterSetName = "Deployment",
    Position = 5)]
    [Parameter(Mandatory = $true, 
    ParameterSetName = "CleanUp", 
    Position = 5)]
	[Alias("userName")]
    [string]$globalAdminUsername,

    #GlobalAdministrator Password in a plain text.
    [Parameter(Mandatory = $true,
    ParameterSetName = "Deployment",
    Position = 6)]
    [Parameter(Mandatory = $true, 
    ParameterSetName = "CleanUp", 
    Position = 6)]
	[Alias("password")]
    [securestring]$globalAdminPassword,

    #Location. Default is westcentralus.
    [Parameter(Mandatory = $false,
    ParameterSetName = "Deployment",
    Position = 7)]
    [Parameter(Mandatory = $false, 
    ParameterSetName = "CleanUp", 
    Position = 7)]
    [ValidateSet(
        "eastasia",
        "southeastasia",
        "centralus",
        "eastus",
        "eastus2",
        "westus",
        "northcentralus",
        "southcentralus",
        "northeurope",
        "westeurope",
        "japanwest",
        "japaneast",
        "brazilsouth",
        "australiaeast",
        "australiasoutheast",
        "southindia",
        "centralindia",
        "westindia",
        "canadacentral",
        "canadaeast",
        "uksouth",
        "ukwest",
        "westcentralus",
        "westus2",
        "koreacentral",
        "koreasouth"
    )]
	[Alias("loc")]
    [string]$location = "westcentralus",

    #[Optional] Strong deployment password. Auto-generates password if not provided.
    [Parameter(Mandatory = $false,
    ParameterSetName = "Deployment",
    Position = 8)]
    [Alias("dpwd")]
    [string]$deploymentPassword = 'null',

    #package version
    [Parameter(Mandatory = $true,
    ParameterSetName = "Deployment",
    Position = 9)]
	[ValidateSet("v1","v2")]
    [string]$packageVersion,

    #Switch to install required modules.
    [Parameter(Mandatory = $true,
    ParameterSetName = "InstallModules")]
    [switch]$installModules,

    #Switch to cleanup deployment resources from the subscription.
    [Parameter(Mandatory = $true, 
    ParameterSetName = "CleanUp", 
    Position = 8)]
    [switch]$clearDeployment

)
Process {
    ### Manage Session Configuration
    $Host.UI.RawUI.WindowTitle = "NBME - Zero DownTime Deployment"
    $ErrorActionPreference = 'Stop'
    $WarningPreference = 'Continue'
    Set-StrictMode -Version 3
    $scriptRoot = Split-Path $MyInvocation.MyCommand.Path

    ### Create Output  folder to store logs, deploymentoutputs etc.
    if(! (Test-Path -Path "$(Split-Path $MyInvocation.MyCommand.Path)\output")) {
        New-Item -Path $(Split-Path $MyInvocation.MyCommand.Path) -Name 'output' -ItemType Directory
    }
    $outputFolderPath = "$(Split-Path $MyInvocation.MyCommand.Path)\output"

    Start-Transcript -OutputDirectory $outputFolderPath

    ### Import custom powershell functions for deployment.
    . $scriptroot\scripts\pshscripts\PshFunctions.ps1

    ### Install required powershell modules
    $requiredModules=@{
        'AzureRM' = '5.1.1';
        'AzureAD' = '2.0.0.131'
    }

    if ($installModules) {
        log "Trying to install listed modules.."
        $requiredModules
        Install-RequiredModules -moduleNames $requiredModules
        log "All the required modules are now installed. You can now re-run the script without 'installModules' switch." Cyan
        Break
    }

    # Remove all Azure credentials, account, and subscription information.
    Clear-AzureRmContext -Scope CurrentUser -Force

    ### Converting deployment prefix to lowercase
    $deploymentprefix = $deploymentprefix.ToLower()

    ### Actors 
    $actors = @('Alex_SiteAdmin','Kim_NetworkAdmin')

    ### Create PSCredential Object for GlobalAdmin Account
    $credential = New-Object System.Management.Automation.PSCredential ($globalAdminUsername, $globalAdminPassword)

    ### Connect to AzureRM using Global Administrator Account
    log "Connecting to AzureRM Subscription $subscriptionId using Global Administrator Account."
    ### Create PSCredential Object for GlobalAdmin Account
    $credential = New-Object System.Management.Automation.PSCredential ($globalAdminUsername, $globalAdminPassword)
    $globalAdminContext = Login-AzureRmAccount -Credential $credential -Subscription $subscriptionId -ErrorAction SilentlyContinue
    if($globalAdminContext -ne $null){
        log "Connection using Global Administrator Account was successful." Green
    }
    Else{
        logerror
        log "Failed connecting to Azure using Global Administrator Account." Red
        Break
    }

    # components required for creating resourcegroup
    $components = @("artifacts","workload-$packageVersion","networking", "operations", "backend")

    if ($clearDeployment) {
        try {
            log "Looking for Resources to Delete.." Magenta
            log "List of deployment resources for deletion" -displaywithouttimestamp

            #List The Resource Group
            $resourceGroupList =@()
            $components | ForEach-Object { 
                $resourceGroupList += (($deploymentPrefix,$_,'rg') -join '-')
            }

            log "Resource Groups: " Cyan -displaywithouttimestamp
            $resourceGroupList | ForEach-Object {
                $resourceGroupName = $_
                $resourceGroupObj = Get-AzureRmResourceGroup -Name $resourceGroupName -ErrorAction SilentlyContinue
                if($resourceGroupObj-ne $null)
                {
                    log "$($resourceGroupObj.ResourceGroupName)." -displaywithouttimestamp -nonewline
                    $rgCount = 1 
                }
                else 
                {
                    $rgCount = 0
                    log "$resourceGroupName Resource group does not exist." -displaywithouttimestamp
                }
            }

            #List the Service principal
            log "Service Principals: " Cyan -displaywithouttimestamp
            $servicePrincipalObj = Get-AzureRmADServicePrincipal -SearchString $deploymentPrefix -ErrorAction SilentlyContinue
            if ($servicePrincipalObj -ne $null)
            {
                $servicePrincipalObj | ForEach-Object {
                    log "$($_.DisplayName)" -displaywithouttimestamp -nonewline
                }
            }
            else{ 
                log "Service Principal does not exist for '$deploymentPrefix' prefix" Yellow
            }

            #List the AD Application
            $adApplicationObj = Get-AzureRmADApplication -DisplayNameStartWith "$deploymentPrefix Azure HealthCare LOS Sample"
            log "AD Applications: " Cyan -displaywithouttimestamp
            if($adApplicationObj -ne $null){
                log "$($adApplicationObj.DisplayName)" -displaywithouttimestamp -nonewline
            }
            Else{
                log "AD Application does not exist for '$deploymentPrefix' prefix" Yellow -displaywithouttimestamp
            }

            #List the AD Users
            log "AD Users: " Cyan -displaywithouttimestamp
            foreach ($actor in $actors) {
                $upn = Get-AzureRmADUser -SearchString $actor
                $fullUpn = $actor + '@' + $tenantDomain
                if ($upn -ne $null )
                {
                    log "$fullUpn" -displaywithouttimestamp -nonewline
                }
            }
            if ($upn -eq $null)
            {
                log "No user exist" Yellow -displaywithouttimestamp
            }
            Write-Host ""
            # Remove deployment resources
            $message = "Do you want to DELETE above listed Deployment Resources ?"
            $yes = New-Object System.Management.Automation.Host.ChoiceDescription "&Yes", `
            "Deletes Deployment Resources"
            $no = New-Object System.Management.Automation.Host.ChoiceDescription "&No", `
            "Skips Deployment Resources Deletion"
            $options = [System.Management.Automation.Host.ChoiceDescription[]]($yes, $no)
            $result = $host.ui.PromptForChoice($null, $message, $options, 0)
            switch ($result){
                0 {
                    # Remove ResourceGroups
                    if ($rgCount -eq 1)
                    {
                    $resourceGroupList | ForEach-Object { 
                        $resourceGroupName = $_
                        Get-AzureRmResourceGroup -Name $resourceGroupName | Out-Null
                            log "Deleting Resource group $resourceGroupName" Yellow -displaywithouttimestamp
                            Remove-AzureRmResourceGroup -Name $resourceGroupName -Force| Out-Null
                            log "ResourceGroup $resourceGroupName was deleted successfully" Yellow -displaywithouttimestamp
                        }
                    }

                    # Remove Service Principal
                    if ($servicePrincipals = Get-AzureRmADServicePrincipal -SearchString $deploymentPrefix) {
                        $servicePrincipals | ForEach-Object {
                            log "Removing Service Principal - $($_.DisplayName)."
                            Remove-AzureRmADServicePrincipal -ObjectId $_.Id -Force
                            log "Service Principal - $($_.DisplayName) was removed successfully" Yellow -displaywithouttimestamp
                        }
                    }

                    # Remove Azure AD Users
                    
                    if ($upn -ne $null)
                    {
                        Write-Host "FOR DEVELOPMENT PURPOSE WE RECONFIRMING FOR AAD USERS DELETION." -ForegroundColor Magenta
                            # Prompt to remove AAD Users
                        $message = "Do you want to DELETE AAD users?"
                        $yes = New-Object System.Management.Automation.Host.ChoiceDescription "&Yes", `
                        "Deletes AAD users"
                        $no = New-Object System.Management.Automation.Host.ChoiceDescription "&No", `
                        "Skips AAD users Deletion"
                        $options = [System.Management.Automation.Host.ChoiceDescription[]]($yes, $no)
                        $result = $host.ui.PromptForChoice($null, $message, $options, 0)
                        switch ($result){
                            0 {
                        log "Removing Azure AAD User" Yellow -displaywithouttimestamp
                        foreach ($actor in $actors) {
                            try {
                                $upn = $actor + '@' + $tenantDomain
                                Get-AzureRmADUser -SearchString $upn
                                Remove-AzureRmADUser -UPNOrObjectId $upn -Force -ErrorAction SilentlyContinue
                                log "$upn was deleted successfully. " Yellow -displaywithouttimestamp
                            }
                            catch [System.Exception] {
                                logerror
                                Break
                            }
                        }
                    }
                    1 {
                        log "Skipped - AAD users Deletion." Cyan
                    }
                }
                    }
                    
                    #Remove AAD Application.
                    if($adApplicationObj)
                    {
                        log "Removing Azure AD Application - $deploymentPrefix Azure HealthCare LOS Sample." Yellow -displaywithouttimestamp
                        Get-AzureRmADApplication -DisplayNameStartWith "$deploymentPrefix Azure HealthCare LOS Sample" | Remove-AzureRmADApplication -Force
                        log "Azure AD Application - $deploymentPrefix Azure HealthCare LOS Sample deleted successfully" Yellow -displaywithouttimestamp
                    }
                    log "Resources cleared successfully." Magenta
                }
                1 {
                    log "Skipped - Resource Deletion." Cyan
                }
            }
        }
        catch {
            logerror
            Break
        }
    }
    else {
        ### Set Deployment password if not already set.
        if ($deploymentPassword -eq 'null') {
            log "Deployment password was not provided. Creating strong password for deployment."
            $deploymentPassword = New-RandomPassword
            log "Deployment password $deploymentPassword generated successfully."
        }

        ### Convert deploymentPasssword to SecureString.
        $secureDeploymentPassword = ConvertTo-SecureString $deploymentPassword -AsPlainText -Force

        ### Convert Service Administrator to plaintext
        $convertedServiceAdminPassword = $globalAdminPassword | ConvertFrom-SecureString 
        $securePassword = ConvertTo-SecureString $convertedServiceAdminPassword
        $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecurePassword)
        $plainServiceAdminPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

        ### Configure AAD User Accounts.
        log "Creating AAD account for solution actors using ServiceAdmin Account."
        try
        {
            log "Initiating separate powershell session for creating accounts."
            Start-Process Powershell -ArgumentList "-NoExit", "-WindowStyle Normal", "-ExecutionPolicy UnRestricted", ".\scripts\pshscripts\Configure-AADUsers.ps1 -tenantId $tenantId -subscriptionId $subscriptionId -tenantDomain $tenantDomain -globalAdminUsername $globalAdminUsername -globalAdminPassword $plainServiceAdminPassword -deploymentPassword '$deploymentPassword'"
        }
        catch [System.Exception]
        {
            logerror
            Break
        }

        log "Wait for AAD Users to be provisioned." Cyan
        Start-Sleep 15

        ### Register Resource provider.
        log "Register Resource Providers."
        try {
            $resourceProviders = @(
                "Microsoft.Storage",
                "Microsoft.Compute",
                "Microsoft.KeyVault",
                "Microsoft.Network",
                "Microsoft.Web",
                "Microsoft.Insights",
                "Microsoft.Security"
            )
            if($resourceProviders.length) {
                foreach($resourceProvider in $resourceProviders) {
                    RegisterRP($resourceProvider);
                }
            }
        }
        catch {
            logerror
            Break
        }

        ### Create Resource Group for deployment and assigning RBAC to users.
        $components | ForEach-Object { 
            $rgName = (($deploymentPrefix,$_,'rg') -join '-')
            log "Creating ResourceGroup $rgName at $location."
            New-AzureRmResourceGroup -Name $rgName -Location $location -Force -OutVariable $_
        }

        ### Assign Roles to the Users
        log "Assigning roles to the users."
        $rbactmp = [System.IO.Path]::GetTempFileName()
        $rbacData = Get-Content "$scriptroot\scripts\jsonscripts\subscription.roleassignments.json" | ConvertFrom-Json
        $rbacData.Subscription.Id = $subscriptionId
        ( $rbacData | ConvertTo-Json -Depth 10 ) -replace "\\u0027", "'" | Out-File $rbactmp
        Update-RoleAssignments -inputFile $rbactmp -prefix $deploymentPrefix -domain $tenantDomain
        Start-Sleep 10

        ### Invoke ARM deployment.
        log "Intiating Zero Down Time Solution Deployment." Cyan
        
        log "Invoke Background Job Deployment for Monitoring Solution - OMS Workspace and Application Insights."
        Invoke-ARMDeployment -subscriptionId $subscriptionId -resourceGroupPrefix $deploymentPrefix -location $location -steps 1 -prerequisiteRefresh -packageVersion $packageVersion

        # Pause Session for Background Job to Initiate.
        log "Waiting session for background job to initiate"
        Start-Sleep 20

        #Get deployment status
        while ((Get-Job -Name '1-create' | Select-Object -Last 1).State -eq 'Running') {
            Get-ARMDeploymentStatus -jobName '1-create'
            Start-Sleep 10
        }
        if ((Get-Job -Name '1-create' | Select-Object -Last 1).State -eq 'Completed') 
        {
            Get-ARMDeploymentStatus -jobName '1-create'
        }
        else
        {
            Get-ARMDeploymentStatus -jobName '1-create'
            log $error[0] -color Red
            Break
        }

        log "Invoke Backend deployment."
        Invoke-ARMDeployment -subscriptionId $subscriptionId -resourceGroupPrefix $deploymentPrefix -location $location -steps 2 -packageVersion $packageVersion

        # Pause Session for Background Job to Initiate.
        log "Waiting for background job to initiate"
        Start-Sleep 20

        #Get deployment status
        while ((Get-Job -Name '2-create' | Select-Object -Last 1).State -eq 'Running') {
            Get-ARMDeploymentStatus -jobName '2-create'
            Start-Sleep 10
        }
        if ((Get-Job -Name '2-create' | Select-Object -Last 1).State -eq 'Completed') 
        {
            Get-ARMDeploymentStatus -jobName '2-create'
        }
        else
        {
            Get-ARMDeploymentStatus -jobName '2-create'
            log $error[0] -color Red
            Break
        }
        
        log "Invoke Workload deployment."
        Invoke-ARMDeployment -subscriptionId $subscriptionId -resourceGroupPrefix $deploymentPrefix -location $location -packageVersion $packageVersion  -steps 3

        # Pause Session for Background Job to Initiate.
        log "Waiting for background job to initiate"
        Start-Sleep 20

        #Get deployment status
        while ((Get-Job -Name '3-create' | Select-Object -Last 1).State -eq 'Running') {
            Get-ARMDeploymentStatus -jobName '3-create'
            Start-Sleep 10
        }
        if ((Get-Job -Name '3-create' | Select-Object -Last 1).State -eq 'Completed') 
        {
            Get-ARMDeploymentStatus -jobName '3-create'
        }
        else
        {
            Get-ARMDeploymentStatus -jobName '3-create'
            log $error[0] -color Red
            Break
        }    

        log "Invoke Network deployment."
        Invoke-ARMDeployment -subscriptionId $subscriptionId -resourceGroupPrefix $deploymentPrefix -location $location -steps 4 -packageVersion $packageVersion

        # Pause Session for Background Job to Initiate.
        log "Waiting for background job to initiate"
        Start-Sleep 20

        #Get deployment status
        while ((Get-Job -Name '4-create' | Select-Object -Last 1).State -eq 'Running') {
            Get-ARMDeploymentStatus -jobName '4-create'
            Start-Sleep 10
        }
        if ((Get-Job -Name '4-create' | Select-Object -Last 1).State -eq 'Completed') 
        {
            Get-ARMDeploymentStatus -jobName '4-create'
        }
        else
        {
            Get-ARMDeploymentStatus -jobName '4-create'
            log $error[0] -color Red
            Break
        }
        
        log "Create availability test"
        New-AzureRmResourceGroupDeployment -Name "CreateWebTest" -ResourceGroupName "$deploymentPrefix-operations-rg" `
            -TemplateParameterFile "$ScriptRoot/templates/webtest.parameters.json" `
            -TemplateFile "$ScriptRoot/templates/resources/microsoft.appinsights/webtest.json"

        log "Deployment has been completed." Cyan
    }
}
End {
    Stop-Transcript
}