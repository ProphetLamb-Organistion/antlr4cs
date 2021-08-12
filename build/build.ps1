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
	Write-Error "The script was run from an invalid working directory."
	exit 0x05
}

. .\version.ps1

if ($Debug) {
	$BuildConfig = 'Debug'
} Else {
	$BuildConfig = 'Release'
}

if ($AntlrVersion.Contains('-')) {
	$KeyConfiguration = 'Dev'
} Else {
	$KeyConfiguration = 'Final'
}

if ($NoClean) {
	$Target = 'build'
} Else {
	$Target = 'rebuild'
}

if (-not $MavenHome) {
	$MavenHome = $env:M2_HOME
}

if (-not $UseMsBuild -and $env:DOTNET_CLI_TELEMETRY_OPTOUT -ne 1 ) {
	Write-Warning "Friendly reminder to opt-out of telemetry reporting.`n No more reporting back to the mothership!"
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
	if (-not $_nuget) {
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
$nuget=GetNugetPath

# Build the Java library using Maven
if (-not $SkipMaven) {
	$OriginalPath = $PWD

	Set-Location '..\tool'
	$MavenPath = "$MavenHome\bin\mvn.cmd"
	If (-not (Test-Path $MavenPath)) {
		$MavenPath = "$MavenHome\bin\mvn.bat"
	}

	If (-not (Test-Path $MavenPath)) {
		Write-Error "Couldn't locate Maven binary: $MavenPath"
		Set-Location $OriginalPath
		exit 0x13
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
		Write-Error 'Maven build of the C# Target custom Tool failed, aborting!'
		Set-Location $OriginalPath
		exit 0x20
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

if ($Logger) {
	$LoggerArgument = "/logger:$Logger"
}
if ($UseMsBuild) {
	&$nuget 'restore' $SolutionPath -Project2ProjectTimeOut 1200
	if (-not $?) {
		Write-Error 'Build failed, aborting!'
		exit 0x22
	}
	&$msbuild '/nologo' '/m' '/nr:false' "/t=$Target" $LoggerArgument "/verbosity=$Verbosity" "/p:Configuration=$BuildConfig" $visualStudioVersionOption "/p:KeyConfiguration=$KeyConfiguration" $SolutionPath
} else {
	if (-not $NoClean) {
		dotnet 'clean' '--nologo' $SolutionPath
	}
	dotnet 'build' '--nologo' "--verbosity $Verbosity" "--configuration=$BuildConfig" $SolutionPath
}
if (-not $?) {
	Write-Error 'Build failed, aborting!'
	exit 0x22
}

if (-not (Test-Path 'nuget')) {
	mkdir "nuget"
}

$JarPath = "..\tool\target\antlr4-csharp-$CSharpToolVersion-complete.jar"
if (!(Test-Path $JarPath)) {
	Write-Error "Couldn't locate the complete jar used for building C# parsers: $JarPath"
	exit 0x14
}

# By default, do not create a NuGet package unless the expected strong name key files were used
if (-not $SkipKeyCheck) {
	. .\keys.ps1

	foreach ($pair in $Keys.GetEnumerator()) {
		$assembly = Resolve-FullPath -Path "..\runtime\CSharp\Antlr4.Runtime\bin\$BuildConfig\$($pair.Key)\Antlr4.Runtime.dll"

		# Run the actual check in a separate process or the current process will keep the assembly file locked
		powershell -Command ".\check-key.ps1 -Assembly '$assembly' -ExpectedKey '$($pair.Value)' -Build '$($pair.Key)'"
		if (-not $?) {
			exit 0x30
		}
	}
}

$packages = @(
	'Antlr4.CodeGenerator'
	'Antlr4')

ForEach ($package in $packages) {
	If (-not (Test-Path ".\$package.nuspec")) {
		Write-Error "Couldn't locate NuGet package specification: $package"
		exit 0x15
	}

	&$nuget 'pack' ".\$package.nuspec" '-OutputDirectory' 'nuget' '-Prop' "Configuration=$BuildConfig" '-Version' "$AntlrVersion" '-Prop' "M2_REPO=$M2_REPO" '-Prop' "CSharpToolVersion=$CSharpToolVersion" '-Symbols'
	if (-not $?) {
		exit 0x23
	}
}

Write-Host "`n`n********************************************************`n* Validate code generation using the Java code generator`n********************************************************`n"
if (-not $NoValidate) {
	git 'clean' '-dxf' 'DotnetValidationJavaCodegen'
	if ($UseMsBuild) {
		&$nuget 'restore' 'DotnetValidationJavaCodegen'
		&$msbuild '/nologo' '/m' '/nr:false' '/t:Rebuild' $LoggerArgument "/verbosity=$Verbosity" "/p:Configuration=$BuildConfig" '.\DotnetValidationJavaCodegen\DotnetValidation.sln'
	} else {
		dotnet 'clean' '--nologo' '.\DotnetValidationJavaCodegen\DotnetValidation.sln'
		dotnet 'build' '--nologo' $LoggerArgument "--verbosity $Verbosity" "--configuration $BuildConfig" '.\DotnetValidationJavaCodegen\DotnetValidation.sln'
	}
	if (-not $?) {
		Write-Error 'Build failed, aborting!'
		exit 0x22
	}

	dotnet 'run'  '--framework netcoreapp1.5' "--configuration $BuildConfig" '.\DotnetValidationJavaCodegen\DotnetValidation.csproj'
	if (-not $?) {
		Write-Error 'Validation failed!'
		$validationFailed = $true
	}

	dotnet 'run' '--framework netcoreapp2.0' "--configuration $BuildConfig" '.\DotnetValidationJavaCodegen\DotnetValidation.csproj'
	if (-not $?) {
		Write-Error 'Validation failed!'
		$validationFailed = $true
	}

	dotnet 'run' '--framework netcoreapp2.1' "--configuration $BuildConfig" '.\DotnetValidationJavaCodegen\DotnetValidation.csproj'
	if (-not $?) {
		Write-Error 'Validation failed!'
		$validationFailed = $true
	}

	dotnet 'run' '--framework net45' "--configuration $BuildConfig" '.\DotnetValidationJavaCodegen\DotnetValidation.csproj'
	if (-not $?) {
		Write-Error 'Validation failed!'
		$validationFailed = $true
	}

	dotnet 'run' '--framework net5.0' "--configuration $BuildConfig" '.\DotnetValidationJavaCodegen\DotnetValidation.csproj'
	if (-not $?) {
		Write-Error 'Validation failed!'
		$validationFailed = $true
	}

	if ($validationFailed) {
		Write-Error 'One or more Java codegen validations failed, aborting!'
		exit 0x24
	}
}

Write-Host "`n`n******************************************************`n* Validate code generation using the C# code generator`n******************************************************`n"
if (-not $NoValidate) {
	git 'clean' '-dxf' 'DotnetValidation'
	if ($UseMsBuild) {
		&$nuget 'restore' 'DotnetValidation'
		&$msbuild '/nologo' '/m' '/nr:false' '/t:Rebuild' $LoggerArgument "/verbosity=$Verbosity" "/p:Configuration=$BuildConfig" '.\DotnetValidation\DotnetValidation.sln'
	} else {
		dotnet 'clean' '--nologo' '.\DotnetValidation\DotnetValidation.sln'
		dotnet 'build' '--nologo' $LoggerArgument "--verbosity $Verbosity" "--configuration $BuildConfig" '.\DotnetValidation\DotnetValidation.sln'
	}
	if (-not $?) {
		Write-Error 'Build failed, aborting!'
		exit 0x22
	}

	".\DotnetValidation\bin\$BuildConfig\netstandard1.5\DotnetValidation.exe"
	if (-not $?) {
		Write-Error 'Validation failed!'
		$validationFailed = $true
	}

	".\DotnetValidation\bin\$BuildConfig\netstandard2.0\DotnetValidation.exe"
	if (-not $?) {
		Write-Error 'Validation failed!'
		$validationFailed = $true
	}

	".\DotnetValidation\bin\$BuildConfig\netstandard2.1\DotnetValidation.exe"
	if (-not $?) {
		Write-Error 'Validation failed!'
		$validationFailed = $true
	}

	".\DotnetValidation\bin\$BuildConfig\net45\DotnetValidation.exe"
	if (-not $?) {
		Write-Error 'Validation failed!'
		$validationFailed = $true
	}

	".\DotnetValidation\bin\$BuildConfig\net50\DotnetValidation.exe"
	if (-not $?) {
		Write-Error 'Validation failed!'
		$validationFailed = $true
	}

	if ($validationFailed) {
		Write-Error 'One or more C# codegen validations failed, aborting!'
		exit 0x24
	}
}

# Validate code generation using the C# code generator (single target framework)
if (-not $NoValidate) {
	git 'clean' '-dxf' 'DotnetValidationSingleTarget'
	dotnet 'run' '--project' '.\DotnetValidationSingleTarget\DotnetValidation.csproj' '--framework' 'netcoreapp1.1'
	if (-not $?) {
		Write-Error 'Build failed, aborting!'
		exit 0x24
	}

	git 'clean' '-dxf' 'DotnetValidationSingleTarget'
	if ($UseMsBuild) {
		&$nuget 'restore' 'DotnetValidationSingleTarget'
		&$msbuild '/nologo' '/m' '/nr:false' '/t:Rebuild' $LoggerArgument "/verbosity=$Verbosity" "/p:Configuration=$BuildConfig" '.\DotnetValidationSingleTarget\DotnetValidation.sln'
	} else {
		dotnet 'clean' '--nologo' '.\DotnetValidationSingleTarget\DotnetValidation.sln'
		dotnet 'build' '--nologo' $LoggerArgument "--verbosity $Verbosity" "--configuration $BuildConfig" '.\DotnetValidationSingleTarget\DotnetValidation.sln'
	}
	if (-not $?) {
		Write-Error 'Build failed, aborting!'
		exit 0x24 
	}
}
