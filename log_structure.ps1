& "$PSScriptRoot\dir_structure.ps1" "$PSScriptRoot\server\src" ""| Out-File -FilePath "$PSScriptRoot\server_structure.txt" -Encoding utf8
& "$PSScriptRoot\dir_structure.ps1" "$PSScriptRoot\user\src" "" | Out-File -FilePath "$PSScriptRoot\user_structure.txt" -Encoding utf8

& "$PSScriptRoot\dir_structure.ps1" "$PSScriptRoot\server\src" ""| Out-File -FilePath "$PSScriptRoot\structure.txt" -Encoding utf8
& "$PSScriptRoot\dir_structure.ps1" "$PSScriptRoot\user\src" "" | Out-File -FilePath "$PSScriptRoot\structure.txt" -Append -Encoding utf8