$Archs = {"Net40", "SL5", "WP7", "WinRT"}
$Projects = {"ReactiveUI", "ReactiveUI.Testing", "ReactiveUI.Xaml", "ReactiveUI.Routing", "ReactiveUI.Blend"}

$SlnFileExists = Test-Path ".\ReactiveUI.sln"
if ($SlnFileExists -eq $False) {
    echo "*** ERROR: Run this in the project root ***"
    exit -1
}

rmdir -r --force .\Release

foreach-object $Archs | %{mkdir -p ".\Release\$_"}

foreach-object $Archs | %{
    $currentArch = $_
    
    foreach-object $Projects | %{cp -r ".\$_\bin\Release\$currentArch\*" ".\Release\$currentArch"}
    
    #ls -r | ?{$_.FullName.Contains("bin\Release\$currentArch") -and $_.Length} | %{echo cp $_.FullName ".\Release\$currentArch"}
}

ls -r .\Release | ?{$_.FullName.Contains("Clousot")} | %{rm $_.FullName}
