# �������� ����������
if (-not $args[0]) {
    Write-Output "Usage: .\dir_structure.ps1 `"C:\Your\Path`" [exclude_file1 exclude_file2...]"
    exit
}

$rootPath = $args[0]
$excludeFiles = if ($args.Count -gt 1) { $args[1..($args.Count-1)] } else { @() }

# �������� ����
if (-not (Test-Path $rootPath)) {
    Write-Output "ERROR: Path not found"
    exit
}

function Show-Tree {
    param(
        [string]$path,
        [string]$indent = ""
    )
    
    $items = Get-ChildItem -Path $path -Force -ErrorAction SilentlyContinue
    
    foreach ($item in $items) {
        if ($item.PSIsContainer) {
            Write-Output "${indent}+-- $($item.Name)/"
            Show-Tree -path $item.FullName -indent "$indent|   "
        } else {
            Write-Output "${indent}+-- $($item.Name)"
        }
    }
}

# ����������� ���������
Write-Output "[DIRECTORY TREE]"
Show-Tree -path $rootPath
Write-Output "`n"

# ���������� ����������
Write-Output "`n[FILE CONTENTS]"
Get-ChildItem $rootPath -Recurse -File | ForEach-Object {
    $file = $_
    
    # ���������, ����� �� ��������� ����
    $exclude = $excludeFiles | Where-Object {
        $file.Name -eq $_ -or $file.FullName -eq $_
    }
    
    if (-not $exclude) {
        Write-Output "=== FILE: $($file.FullName.Substring($rootPath.Length).TrimStart('\')) ==="
        Get-Content $file.FullName
        Write-Output "`n---`n"
    }
}