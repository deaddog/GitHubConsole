
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
  githubCommandsFromApp "" | Where { $_ -match "^" + $command }
}
function script:githubCommandsFromApp([string]$dummy)
{
  $output = github help
  foreach ($line in $output) {
    if ($line -match '^  ([^ ]+)') {
      #$line = $matches[1]
      #if ($line -match "^" + $command) {
        #$line
      #}
      $matches[1]
    }
  }
}
