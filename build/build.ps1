param (
	[switch]$Debug,
	[string]$Verbosity = "minimal",
	[string]$Java6Home,
	[string]$MavenHome,
	[string]$MavenRepo,
	[switch]$SkipMaven,
	[switch]$SkipKeyCheck,
	[switch]$GenerateTests,
	[switch]$NoValidate,
	[switch]$NoClean
)
# Ulitiy functions
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

function GetMavenPath() {
	$_mavenPath = "$MavenHome\bin\mvn.cmd"
	If (-not (Test-Path $_mavenPath)) {
		$_mavenPath = "$MavenHome\bin\mvn.bat"
	}
	
	If (Test-Path $_mavenPath) {
		return $_mavenPath
	}

	Write-Error "Couldn't locate Maven binary: $_mavenPath"
	Set-Location $OriginalPath
	exit 0x13
}

if ((Get-Variable DOTNET_CLI_TELEMETRY_OPTOUT -ErrorAction SilentlyContinue) -ne 1) {
	Write-Warning "Disabling telemetry. No more reporting back to the mothership!"
	Set-Variable DOTNET_CLI_TELEMETRY_OPTOUT 1
}

$SolutionPath = "..\Runtime\CSharp\Antlr4.sln"

# make sure the script was run from the expected path
if (-not (Test-Path $SolutionPath)) {
	Write-Error "The script was run from an invalid working directory."
	exit 0x05
}

$buildConfig = if ($Debug) { 'Debug' } else { 'Release' }

if (-not $MavenHome) {
	$MavenHome = $env:M2_HOME
}

$Java6Home=GetJavaPath

if (-not $MavenHome) {
	$MavenHome = $env:M2_HOME
}

. .\version.ps1

if (-not $SkipMaven) {
	Write-Host "`n`n************************************`n* Build the Java library using Maven`n************************************`n"
	$OriginalPath = $PWD
	
	Set-Location '..\tool'
	$mavenPath = GetMavenPath
	
	$SkipTestsArg = if ($GenerateTests) { 'false' } else { 'true' }
	
	If ($MavenRepo) {
		$MavenRepoArg = "-Dmaven.repo.local=`"$MavenRepo`""
	}
	
	$mavenGoal = 'package'
	&$mavenPath '-B' $MavenRepoArg "-DskipTests=$SkipTestsArg" '--errors' '-e' '-Dgpg.useagent=true' "-Djava6.home=$Java6Home" '-Psonatype-oss-release' $mavenGoal
	if (-not $?) {
		Write-Error 'Maven build of the C# Target custom Tool failed, aborting!'
		Set-Location $OriginalPath
		exit 0x20
	}
	
	Set-Location $OriginalPath
}

Write-Host "`n`n**********************`n* Building the C# tool`n**********************n"
[xml]$pom = Get-Content "..\tool\pom.xml"
$csharpToolVersionNodeInfo = Select-Xml "/mvn:project/mvn:version" -Namespace @{mvn='http://maven.apache.org/POM/4.0.0'} $pom
$csharpToolVersion = $csharpToolVersionNodeInfo.Node.InnerText.trim()

dotnet build --nologo --verbosity $Verbosity --configuration $buildConfig $SolutionPath
if (-not $?) {
	Write-Error 'Build failed, aborting!'
	exit 0x22
}

$jarPath = "..\tool\target\antlr4-csharp-$csharpToolVersion-complete.jar"
if (!(Test-Path $jarPath)) {
	Write-Error "Couldn't locate the complete jar used for building C# parsers: $jarPath"
	exit 0x14
}

# By default, do not create a NuGet package unless the expected strong name key files were used
if (-not $SkipKeyCheck) {
	. .\keys.ps1

	foreach ($pair in $Keys.GetEnumerator()) {
		$assembly = Resolve-FullPath -Path "..\runtime\CSharp\Antlr4.Runtime\bin\$buildConfig\$($pair.Key)\Antlr4.Runtime.dll"

		# Run the actual check in a separate process or the current process will keep the assembly file locked
		powershell -Command ".\check-key.ps1 -Assembly '$assembly' -ExpectedKey '$($pair.Value)' -Build '$($pair.Key)'"
		if (-not $?) {
			exit 0x30
		}
	}
}

$packages = 'Antlr4.CodeGenerator', 'Antlr4'

foreach ($package in $packages) {
	dotnet pack ".\$package.nuspec" -o .\artifacts\ --no-build --verbosity $Verbosity --configuration $buildConfig /p:Version=$AntlrVersion /p:M2_REPO=$M2_REPO /p:CSharpToolVersion=$CSharpToolVersion --include-source --include-symbols
	if (-not $?) {
		exit 0x23
	}
}

if (-not $NoValidate) {
	$frameworks= 'netstandard1.5', 'netstandard2.0', 'netstandard2.1', 'net45', 'net50'

	Write-Host "`n`n********************************************************`n* Validate code generation using the Java code generator`n********************************************************`n"
	if (-not $NoClean) {
		git clean -dxf DotnetValidationJavaCodegen
		dotnet clean --nologo --verbosity quite .\DotnetValidationJavaCodegen\DotnetValidation.sln
	}
	dotnet build --nologo --verbosity $Verbosity --configuration $buildConfig .\DotnetValidationJavaCodegen\DotnetValidation.sln
	if (-not $?) {
		Write-Error 'Build failed, aborting!'
		exit 0x22
	}

	foreach ($framework in $frameworks) {
		dotnet run --nologo --no-build --configuration $buildConfig --framework $framework .\DotnetValidationJavaCodegen\DotnetValidation.sln
		if (-not $?) {
			Write-Error 'Validation failed!'
			$validationFailed = $true
		}
	}
	if ($validationFailed) {
		Write-Error 'One or more Java codegen validations failed, aborting!'
		exit 0x24
	}

	Write-Host "`n`n******************************************************`n* Validate code generation using the C# code generator`n******************************************************`n"

	if (-not $NoClean) {
		git clean -dxf DotnetValidationSingleTarget
	}
	dotnet run --project .\DotnetValidationSingleTarget\DotnetValidation.csproj --framework netcoreapp1.1
	if (-not $?) {
		Write-Error 'Build failed, aborting!'
		exit 0x24
	}

	if (-not $NoClean) {
		git clean -dxf DotnetValidationSingleTarget
		dotnet clean --nologo --verbosity quite .\DotnetValidationSingleTarget\DotnetValidation.sln
	}
	dotnet build --nologo --verbosity $Verbosity --configuration $buildConfig .\DotnetValidationSingleTarget\DotnetValidation.sln
	if (-not $?) {
		Write-Error 'Build failed, aborting!'
		exit 0x24 
	}

	if (-not $NoClean) {
		git clean -dxf DotnetValidation
		dotnet clean --nologo --verbosity quite .\DotnetValidation\DotnetValidation.sln
	}
	dotnet build --nologo --verbosity $Verbosity --configuration $buildConfig .\DotnetValidation\DotnetValidation.sln
	if (-not $?) {
		Write-Error 'Build failed, aborting!'
		exit 0x22
	}

	foreach ($framework in $frameworks) {
		dotnet run --nologo --no-build --configuration $buildConfig --framework $framework .\DotnetValidation\DotnetValidation.sln
		if (-not $?) {
			Write-Error 'Validation failed!'
			$validationFailed = $true
		}
	}
	if ($validationFailed) {
		Write-Error 'One or more C# codegen validations failed, aborting!'
		exit 0x24
	}
}
