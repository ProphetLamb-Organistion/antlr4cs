# Note: these values may only change during major release

If ($AntlrVersion.Contains('-')) {

	# Use the development keys
	$Keys = @{
		'netstandard1.5' = 'e9931a4108ef2354'
		'netstandard2.0' = 'e9931a4108ef2354'
		'netstandard2.1' = 'e9931a4108ef2354'
		'net45' = 'e9931a4108ef2354'
		'net50' = 'e9931a4108ef2354'
	}

} Else {

	# Use the final release keys
	$Keys = @{
		'netstandard1.5' = '09abb75b9ed49849'
		'netstandard2.0' = '09abb75b9ed49849'
		'netstandard2.1' = '09abb75b9ed49849'
		'net45' = '09abb75b9ed49849'
		'net50' = '09abb75b9ed49849'
	}

}

function Resolve-FullPath() {
	param([string]$Path)
	[System.IO.Path]::GetFullPath((Join-Path (pwd) $Path))
}
