# Determine if a function called TabExpansion is already defined, and if so rename it GitHubTabExpansionBackup.
if ((Test-Path Function:\TabExpansion) -and (-not (Test-Path Function:\GitHubTabExpansionBackup))) {
  Rename-Item Function:\TabExpansion GitHubTabExpansionBackup
}

# The definition of the new tab-expansion method.
function TabExpansion($line, $lastWord) {
  # Get the last block in the current string by handling piping and command separation.
  $lastBlock = [regex]::Split($line, '[|;]')[-1].TrimStart()

  # If the last block starts with 'github' or an alias, call the new tab expansion method.
  # Otherwise call the tab expansion backup.
  switch -regex ($lastBlock) {
    "^$(Get-AliasPattern github) (.*)" { GitHubTabExpansion $lastBlock }

    default { if (Test-Path Function:\GitHubTabExpansionBackup) { GitHubTabExpansionBackup $line $lastWord } }
  }
}

# The definition of which type of tab expansion to perform.
function GitHubTabExpansion($lastBlock) {
  # Remove the 'github' or other aliasing from the block.
  switch -regex ($lastBlock -replace "^$(Get-AliasPattern github) ","") {

    # Handles github <cmd>
    "^(?<cmd>[^ ]*)$" {
        githubCommands $matches['cmd']
    }

    # No tab expansion.
    default { @("") }
  }
}

# A global variable for caching the list of available github commands.
$global:githubListOfAvailableCommands = $null

# The function that handles tab expansion for github commands.
function script:githubCommands($command)
{
  if ($global:githubListOfAvailableCommands -eq $null) {
    $global:githubListOfAvailableCommands = githubCommandsFromApp ""
  }
  $global:githubListOfAvailableCommands | Where { $_ -match "^" + $command }
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
