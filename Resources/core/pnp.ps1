param ($destinationFolder , $eggXamplesFile , $configFile)

Clear-Host
$ErrorActionPreference = "Stop"

function Get-PnPSamplesRepo([string] $destinationFolder, [string] $knifeTempFolder ) {
    $fx = "Get-PnPSamplesRepo"
    
    Write-Host "[$fx] Start"
    
    $repoUrl = "https://github.com/pnp/script-samples/archive/refs/heads/main.zip";
    Write-Host "[$fx]  PnPScriptSamples Url: $repoUrl"
  
    $rndFile = [System.IO.Path]::GetTempFileName() 
    $rndFile = [System.IO.Path]::ChangeExtension($rndFile, ".zip");
    $tempFolder = (Split-path $rndFile -Parent ) + "\$knifeTempFolder\" 
    $rootTempFolder = $tempFolder

    $rndFolder = [System.IO.Path]::GetTempFileName()
    $rndFolder = Split-Path -Path $rndFolder -Leaf
    $rndFolder = "fld_" + $rndFolder.Replace(".tmp", "\");

    $tempFolder = $tempFolder + $rndFolder

    [System.IO.Directory]::CreateDirectory($tempFolder);
    Write-Host "[$fx]  TempFolder: $tempFolder"

    $rndFile = $tempFolder + (Split-path $rndFile -Leaf )
    Write-Host "[$fx]  Downloading repo ...."
    $ProgressPreference = 'SilentlyContinue'
    Invoke-RestMethod -Uri $repoUrl -OutFile $rndFile
    Write-Host "[$fx]  Repo downloaded!"
    #Extract Zip File
    Write-Host "[$fx]  Extract Repo package"
    $ProgressPreference = 'SilentlyContinue'
    Expand-Archive -Path $rndFile -DestinationPath ($tempFolder) -Force
    Write-Host "[$fx]  Repo Package extracted!"
   
    Write-Host ("[$fx]  Create {0} folder on destination" -f ($tempFolder + "\script-samples-main\scripts"))
    ## move tmpfolder to realfolder 
    $sourceFolder = Resolve-Path -Path ($tempFolder + "\script-samples-main\scripts")

    if (Test-Path -Path "$destinationFolder") {
        Write-Host "[$fx]  Remove old script folder"
        Remove-Item ($destinationFolder) -Recurse -force
    }
    New-Item -Path $destinationFolder -ItemType Directory -Force
    $destinationFolder = Resolve-Path -Path  $destinationFolder

    Write-Host "[$fx]  Move repo scripts to destination $destinationFolder"
    Move-Item -Path $sourceFolder.Path -Destination $destinationFolder -Force
    # Remove-Item ($destinationFolder.Path + "\tmp" ) -Recurse -force
    Write-Host "[$fx]  Remove tempfolder $rootTempFolder"
    Remove-Item ($rootTempFolder) -Recurse -force
   
    
    ## create relative PS1
    $folders = Get-ChildItem -Path ($destinationFolder + "\scripts") -Directory | Where-Object { $_.Name -ne "_template-script-submission" }
    foreach ($f in $folders) {
        New-PowerShellScript -sampleFolder $f.FullName
    }
    Write-Host "[$fx] End"
   
}
function Format-Text($text) {
    [int]$totalChars = 92

    if ($text.Length -gt $totalChars) {
        $text = $text.SubString(0, $totalChars - 4) + " ..."
    }

    $text
}
### create .ps1 files from sample.json
function New-PowerShellScript($sampleFolder) {
    
    $fx = "New-PowerShellScript"
    $tobeCreated = $true
    Write-Host "[$fx]  Start"

    $readMeFile = "$sampleFolder\README.md"
    $sampleFile = "$sampleFolder\assets\sample.json"
    #Parse the file
    Write-Host "[$fx]   Parse the file [$readMeFile]"
    $ast = [System.Management.Automation.Language.Parser]::ParseFile($readMeFile, [ref]$null, [ref]$null)
    $s1 = $ast.Extent.Text
    $s = $s1 -split "````powershell" ##$s.Split("```powershell")
    if ($s.Length -eq 1) {
        $s = $s1 -split "```` powershell"
    }

    $sampleInfo = Get-Content -Path $sampleFile -Raw | ConvertFrom-Json 
    $props = $sampleInfo | Get-Member -MemberType NoteProperty
    $authorsFull = ""
    $authorsShort = ""
    foreach ($aut in $sampleInfo.authors) {
        $authorsFull += $aut.name + " [github account:" + $aut.gitHubAccount + "],"
        $authorsShort += ($aut.name -split ' ')[0] + ","
    }
    $authorsFull = $authorsFull -replace ".$"
    $authorsShort = $authorsShort -replace ".$"
    $props = $sampleInfo | Get-Member -MemberType NoteProperty
    foreach ($p in $props) {
        $pname = $p.Name
        $tab = "`t"
        # if ("creationDateTime,updateDateTime" -like ("*" + $pname + "*")) {
        #     $tab = "`t"
        # }
        # if ("shortDescription,creationDateTime,updateDateTime" -like ("*" + $pname + "*")) {
        #     $tab = "`t"
        # }
        $sampleInfo[0]."$pname" = $tab + ($sampleInfo."$pname")
    }
    $script = Get-Content -Path "$PSScriptRoot\phmsg.txt" -Raw
    $sampleInfo = $sampleInfo[0]
    $authorsShort = Format-Text -text $authorsShort
    $sampleInfo.Title = Format-Text -text $sampleInfo.Title
    $sampleInfo.Name = Format-Text -text $sampleInfo.Name
    $sampleInfo.shortDescription = Format-Text -text $sampleInfo.shortDescription

    ##
    
    $sampleInfo.Url = ($sampleInfo.Url.ToLower() -replace "https://pnp.github.io/script-samples/", "https://aka.ms/script-samples/")
    $script = $script -f $authorsShort , `
        $sampleInfo.Name, $sampleInfo.Title `
        , $sampleInfo.shortDescription, $sampleInfo.Url `
        , $sampleInfo.creationDateTime, $sampleInfo.updateDateTime `
        , ("`t" + $authorsFull), '{', '}'
    try {
        $script += ($s[1] -split '```')[0] 
    }
    catch {
        ### not create ps1 
        Write-Host "[$fx]    This file [ $readMeFile] doesnt have any powershell script references in onder to be created "
        $tobeCreated = $false
    }
    $script += "`n`r"
    $filename = (Split-Path -Path (Split-Path -Path $readMeFile -Parent) -leaf) + ".ps1"
    Write-Host "[$fx]   End of file parsing"
    if ( $tobeCreated -eq $true) {
        $script | Set-Content -Path (((Split-Path -Path $readMeFile -Parent)) + "/" + $filename)
        Write-Host "[$fx]   File [$filename] created."
    }
    Write-Host "[$fx]  End"

}
## Build shorcuts and Cmdlines based on scripts folder
function Set-ShortcutsObjs($scriptsfolder, $eggXamplesFile, $configFile ) {
    $fx = "Set-ShortcutsObjs"
    Write-Host "[$fx] Start"
    ## Read all Folders
    $tempFolder = $scriptsfolder
    # $path = "$tempFolder\$firstElement\"
    $scriptsFolder = "$scriptsFolder\scripts" 
    $extension = "ps1"
    $ToolTipWildCard = "PNP-SCRIPT-SAMPLES-UPDATED#"
    $ToolTip = "PnP Script Samples, list of samples on PnP Script Samples github repo .[ $ToolTipWildCard - " + (Get-Date -Format "yyyy-MM-dd HH:mm:ss") + "]"
    
    Write-Host "[$fx]  Get all folders under scripts"
    $Allfolders = Get-ChildItem -Path $scriptsFolder | where-object `
    { $_.Name -ne '_template-script-submission' -And $_.Name -ne 'README.md' }
                    
    $Allfolders = $Allfolders |  `
        Select-Object -Property  @{Name = 'Folder'; Expression = { $_.Name } } 

    Write-Host "[$fx]  Build Object with info"
    foreach ($f in $Allfolders) {
        $f | Add-Member -MemberType NoteProperty -Name "Status" -Value "CHECK" -Force
        $f | Add-Member -MemberType NoteProperty -Name "Title" -Value "" -Force
        $f | Add-Member -MemberType NoteProperty -Name "Key" -Value "" -Force
        $f | Add-Member -MemberType NoteProperty -Name "WFolder" -Value "" -Force
        $f | Add-Member -MemberType NoteProperty -Name "WFile" -Value "" -Force
        $f | Add-Member -MemberType NoteProperty -Name "Order" -Value 99 -Force
        $f | Add-Member -MemberType NoteProperty -Name "Path" -Value "" -Force
        $wildCard = "aad-,spo-,teams-,flow-,powerapps-,graph-, modernize-, stream-, onedrive-"
        for ($i = 3; $i -lt 10; $i++) {
            $grp01 = $f.Folder.SubString(0, $i)
            if ($wildCard -Like ("*" + $grp01 + "-*")) {
                $f | Add-Member -MemberType NoteProperty -Name "Status" -Value "OK" -Force
                $f | Add-Member -MemberType NoteProperty -Name "Path" -Value ($grp01 + "\" + $f.Folder.Substring($i + 1, ($f.Folder).Length - $i - 1 )) -Force
                $f | Add-Member -MemberType NoteProperty -Name "Order" -Value 1 -Force
                $f | Add-Member -MemberType NoteProperty -Name "Title" -Value ($grp01 ) -Force
                $f | Add-Member -MemberType NoteProperty -Name "Key" -Value ($grp01 + "\hive") -Force
                $f | Add-Member -MemberType NoteProperty -Name "WFolder" -Value ($grp01 + "-**") -Force
                $f | Add-Member -MemberType NoteProperty -Name "WFile" -Value ($grp01 + "-**/*.$extension") -Force
            }
        }
        if ($f.status -ne "OK") {
            $f.Path = "misc\" + $f.Folder
            $f.Title = $f.Folder 
            $f.WFolder = ($f.Folder + "")
            # $f.WFile = ( $f.WFolder + "/*.$extension")
            $f.WFile = ( $f.WFolder + "/" + $f.WFolder + ".$extension")
            $f | Add-Member -MemberType NoteProperty -Name "Status" -Value "OK" -Force
            $f | Add-Member -MemberType NoteProperty -Name "Order" -Value 9999 -Force
            $f | Add-Member -MemberType NoteProperty -Name "Key" -Value ("misc\hive") -Force
        }
    }
    $shortcs = $Allfolders | Select-Object  Title, Key, Wfolder, WFile, Order -Unique | Sort-Object -Property Order, WFolder 
    ## create shortcuts
    Write-Host "[$fx]  End Build Object with info"
    ## get objects
    Write-Host "[$fx]  Get eggamxples data"
   
 
    $shortCutModel = '{
    "Key": "S1\\F1",
    "Title": "FilesAndFolders",
    "ToolTip": "Yup , shortcuts to files and folders, pointing to a root folder",
    "Type": 1,
    "Command": ".\\\\Resources\\\\samples\\\\shortcuts\\\\FilesAndFolders",
    "IconPath": null,
    "ShortCutType": 0,
    "CmdLineType": 4,
    "WildCardFolders": "*",
    "WildCardFiles": "*",
    "Visible": true,
    "Order": 201
  }'
    
    $rootItemShortCut = $shortCutModel | ConvertFrom-Json #-Depth 10

    Write-Host "[$fx]  Build ShortCust,Cmdline objects"
    $tobeShortcus = @()
    $ct = 9000
    $key = "PNP"
    $miscRootFolderCreated= $false
    foreach ($s in $shortcs) {

        $title = $s.Title
        $wfolder = $s.WFolder
        $wfile = $s.WFile
        if ($s.key.IndexOf("misc\") -gt -1) {

            if ($miscRootFolderCreated -eq $false)
            {
                $tmpS = $rootItemShortCut.psobject.copy()
                $tmpS.Title = "misc"
                $tmpS.Key = "$Key\misc"
                $tmpS.ShortCutType = 0
                $tmpS.Command = $scriptsfolder + "\"
                $tmpS.WildCardFolders = ""
                $tmpS.WildCardFiles = ""
                $tmpS.Order = $ct + 5
                $tobeShortcus += $tmpS
                $miscRootFolderCreated = $true
            }
            #ROOT#
            $tmpS = $rootItemShortCut.psobject.copy()
            $tmpS.Title = $title
            $tmpS.Key = "$Key\misc\$title"
            $tmpS.ShortCutType = 3
            $tmpS.Command = $scriptsfolder + "\" + $s.WFile
            $tmpS.WildCardFolders = ""
            $tmpS.WildCardFiles = ""
            $tmpS.Order = $ct + 10
            $tobeShortcus += $tmpS
        }
        else {
            #hive
            $tmpS = $rootItemShortCut.psobject.copy()
            $tmpS.Title = "hive"
            $tmpS.Key = "$Key\$title\hive"
            $tmpS.ShortCutType = 0
            $tmpS.Command = $scriptsfolder + "\"
            $tmpS.WildCardFolders = $wfolder
            $tmpS.WildCardFiles = ""
            $tmpS.Order = $ct + 20
            $tobeShortcus += $tmpS
            #ROOT#
            $tmpS = $rootItemShortCut.psobject.copy()
            $tmpS.Title = $title
            $tmpS.Key = "$Key\$title"
            $tmpS.ShortCutType = 2
            $tmpS.Command = $scriptsfolder + "\"
            $tmpS.WildCardFolders = $wfolder
            $tmpS.WildCardFiles = $wfile
            $tmpS.Order = $ct + 10
            $tobeShortcus += $tmpS
        }
 
        $ct = $ct + 20
    }
    $iconPath= ($PSScriptRoot + "\parker.png").Replace("\", "\\")
    $rootModel = '
        {
          "Key": "'+ $key + '",
          "Title": "PnP Script Samples ! ",
          "ToolTip": "Root Element",
          "Type": 0,
          "Command": null,
          "IconPath": "' + $iconPath+ '",
          "ShortCutType": 4,
          "CmdLineType": 4,
          "WildCardFolders": "*",
          "WildCardFiles": "*.*",
          "Visible": true,
          "Order": 100
        }'
    $rootItem = $rootModel | ConvertFrom-Json #-Depth 10
    $rootItem.Title = "PnP Script Samples"

    $rootItem.Tooltip = $tooltip
    $rootItem.Order = "9000"

    Write-Host "[$fx]  End Build ShortCust,Cmdline objects"

    Write-Host "[$fx]  Update current configuration"
    #add or replace existing
    # find if  $ToolTipWildCard exists on curretn config 
    ## if it does exist replace , if not create a new one
 
    ##$tobeShortcus = $tobeShortcus | Where-Object { $_.Key -like '*graph*' }

    $tobeShortcus += $rootItem
    if (Test-Path -Path $configFile) {
        Write-Host "[$fx]  Config exists, test if empty"
        $config = (Get-Content -Path $configFile -Raw) | ConvertFrom-Json ##-Depth 10
        if ($null -eq $config) {
            Write-Host "[$fx]   Apparently Config exists, but is empty, so unique element will be this one" 
            $ToolTip = "PnP Script Samples, list of samples on PnP Script Samples github repo .[ $ToolTipWildCard - " + (Get-Date -Format "yyyy-MM-dd HH:mm:ss") + "]"
            $rootItem.Tooltip = $ToolTip
            # $tobeShortcus += $rootItem
            $configTmp = ($tobeShortcus | ConvertTo-Json) 
            $configTxt = $configTmp | ConvertTo-Json -Depth 10
            $configTxt = $configTxt | ConvertFrom-Json

            $a = ""
        }
        else {
            Write-Host "[$fx]   Config exists and with elements, lets search if the entry PnPScriptSamples exist" 

            $found = @($config | Where-Object { $_.Tooltip -like "*" + $ToolTipWildCard + "*" } | select-object -Unique)
            if ($found.Count -gt 0) {

                $key = $found.Key
                $config = $config | Where-Object { $_.Key -notlike "*$key*" }
               
                Write-Host "[$fx]   Config exists,with elements and entry PnPScriptSamples exist. It will delete exisitjg entries and add new ones" 
                $ToolTip = "PnP Script Samples, list of samples on PnP Script Samples github repo .[ $ToolTipWildCard - " + (Get-Date -Format "yyyy-MM-dd HH:mm:ss") + "]"
                $tobeShortcus[$tobeShortcus.Length - 1].Tooltip = $ToolTip

                $all = @()
                $all += $config
                $all += $tobeShortcus
                $config = $all

                if ($config.Count -eq 1) {
                    $configTmp = "[" + ($config | ConvertTo-Json) + "]"
                    $configTxt = $configTmp | ConvertTo-Json -Depth 10
                    $configTxt = $configTxt | ConvertFrom-Json
                }
                else {
                    $configTxt = $config | ConvertTo-Json -Depth 10 
                }
       
                

            }
            else {
                $ToolTip = "PnP Script Samples, list of samples on PnP Script Samples github repo .[ $ToolTipWildCard - " + (Get-Date -Format "yyyy-MM-dd HH:mm:ss") + "]"
                $tobeShortcus[$tobeShortcus.Length - 1].Tooltip = $ToolTip
                if ($config.Count -eq 0) {
                    # $configTmp = "[" + ($tobeShortcus | ConvertTo-Json) + "]"
                    $configTmp = ($tobeShortcus | ConvertTo-Json) 
                    $configTxt = $configTmp | ConvertTo-Json -Depth 10
                    $configTxt = $configTxt | ConvertFrom-Json
                }
                else {
                    $all = @()
                    $all += $config
                    $all += $tobeShortcus
                    $config = $all
                    $configTxt = $config | ConvertTo-Json -Depth 10
                }
               
            }
           
        }
    }   
    ## -replace ".$"

    $configTxt | Set-Content -Path $configFile 
    Write-Host "[$fx]  End Update current configuration"
    # $rootItems
    Write-Host "[$fx] End"
} 




$destinationFolder = "$PSScriptRoot\..\pnp\script-samples"
$eggXamplesFile = "$PSScriptRoot\..\samples\eggxamples.json"
$configFile = "$PSScriptRoot\..\..\settings\mnu-config.json"


$configFile = (Resolve-path -path $configFile).Path
function Get-PnpSamples($destinationFolder, $eggXamplesFile, $configFile) {
    Get-PnPSamplesRepo -destinationFolder $destinationFolder -knifeTempFolder "knife"
    $destinationFolder = (Resolve-Path -Path  $destinationFolder).Path
    Set-ShortcutsObjs -scriptsfolder $destinationFolder -eggXamplesFile $eggXamplesFile -configFile $configFile
}
if ( ($null -ne $destinationFolder) -and ($null -ne $eggXamplesFile ) -and ($null -ne $configFile)) {
    Get-PnpSamples -destinationFolder $destinationFolder -eggXamplesFile $eggXamplesFile -configFile $configFile 
    Write-Host "`n PnP Script Samples Imported!"
    Write-Host " (hit any key to continue)"
    $Host.UI.RawUI.ReadKey() | Out-Null
} 
else {
    
    Write-Host ""
}
exit


