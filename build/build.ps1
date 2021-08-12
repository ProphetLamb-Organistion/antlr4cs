param (
	[switch]$Debug,
	[string]$VisualStudioVersion,
	[switch]$NoClean,
	[string]$Verbosity = "minimal",
	[string]$Logger,
	[string]$Java6Home,
	[string]$MavenHome,
	[string]$MavenRepo,
	[switch]$SkipMaven,
	[switch]$SkipKeyCheck,
	[switch]$GenerateTests,
	[switch]$NoValidate,
	[switch]$UseMsBuild
)

# build the solution
$SolutionPath = "..\Runtime\CSharp\Antlr4.sln"

# make sure the script was run from the expected path
if (!(Test-Path $SolutionPath)) {
	Write-Error("The script was run from an invalid working directory.")
	exit 1
}

. .\version.ps1

If ($Debug) {
	$BuildConfig = 'Debug'
} Else {
	$BuildConfig = 'Release'
}

If ($AntlrVersion.Contains('-')) {
	$KeyConfiguration = 'Dev'
} Else {
	$KeyConfiguration = 'Final'
}

If ($NoClean) {
	$Target = 'build'
} Else {
	$Target = 'rebuild'
}

If (-not $MavenHome) {
	$MavenHome = $env:M2_HOME
}

function GetJavaPath() {
	$_javaKey = 'HKLM:\SOFTWARE\JavaSoft\Java Runtime Environment\1.6'
	$_javaPath = 'JavaHome'
	if ($Java6Home -and (Test-Path $Java6Home)) {
		return $Java6Home
	}

	if (Test-Path $_javaKey) {
		$_javaHomeKey = Get-Item -LiteralPath $_javaKey
		if ($null -ne $_javaHomeKey.GetValue($_javaPath, $null)) {
			$_javaHomeProperty = Get-ItemProperty $_javaKey $_javaPath
			return $_javaHomeProperty.$_javaPath
		}
	}

	Write-Error 'Unable to locate the Java6 runtime.'
	exit 0x10
}

function GetNugetPath() {
	$_nuget=Get-Command('nuget') -ErrorAction SilentlyContinue
	if (!$_nuget) {
		$_nuget = $_nuget.Source
	} else {
		$_nuget = '..\runtime\CSharp\.nuget\NuGet.exe'
		If (-not (Test-Path $_nuget)) {
			If (-not (Test-Path '..\runtime\CSharp\.nuget')) {
				mkdir '..\runtime\CSharp\.nuget'
			}
	
			$_nugetSource = 'https://dist.nuget.org/win-x86-commandline/v5.7.0/nuget.exe'
			Invoke-WebRequest $_nugetSource -OutFile $_nuget
			if (-not $?) {
				Write-Error 'Downloading the nuget package manager failed.'
				$_nuget = $null
			}
		}
	}
	if ($null -ne $_nuget) {
		return $_nuget
	}

	Write-Error 'Unable to obtain the nuget package manager.'
	exit 0x11
}

function GetMsBuildPath() {
	if (-not [String]::IsNullOrEmpty($VisualStudioVersion)) {
		$_visualStudio = (Get-ItemProperty 'HKLM:\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\SxS\VS7')."$VisualStudioVersion"
		return "$_visualStudio\MSBuild\$VisualStudioVersion\Bin\MSBuild.exe"
	}
	$_key=(Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\MSBuild\ToolsVersions\4.0' -errorAction SilentlyContinue)
	if ($null -ne $_key) { 
		return [String]::Concat($_key.MSBuildToolsPath, "MSBuild.exe")
	}
	$_key=(Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\MSBuild\ToolsVersions\2.0' -errorAction SilentlyContinue)
	if ($null -ne $_key) { 
		return [String]::Concat($_key.MSBuildToolsPath, "MSBuild.exe")
	}

	Write-Error 'Unable to obtain the MSBuild tool.'
	exit 0x12
}

$Java6Home=GetJavaPath

# Build the Java library using Maven
If (-not $SkipMaven) {
	$OriginalPath = $PWD

	Set-Location '..\tool'
	$MavenPath = "$MavenHome\bin\mvn.cmd"
	If (-not (Test-Path $MavenPath)) {
		$MavenPath = "$MavenHome\bin\mvn.bat"
	}

	If (-not (Test-Path $MavenPath)) {
		Write-Error("Couldn't locate Maven binary: $MavenPath")
		Set-Location $OriginalPath
		exit 1
	}

	If ($GenerateTests) {
		$SkipTestsArg = 'false'
	} Else {
		$SkipTestsArg = 'true'
	}

	If ($MavenRepo) {
		$MavenRepoArg = "-Dmaven.repo.local=`"$MavenRepo`""
	}

	$MavenGoal = 'package'
	&$MavenPath '-B' $MavenRepoArg "-DskipTests=$SkipTestsArg" '--errors' '-e' '-Dgpg.useagent=true' "-Djava6.home=$Java6Home" '-Psonatype-oss-release' $MavenGoal
	if (-not $?) {
		Write-Error('Maven build of the C# Target custom Tool failed, aborting!')
		Set-Location $OriginalPath
		Exit $LASTEXITCODE
	}

	Set-Location $OriginalPath
}

# this is configured here for path checking, but also in the .props and .targets files
[xml]$pom = Get-Content "..\tool\pom.xml"
$CSharpToolVersionNodeInfo = Select-Xml "/mvn:project/mvn:version" -Namespace @{mvn='http://maven.apache.org/POM/4.0.0'} $pom
$CSharpToolVersion = $CSharpToolVersionNodeInfo.Node.InnerText.trim()

# build the main project
if ($UseMsBuild) {
	$msbuild=GetMsBuildPath
	$visualStudioVersionOption=if($null -eq $VisualStudioVersion) { $null } else { "/p:VisualStudioVersion=$VisualStudioVersion" }
}

If ($Logger) {
	$LoggerArgument = "/logger:$Logger"
}

&$nuget 'restore' $SolutionPath -Project2ProjectTimeOut 1200
if (-not $?) {
	Write-Error('Restore failed, aborting!')
	Exit $LASTEXITCODE
}
if ($UseMsBuild) {
	&$msbuild '/nologo' '/m' '/nr:false' "/t:$Target" $LoggerArgument "/verbosity:$Verbosity" "/p:Configuration=$BuildConfig" $visualStudioVersionOption "/p:KeyConfiguration=$KeyConfiguration" $SolutionPath
} else {
	if ($NoClean) {
		dotnet clear "--nologo" $SolutionPath
	}
	dotnet build "--nologo" "--verbosity:$Verbosity" "--configuration=$BuildConfig" "/p:KeyConfiguration=$KeyConfiguration" $SolutionPath
}
if (-not $?) {
	Write-Error('Build failed, aborting!')
	Exit $LASTEXITCODE
}

if (-not (Test-Path 'nuget')) {
	mkdir "nuget"
}

$JarPath = "..\tool\target\antlr4-csharp-$CSharpToolVersion-complete.jar"
if (!(Test-Path $JarPath)) {
	Write-Error("Couldn't locate the complete jar used for building C# parsers: $JarPath")
	exit 1
}

# By default, do not create a NuGet package unless the expected strong name key files were used
if (-not $SkipKeyCheck) {
	. .\keys.ps1

	foreach ($pair in $Keys.GetEnumerator()) {
		$assembly = Resolve-FullPath -Path "..\runtime\CSharp\Antlr4.Runtime\bin\$BuildConfig\$($pair.Key)\Antlr4.Runtime.dll"

		# Run the actual check in a separate process or the current process will keep the assembly file locked
		powershell -Command ".\check-key.ps1 -Assembly '$assembly' -ExpectedKey '$($pair.Value)' -Build '$($pair.Key)'"
		if (-not $?) {
			Exit $LASTEXITCODE
		}
	}
}

$packages = @(
	'Antlr4.CodeGenerator'
	'Antlr4')

ForEach ($package in $packages) {
	If (-not (Test-Path ".\$package.nuspec")) {
		Write-Error("Couldn't locate NuGet package specification: $package")
		exit 1
	}

	&$nuget 'pack' ".\$package.nuspec" '-OutputDirectory' 'nuget' '-Prop' "Configuration=$BuildConfig" '-Version' "$AntlrVersion" '-Prop' "M2_REPO=$M2_REPO" '-Prop' "CSharpToolVersion=$CSharpToolVersion" '-Symbols'
	if (-not $?) {
		Exit $LASTEXITCODE
	}
}

# Validate code generation using the Java code generator
If (-not $NoValidate) {
	git 'clean' '-dxf' 'DotnetValidationJavaCodegen'
	dotnet 'run' '--project' '.\DotnetValidationJavaCodegen\DotnetValidation.csproj' '--framework' 'netcoreapp1.1'
	if (-not $?) {
		Write-Error('Build failed, aborting!')
		Exit $LASTEXITCODE
	}

	git 'clean' '-dxf' 'DotnetValidationJavaCodegen'
	dotnet 'run' '--project' '.\DotnetValidationJavaCodegen\DotnetValidation.csproj' '--framework' 'netcoreapp2.1'
	if (-not $?) {
		Write-Error('Build failed, aborting!')
		Exit $LASTEXITCODE
	}

	git 'clean' '-dxf' 'DotnetValidationJavaCodegen'
	&$nuget 'restore' 'DotnetValidationJavaCodegen'
	&$msbuild '/nologo' '/m' '/nr:false' '/t:Rebuild' $LoggerArgument "/verbosity:$Verbosity" "/p:Configuration=$BuildConfig" '.\DotnetValidationJavaCodegen\DotnetValidation.sln'
	if (-not $?) {
		Write-Error('Build failed, aborting!')
		Exit $LASTEXITCODE
	}

	".\DotnetValidationJavaCodegen\bin\$BuildConfig\net20\DotnetValidation.exe"
	if (-not $?) {
		Write-Error('Build failed, aborting!')
		Exit $LASTEXITCODE
	}

	".\DotnetValidationJavaCodegen\bin\$BuildConfig\net30\DotnetValidation.exe"
	if (-not $?) {
		Write-Error('Build failed, aborting!')
		Exit $LASTEXITCODE
	}

	".\DotnetValidationJavaCodegen\bin\$BuildConfig\net35\DotnetValidation.exe"
	if (-not $?) {
		Write-Error('Build failed, aborting!')
		Exit $LASTEXITCODE
	}

	".\DotnetValidationJavaCodegen\bin\$BuildConfig\net40\DotnetValidation.exe"
	if (-not $?) {
		Write-Error('Build failed, aborting!')
		Exit $LASTEXITCODE
	}

	".\DotnetValidationJavaCodegen\bin\$BuildConfig\portable40-net40+sl5+win8+wp8+wpa81\DotnetValidation.exe"
	if (-not $?) {
		Write-Error('Build failed, aborting!')
		Exit $LASTEXITCODE
	}

	".\DotnetValidationJavaCodegen\bin\$BuildConfig\net45\DotnetValidation.exe"
	if (-not $?) {
		Write-Error('Build failed, aborting!')
		Exit $LASTEXITCODE
	}
}

# Validate code generation using the C# code generator
If (-not $NoValidate) {
	git 'clean' '-dxf' 'DotnetValidation'
	dotnet 'run' '--project' '.\DotnetValidation\DotnetValidation.csproj' '--framework' 'netcoreapp1.1'
	if (-not $?) {
		Write-Error('Build failed, aborting!')
		Exit $LASTEXITCODE
	}

	git 'clean' '-dxf' 'DotnetValidation'
	dotnet 'run' '--project' '.\DotnetValidation\DotnetValidation.csproj' '--framework' 'netcoreapp2.1'
	if (-not $?) {
		Write-Error('Build failed, aborting!')
		Exit $LASTEXITCODE
	}

	git 'clean' '-dxf' 'DotnetValidation'
	&$nuget 'restore' 'DotnetValidation'
	&$msbuild '/nologo' '/m' '/nr:false' '/t:Rebuild' $LoggerArgument "/verbosity:$Verbosity" "/p:Configuration=$BuildConfig" '.\DotnetValidation\DotnetValidation.sln'
	if (-not $?) {
		Write-Error('Build failed, aborting!')
		Exit $LASTEXITCODE
	}

	".\DotnetValidation\bin\$BuildConfig\net20\DotnetValidation.exe"
	if (-not $?) {
		Write-Error('Build failed, aborting!')
		Exit $LASTEXITCODE
	}

	".\DotnetValidation\bin\$BuildConfig\net30\DotnetValidation.exe"
	if (-not $?) {
		Write-Error('Build failed, aborting!')
		Exit $LASTEXITCODE
	}

	".\DotnetValidation\bin\$BuildConfig\net35\DotnetValidation.exe"
	if (-not $?) {
		Write-Error('Build failed, aborting!')
		Exit $LASTEXITCODE
	}

	".\DotnetValidation\bin\$BuildConfig\net40\DotnetValidation.exe"
	if (-not $?) {
		Write-Error('Build failed, aborting!')
		Exit $LASTEXITCODE
	}

	".\DotnetValidation\bin\$BuildConfig\portable40-net40+sl5+win8+wp8+wpa81\DotnetValidation.exe"
	if (-not $?) {
		Write-Error('Build failed, aborting!')
		Exit $LASTEXITCODE
	}

	".\DotnetValidation\bin\$BuildConfig\net45\DotnetValidation.exe"
	if (-not $?) {
		Write-Error('Build failed, aborting!')
		Exit $LASTEXITCODE
	}
}

# Validate code generation using the C# code generator (single target framework)
If (-not $NoValidate) {
	git 'clean' '-dxf' 'DotnetValidationSingleTarget'
	dotnet 'run' '--project' '.\DotnetValidationSingleTarget\DotnetValidation.csproj' '--framework' 'netcoreapp1.1'
	if (-not $?) {
		Write-Error('Build failed, aborting!')
		Exit $LASTEXITCODE
	}

	git 'clean' '-dxf' 'DotnetValidationSingleTarget'
	&$nuget 'restore' 'DotnetValidationSingleTarget'
	&$msbuild '/nologo' '/m' '/nr:false' '/t:Rebuild' $LoggerArgument "/verbosity:$Verbosity" "/p:Configuration=$BuildConfig" '.\DotnetValidationSingleTarget\DotnetValidation.sln'
	if (-not $?) {
		Write-Error('Build failed, aborting!')
		Exit $LASTEXITCODE
	}
}
