namespace Forma.Tests.Sanitization;

/// <summary>Addresses that run script where a page writes them, each spelled as an attacker would.</summary>
public static class HostileAddresses
{
    /// <summary>A script's address: plain, in mixed case, behind spaces and control characters, split by a tab, in VBScript, and as a page of HTML.</summary>
    public static IReadOnlyList<string> Scripts { get; } =
    [
        "javascript:alert(1)",
        "JaVaScRiPt:alert(1)",
        " \u0001\u001fjavascript:alert(1)",
        "java\tscript:alert(1)",
        "vbscript:msgbox(1)",
        "data:text/html,<script>alert(1)</script>",
    ];
}
