#Requires -Version 7
<#
.SYNOPSIS
    Measures every control the Showcase renders and reports any whose height is off the control scale.
.DESCRIPTION
    Opens each Showcase page in a browser, reads the rendered height of every button, input, select and
    switch in the page's content, and lists those that stand on none of the control tokens. Pass -Show to
    watch the browser as it goes.
.PARAMETER BaseUrl
    Where the Showcase is listening.
.PARAMETER Show
    Runs the browser visibly, slowed enough to follow.
#>
[CmdletBinding()]
param(
    [string] $BaseUrl = 'http://localhost:5000',
    [switch] $Show
)

$ErrorActionPreference = 'Stop'

$script = Join-Path $PSScriptRoot 'ControlScale.cs'
$arguments = @($BaseUrl)

if ($Show) {
    $arguments += '--show'
}

dotnet run $script -- @arguments
exit $LASTEXITCODE
