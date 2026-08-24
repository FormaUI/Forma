$ErrorActionPreference = 'Stop'

# Regenerates FormaGlyph.razor.cs from the Font Awesome Free source SVGs.

$map = [ordered]@{
    AlignCenter          = 'solid/align-center'
    AlignJustify         = 'solid/align-justify'
    AlignLeft            = 'solid/align-left'
    AlignRight           = 'solid/align-right'
    ArrowDown            = 'solid/arrow-down'
    ArrowDownAZ          = 'solid/arrow-down-a-z'
    ArrowLeft            = 'solid/arrow-left'
    ArrowLeftLong        = 'solid/arrow-left-long'
    ArrowRight           = 'solid/arrow-right'
    ArrowRightArrowLeft  = 'solid/arrow-right-arrow-left'
    ArrowRightLong       = 'solid/arrow-right-long'
    ArrowsRotate         = 'solid/arrows-rotate'
    ArrowUp              = 'solid/arrow-up'
    ArrowUpZA            = 'solid/arrow-up-z-a'
    Ban                  = 'solid/ban'
    Bold                 = 'solid/bold'
    BorderAll            = 'solid/border-all'
    BoxOpen              = 'solid/box-open'
    Bug                  = 'solid/bug'
    CartShopping         = 'solid/cart-shopping'
    Check                = 'solid/check'
    ChevronDown          = 'solid/chevron-down'
    ChevronLeft          = 'solid/chevron-left'
    ChevronRight         = 'solid/chevron-right'
    ChevronUp            = 'solid/chevron-up'
    CircleCheck          = 'solid/circle-check'
    CircleDot            = 'solid/circle-dot'
    CircleExclamation    = 'solid/circle-exclamation'
    CircleInfo           = 'solid/circle-info'
    CirclePlay           = 'solid/circle-play'
    CircleXmark          = 'solid/circle-xmark'
    Clock                = 'regular/clock'
    CloudArrowUp         = 'solid/cloud-arrow-up'
    Code                 = 'solid/code'
    Comment              = 'solid/comment'
    Copy                 = 'solid/copy'
    CreditCard           = 'solid/credit-card'
    Download             = 'solid/download'
    Ellipsis             = 'solid/ellipsis'
    EllipsisVertical     = 'solid/ellipsis-vertical'
    Eye                  = 'solid/eye'
    EyeSlash             = 'solid/eye-slash'
    File                 = 'solid/file'
    FileCode             = 'solid/file-code'
    FileLines            = 'solid/file-lines'
    FileZipper           = 'solid/file-zipper'
    Film                 = 'solid/film'
    Filter               = 'solid/filter'
    FloppyDisk           = 'solid/floppy-disk'
    Folder               = 'solid/folder'
    FolderOpen           = 'solid/folder-open'
    FolderPlus           = 'solid/folder-plus'
    Font                 = 'solid/font'
    Forward              = 'solid/forward'
    Gear                 = 'solid/gear'
    Heading              = 'solid/heading'
    HourglassHalf        = 'solid/hourglass-half'
    Highlighter          = 'solid/highlighter'
    Image                = 'solid/image'
    Images               = 'solid/images'
    Indent               = 'solid/indent'
    Italic               = 'solid/italic'
    Keyboard             = 'solid/keyboard'
    Lightbulb            = 'solid/lightbulb'
    Link                 = 'solid/link'
    List                 = 'solid/list'
    ListCheck            = 'solid/list-check'
    ListOl               = 'solid/list-ol'
    ListUl               = 'solid/list-ul'
    LocationCrosshairs   = 'solid/location-crosshairs'
    MagnifyingGlass      = 'solid/magnifying-glass'
    Maximize             = 'solid/maximize'
    Minimize             = 'solid/minimize'
    Minus                = 'solid/minus'
    Music                = 'solid/music'
    Outdent              = 'solid/outdent'
    Palette              = 'solid/palette'
    Paragraph            = 'solid/paragraph'
    Paste                = 'solid/paste'
    Pen                  = 'solid/pen'
    PenToSquare          = 'solid/pen-to-square'
    Play                 = 'solid/play'
    Plus                 = 'solid/plus'
    QuoteLeft            = 'solid/quote-left'
    RightToBracket       = 'solid/right-to-bracket'
    RotateLeft           = 'solid/rotate-left'
    RotateRight          = 'solid/rotate-right'
    Scissors             = 'solid/scissors'
    Sitemap              = 'solid/sitemap'
    SpellCheck           = 'solid/spell-check'
    SquareCheck          = 'solid/square-check'
    SquareMinus          = 'solid/square-minus'
    SquarePlus           = 'solid/square-plus'
    Strikethrough        = 'solid/strikethrough'
    Subscript            = 'solid/subscript'
    Superscript          = 'solid/superscript'
    Table                = 'solid/table'
    TableList            = 'solid/table-list'
    Terminal             = 'solid/terminal'
    TextHeight           = 'solid/text-height'
    TextSlash            = 'solid/text-slash'
    TextWidth            = 'solid/text-width'
    Trash                = 'solid/trash'
    TriangleExclamation  = 'solid/triangle-exclamation'
    Truck                = 'solid/truck'
    Underline            = 'solid/underline'
    Upload               = 'solid/upload'
    Users                = 'solid/users'
    WandMagicSparkles    = 'solid/wand-magic-sparkles'
    Wrench               = 'solid/wrench'
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
namespace Forma.UI.Blazor.Components;

using Forma.UI.Style.Glyphs;

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
