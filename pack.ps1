$outputDir = ".\package\"
$build = "Release"
$version = "1.0.0"

.\.nuget\nuget.exe pack ".\src\Geta.NotFoundHandler\Geta.NotFoundHandler.csproj" -IncludeReferencedProjects -properties Configuration=$build -Version $version -OutputDirectory $outputDir
