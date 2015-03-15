
if (Test-Path Function:\TabExpansion) {
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
    # Handles git push remote <branch>
    # Handles git pull remote <branch>
    "^lolcode (?<lol>.+)" {
      #Write-Host Yes
        #gitBranches $matches['lol']
        @(1,"Hello",3.5,"World")
    }
  }
}