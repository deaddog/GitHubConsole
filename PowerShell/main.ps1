
if ((Test-Path Function:\TabExpansion) -and (-not (Test-Path Function:\GitHubTabExpansionBackup))) {
  Rename-Item Function:\TabExpansion GitHubTabExpansionBackup
}

function TabExpansion($line, $lastWord) {
  $lastBlock = [regex]::Split($line, '[|;]')[-1].TrimStart()

  switch -regex ($lastBlock) {
    "^$(Get-AliasPattern github) (.*)" { GitHubTabExpansion $lastBlock }

    default { if (Test-Path Function:\GitHubTabExpansionBackup) { GitHubTabExpansionBackup $line $lastWord } }
  }
}

function GitHubTabExpansion($lastBlock) {
  switch -regex ($lastBlock -replace "^$(Get-AliasPattern github) ","") {

  #switch -regex ($lastBlock) {
    # Handles github <cmd>
    "^(?<cmd>[^ ]*)$" {
        githubCommands $matches['cmd']
    }

    default { @("") }
  }
}

function script:githubCommands($command)
{
  @("config", "issues") | Where { $_ -match "^" + $command }
}