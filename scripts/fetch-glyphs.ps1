$ErrorActionPreference = 'Stop'

# Regenerates FormaGlyph.razor.cs from the Font Awesome Free source SVGs.

$map = [ordered]@{
    ArrowDown            = 'solid/arrow-down'
    ArrowDownAZ          = 'solid/arrow-down-a-z'
    ArrowLeft            = 'solid/arrow-left'
    ArrowRight           = 'solid/arrow-right'
    ArrowUp              = 'solid/arrow-up'
    ArrowUpZA            = 'solid/arrow-up-z-a'
    Ban                  = 'solid/ban'
    BorderAll            = 'solid/border-all'
    BoxOpen              = 'solid/box-open'
    CartShopping         = 'solid/cart-shopping'
    Check                = 'solid/check'
    ChevronDown          = 'solid/chevron-down'
    ChevronRight         = 'solid/chevron-right'
    CircleCheck          = 'solid/circle-check'
    CircleExclamation    = 'solid/circle-exclamation'
    CircleInfo           = 'solid/circle-info'
    CircleXmark          = 'solid/circle-xmark'
    Clock                = 'regular/clock'
    CloudArrowUp         = 'solid/cloud-arrow-up'
    CreditCard           = 'solid/credit-card'
    Ellipsis             = 'solid/ellipsis'
    EllipsisVertical     = 'solid/ellipsis-vertical'
    Eye                  = 'solid/eye'
    EyeSlash             = 'solid/eye-slash'
    File                 = 'solid/file'
    FileLines            = 'solid/file-lines'
    FileZipper           = 'solid/file-zipper'
    Film                 = 'solid/film'
    Folder               = 'solid/folder'
    FolderOpen           = 'solid/folder-open'
    FolderPlus           = 'solid/folder-plus'
    Forward              = 'solid/forward'
    Gear                 = 'solid/gear'
    HourglassHalf        = 'solid/hourglass-half'
    Image                = 'solid/image'
    Images               = 'solid/images'
    List                 = 'solid/list'
    MagnifyingGlass      = 'solid/magnifying-glass'
    Music                = 'solid/music'
    Pen                  = 'solid/pen'
    Play                 = 'solid/play'
    RotateLeft           = 'solid/rotate-left'
    RotateRight          = 'solid/rotate-right'
    SquareCheck          = 'solid/square-check'
    SquareMinus          = 'solid/square-minus'
    SquarePlus           = 'solid/square-plus'
    TableList            = 'solid/table-list'
    Trash                = 'solid/trash'
    TriangleExclamation  = 'solid/triangle-exclamation'
    Truck                = 'solid/truck'
    Users                = 'solid/users'
    Xmark                = 'solid/xmark'
}

$arms = New-Object System.Collections.Generic.List[string]

foreach ($name in $map.Keys) {
    $slug = $map[$name]
    $url = "https://raw.githubusercontent.com/FortAwesome/Font-Awesome/6.x/svgs/$slug.svg"
    $svg = (Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 30).Content

    if ($svg -notmatch 'viewBox="([^"]+)"') { throw "no viewBox for $slug" }
    $viewBox = $Matches[1]

    $paths = [regex]::Matches($svg, '<path[^>]*\sd="([^"]+)"')
    if ($paths.Count -ne 1) { throw "expected 1 path for $slug but found $($paths.Count)" }
    $d = $paths[0].Groups[1].Value

    $arms.Add("            Glyph.$name => (`"$viewBox`", `"$d`"),")
    Write-Host ("{0,-20} {1}" -f $name, $slug)
}

$body = $arms -join "`n"

$file = @"
namespace Forma.UI.Components;

using Forma.Framework.Style.Glyphs;

using Microsoft.AspNetCore.Components;

/// <summary>Draws one of the library's own glyphs as inline SVG, so no icon font has to be loaded.</summary>
public sealed partial class FormaGlyph : ComponentBase
{
    /// <summary>Which glyph to draw.</summary>
    [Parameter, EditorRequired] public Glyph Name { get; set; }

    /// <summary>Extra classes applied to the SVG element.</summary>
    [Parameter] public string? Class { get; set; }

    private string CssClass
        => string.IsNullOrWhiteSpace(Class) ? "forma-glyph" : `$"forma-glyph {Class}";

    private string ViewBox => Art.ViewBox;

    private string PathData => Art.PathData;

    // Artwork from Font Awesome Free 6 (CC BY 4.0); regenerate with scripts/fetch-glyphs.ps1.
    private (string ViewBox, string PathData) Art
        => Name switch
        {
$body
            _ => ("0 0 512 512", string.Empty)
        };
}
"@

$target = Join-Path $PSScriptRoot '..' 'src' 'Forma.UI.Blazor' 'Components' 'FormaGlyph.razor.cs'
$normalized = ($file -replace "`r`n", "`n").TrimEnd("`n") + "`n"
[System.IO.File]::WriteAllText($target, $normalized, (New-Object System.Text.UTF8Encoding $false))
Write-Host "`nWrote $($arms.Count) glyphs to $target"
