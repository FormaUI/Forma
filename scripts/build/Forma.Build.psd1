@{
    RootModule        = 'Forma.Build.psm1'
    ModuleVersion     = '1.0.0'
    GUID              = 'a3c1f7e2-9b04-4d6a-8e35-2f7c5b1d90aa'
    Author            = 'Eyal Alon'
    CompanyName       = 'FormaUI'
    Copyright         = '(c) Eyal Alon. Licensed under the MIT licence.'
    Description       = 'What the Forma build does: the gate and every action it is made of.'
    PowerShellVersion = '7.0'

    # This list is the only thing that decides what the module offers. Every function in it is written in
    # one file, so a helper the actions share, such as the one that finds the task programs, stays out of
    # the build's surface by being left out here rather than by being kept somewhere separate. Adding a
    # function to the module therefore publishes nothing until it is named below.
    FunctionsToExport = @(
        'Initialize-BuildContext',
        'Build-Solution',
        'Build-TaskProgram',
        'Clear-BuildOutput',
        'Invoke-Gate',
        'Test-BrowserCheck',
        'Test-BrowserScript',
        'Test-MobileExample',
        'Test-Solution',
        'Test-SolutionFormat',
        'Test-SolutionLocale',
        'Write-BuildSummary'
    )

    CmdletsToExport   = @()
    VariablesToExport = @()
    AliasesToExport   = @()
}
