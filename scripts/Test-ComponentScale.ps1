#!/usr/bin/env pwsh
#Requires -Version 7
<#
.SYNOPSIS
    Measures the controls Forma’s components render and reports any standing off the scale.
.DESCRIPTION
    Opens each Showcase page in a browser, reads the rendered height of every button, input, select and
    switch in the page's content, and lists those standing on none of the target sizes. Pass -Show to
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

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script = Join-Path $PSScriptRoot 'cs' 'CheckComponentScale.cs'
$arguments = @($BaseUrl)

if ($Show) {
    $arguments += '--show'
}

dotnet run $script -- @arguments
exit $LASTEXITCODE
