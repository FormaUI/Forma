$ErrorActionPreference = 'Stop'

# Regenerates FormaGlyph.razor.cs from the Phosphor Icons source SVGs.
# Each glyph is fetched in every style the library draws.

$map = [ordered]@{
    AlignCenter         = 'text-align-center'
    AlignJustify        = 'text-align-justify'
    AlignLeft           = 'text-align-left'
    AlignRight          = 'text-align-right'
    Anchor              = 'anchor-simple'
    AnchorCircleXmark   = 'minus-circle'
    ArrowDown           = 'arrow-down'
    ArrowDownAZ         = 'sort-ascending'
    ArrowLeft           = 'arrow-left'
    ArrowLeftLong       = 'arrow-line-left'
    ArrowRight          = 'arrow-right'
    ArrowRightArrowLeft = 'arrows-left-right'
    ArrowRightLong      = 'arrow-line-right'
    ArrowsRotate        = 'arrows-clockwise'
    ArrowUp             = 'arrow-up'
    ArrowUpZA           = 'sort-descending'
    Ban                 = 'prohibit'
    Bars                = 'rows'
    Bold                = 'text-b'
    BorderAll           = 'grid-nine'
    BoxOpen             = 'package'
    Bug                 = 'bug'
    CartShopping        = 'shopping-cart'
    Check               = 'check'
    ChevronDown         = 'caret-down'
    ChevronLeft         = 'caret-left'
    ChevronRight        = 'caret-right'
    ChevronUp           = 'caret-up'
    CircleCheck         = 'check-circle'
    CircleDot           = 'radio-button'
    CircleExclamation   = 'warning-circle'
    CircleInfo          = 'info'
    CirclePlay          = 'play-circle'
    CircleXmark         = 'x-circle'
    Clock               = 'clock'
    ClockRotateLeft     = 'clock-counter-clockwise'
    CloudArrowUp        = 'cloud-arrow-up'
    Code                = 'code'
    Comment             = 'chat-circle'
    ColumnsPlusLeft     = 'columns-plus-left'
    ColumnsPlusRight    = 'columns-plus-right'
    Copy                = 'copy'
    CreditCard          = 'credit-card'
    Download            = 'download-simple'
    Ellipsis            = 'dots-three'
    EllipsisVertical    = 'dots-three-vertical'
    Eye                 = 'eye'
    EyeSlash            = 'eye-slash'
    FaceSmile           = 'smiley'
    File                = 'file'
    FileCode            = 'file-code'
    FileLines           = 'file-text'
    FileZipper          = 'file-zip'
    Film                = 'film-strip'
    Filter              = 'funnel'
    Flag                = 'flag'
    FloppyDisk          = 'floppy-disk'
    Folder              = 'folder'
    FolderOpen          = 'folder-open'
    FolderPlus          = 'folder-plus'
    Font                = 'text-aa'
    Forward             = 'fast-forward'
    Futbol              = 'soccer-ball'
    Gear                = 'gear'
    Heading             = 'text-h'
    Heart               = 'heart'
    Highlighter         = 'highlighter'
    HourglassHalf       = 'hourglass'
    Image               = 'image'
    Images              = 'images'
    Indent              = 'text-indent'
    Italic              = 'text-italic'
    Keyboard            = 'keyboard'
    Lightbulb           = 'lightbulb'
    Link                = 'link'
    LinkSimpleBreak     = 'link-simple-break'
    LinkSlash           = 'link-break'
    List                = 'list'
    ListCheck           = 'list-checks'
    ListOl              = 'list-numbers'
    ListUl              = 'list-bullets'
    LocationCrosshairs  = 'crosshair'
    MagnifyingGlass     = 'magnifying-glass'
    Maximize            = 'arrows-out'
    Minimize            = 'arrows-in'
    Minus               = 'minus'
    Music               = 'music-note'
    Outdent             = 'text-outdent'
    Palette             = 'palette'
    Paragraph           = 'paragraph'
    Paste               = 'clipboard-text'
    Paw                 = 'paw-print'
    Pen                 = 'pencil-simple'
    PenToSquare         = 'note-pencil'
    Plane               = 'airplane'
    Play                = 'play'
    Plus                = 'plus'
    QuoteLeft           = 'quotes'
    RightToBracket      = 'sign-in'
    RotateLeft          = 'arrow-counter-clockwise'
    RotateRight         = 'arrow-clockwise'
    RowsPlusBottom      = 'rows-plus-bottom'
    RowsPlusTop         = 'rows-plus-top'
    Scissors            = 'scissors'
    Sitemap             = 'tree-structure'
    SpellCheck          = 'check-fat'
    SquareCheck         = 'check-square'
    SquareMinus         = 'minus-square'
    SquarePlus          = 'plus-square'
    Strikethrough       = 'text-strikethrough'
    Subscript           = 'text-subscript'
    Superscript         = 'text-superscript'
    Table               = 'table'
    TableCells          = 'grid-four'
    TableColumns        = 'columns'
    TableList           = 'list-dashes'
    Terminal            = 'terminal-window'
    TextHeight          = 'text-t'
    TextSlash           = 'text-t-slash'
    TextWidth           = 'arrows-vertical'
    Trash               = 'trash'
    TriangleExclamation = 'warning'
    Truck               = 'truck'
    Underline           = 'text-underline'
    Upload              = 'upload-simple'
    User                = 'user'
    Users               = 'users'
    Utensils            = 'fork-knife'
    WandMagicSparkles   = 'magic-wand'
    Wrench              = 'wrench'
    Xmark               = 'x'
}

# What Phosphor appends to a slug for each style; regular carries no suffix.
$styles = [ordered]@{
    Regular = ''
    Fill    = '-fill'
}

$ViewBox = '0 0 256 256'
$arms = [ordered]@{}

foreach ($style in $styles.Keys) {
    $suffix = $styles[$style]
    $folder = $style.ToLowerInvariant()
    $lines = New-Object System.Collections.Generic.List[string]

    foreach ($name in $map.Keys) {
        $slug = $map[$name]
        $url = "https://raw.githubusercontent.com/phosphor-icons/core/main/assets/$folder/$slug$suffix.svg"
        $svg = (Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 30).Content

        if ($svg -notmatch 'viewBox="([^"]+)"') { throw "no viewBox for $slug ($style)" }
        if ($Matches[1] -ne $ViewBox) { throw "unexpected viewBox '$($Matches[1])' for $slug ($style)" }

        $paths = [regex]::Matches($svg, '<path[^>]*\sd="([^"]+)"[^>]*>')
        if ($paths.Count -ne 1) { throw "expected 1 path for $slug ($style) but found $($paths.Count)" }

        $lines.Add("            Glyph.$name => `"$($paths[0].Groups[1].Value)`",")
    }

    $arms[$style] = $lines -join "`n"
    Write-Host ("{0,-8} {1} glyphs" -f $style, $lines.Count)
}

$file = @"
namespace Forma.UI.Blazor.Components;

using Forma.UI.Style.Glyphs;

using Microsoft.AspNetCore.Components;

/// <summary>Draws one of the library's own glyphs as inline SVG, so no icon font has to be loaded.</summary>
public sealed partial class FormaGlyph : ComponentBase
{
    /// <summary>Which glyph to draw.</summary>
    [Parameter, EditorRequired] public Glyph Name { get; set; }

    /// <summary>How the glyph is drawn.</summary>
    [Parameter] public GlyphStyle Style { get; set; } = GlyphStyle.Regular;

    /// <summary>Extra classes applied to the SVG element.</summary>
    [Parameter] public string? Class { get; set; }

    private string CssClass
        => string.IsNullOrWhiteSpace(Class) ? "forma-glyph" : `$"forma-glyph {Class}";

    private static string ViewBox => "$ViewBox";

    private string PathData => Art;

    // Artwork from Phosphor Icons (MIT); regenerate with scripts/fetch-glyphs.ps1.
    private string Art
        => Style switch
        {
            GlyphStyle.Fill => Filled(Name),
            _ => Regular(Name),
        };

    private static string Regular(Glyph name)
        => name switch
        {
$($arms['Regular'])
            _ => string.Empty
        };

    private static string Filled(Glyph name)
        => name switch
        {
$($arms['Fill'])
            _ => string.Empty
        };
}
"@

$target = Join-Path $PSScriptRoot '..' 'src' 'Forma.UI.Blazor' 'Components' 'FormaGlyph.razor.cs'
$normalized = ($file -replace "`r`n", "`n").TrimEnd("`n") + "`n"
[System.IO.File]::WriteAllText($target, $normalized, (New-Object System.Text.UTF8Encoding $false))
Write-Host "`nWrote $($map.Count) glyphs in $($styles.Count) styles to $target"
