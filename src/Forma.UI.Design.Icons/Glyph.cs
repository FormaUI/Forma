namespace Forma.UI.Design.Icons;

/// <summary>A mark the library draws, named for what it means rather than for the set it is drawn from.</summary>
public enum Glyph
{
    /// <summary>No glyph. Asking a provider for this is an error.</summary>
    None = 0,

    /// <summary>Columns with one added to the left.</summary>
    AddColumnLeft,

    /// <summary>Columns with one added to the right.</summary>
    AddColumnRight,

    /// <summary>Rows with one added above.</summary>
    AddRowAbove,

    /// <summary>Rows with one added below.</summary>
    AddRowBelow,

    /// <summary>An aeroplane.</summary>
    Airplane,

    /// <summary>Stacked lines centred on a common axis, for centre alignment.</summary>
    AlignCenter,

    /// <summary>Stacked lines flush to both edges, for justified alignment.</summary>
    AlignJustify,

    /// <summary>Stacked lines flush to the start edge, for left alignment.</summary>
    AlignLeft,

    /// <summary>Stacked lines flush to the end edge, for right alignment.</summary>
    AlignRight,

    /// <summary>A ship's anchor, for a named position in a document.</summary>
    Anchor,

    /// <summary>A downward arrow.</summary>
    ArrowDown,

    /// <summary>A leftward arrow.</summary>
    ArrowLeft,

    /// <summary>A rightward arrow.</summary>
    ArrowRight,

    /// <summary>An upward arrow.</summary>
    ArrowUp,

    /// <summary>A quaver, for sound.</summary>
    Audio,

    /// <summary>A heavy letter B, for bold text.</summary>
    Bold,

    /// <summary>An insect, for a defect.</summary>
    Bug,

    /// <summary>Lines beside bullets, for an unordered list.</summary>
    BulletList,

    /// <summary>A tick.</summary>
    Check,

    /// <summary>A ticked circle.</summary>
    CheckCircle,

    /// <summary>A ticked square.</summary>
    CheckSquare,

    /// <summary>A downward chevron.</summary>
    ChevronDown,

    /// <summary>A leftward chevron.</summary>
    ChevronLeft,

    /// <summary>A rightward chevron.</summary>
    ChevronRight,

    /// <summary>An upward chevron.</summary>
    ChevronUp,

    /// <summary>A struck-through letter T, for clearing formatting.</summary>
    ClearFormatting,

    /// <summary>A clock face.</summary>
    Clock,

    /// <summary>A cross, for dismissing.</summary>
    Close,

    /// <summary>A cross inside a circle, for dismissing.</summary>
    CloseCircle,

    /// <summary>A cloud with an upward arrow, for uploading.</summary>
    CloudUpload,

    /// <summary>Opposing angle brackets, for source code.</summary>
    Code,

    /// <summary>A pane divided top to bottom, standing for a table's columns.</summary>
    Columns,

    /// <summary>A speech bubble, for a remark.</summary>
    Comment,

    /// <summary>Two stacked sheets, for duplicating.</summary>
    Copy,

    /// <summary>A payment card.</summary>
    CreditCard,

    /// <summary>Crosshairs, for pinpointing a position.</summary>
    Crosshair,

    /// <summary>A pair of shears, for cutting.</summary>
    Cut,

    /// <summary>A waste bin, for discarding.</summary>
    Delete,

    /// <summary>A downward arrow above a line, for downloading.</summary>
    Download,

    /// <summary>A pencil over a note, for editing or renaming.</summary>
    Edit,

    /// <summary>A smiling face, for picking an emoji.</summary>
    Emoji,

    /// <summary>An exclamation mark inside a circle, for a failure.</summary>
    Error,

    /// <summary>An open eye.</summary>
    Eye,

    /// <summary>A struck-through eye.</summary>
    EyeSlash,

    /// <summary>A pair of triangles, for skipping ahead.</summary>
    FastForward,

    /// <summary>A blank sheet.</summary>
    File,

    /// <summary>A sheet with a zip, for an archive.</summary>
    FileArchive,

    /// <summary>A sheet marked with angle brackets, for a source file.</summary>
    FileCode,

    /// <summary>A sheet ruled with lines.</summary>
    FileText,

    /// <summary>A funnel, for narrowing a set.</summary>
    Filter,

    /// <summary>A pennant on a pole.</summary>
    Flag,

    /// <summary>A closed folder.</summary>
    Folder,

    /// <summary>A folder with a plus, for adding one.</summary>
    FolderAdd,

    /// <summary>An open folder.</summary>
    FolderOpen,

    /// <summary>Two letter As, for typeface.</summary>
    Font,

    /// <summary>A letter T, for text size.</summary>
    FontSize,

    /// <summary>A fork and knife.</summary>
    ForkKnife,

    /// <summary>A square divided into cells, for a tile layout.</summary>
    GridView,

    /// <summary>A letter H, for a heading.</summary>
    Heading,

    /// <summary>A heart.</summary>
    Heart,

    /// <summary>A marker pen, for highlighted text.</summary>
    Highlight,

    /// <summary>A clock with a counter-clockwise arrow, for recent items.</summary>
    History,

    /// <summary>An hourglass, for waiting.</summary>
    Hourglass,

    /// <summary>A framed picture.</summary>
    Image,

    /// <summary>Two overlapping framed pictures.</summary>
    Images,

    /// <summary>Lines pushed toward the end edge, for increasing indentation.</summary>
    Indent,

    /// <summary>A letter i inside a circle.</summary>
    Information,

    /// <summary>A slanted letter I, for italic text.</summary>
    Italic,

    /// <summary>A keyboard.</summary>
    Keyboard,

    /// <summary>A light bulb.</summary>
    Lightbulb,

    /// <summary>Opposing vertical arrows, for line height.</summary>
    LineHeight,

    /// <summary>Two joined chain links.</summary>
    Link,

    /// <summary>Stacked lines, for a list.</summary>
    List,

    /// <summary>Lines beside dashes, for a detailed layout.</summary>
    ListView,

    /// <summary>A wand throwing sparkles, for an automatic fix or format.</summary>
    MagicWand,

    /// <summary>Arrows pointing outward to the four corners, for entering a full-viewport state.</summary>
    Maximize,

    /// <summary>Arrows pointing inward from the four corners, for leaving a full-viewport state.</summary>
    Minimize,

    /// <summary>A minus sign.</summary>
    Minus,

    /// <summary>A square holding a minus.</summary>
    MinusSquare,

    /// <summary>Three dots in a row, for more actions.</summary>
    More,

    /// <summary>Three stacked dots, for more actions.</summary>
    MoreVertical,

    /// <summary>Lines beside numerals, for an ordered list.</summary>
    NumberedList,

    /// <summary>Lines pulled toward the start edge, for decreasing indentation.</summary>
    Outdent,

    /// <summary>An open carton, for something being packed or handled.</summary>
    Package,

    /// <summary>A painter's palette, for theming.</summary>
    Palette,

    /// <summary>A pilcrow, for a paragraph block.</summary>
    Paragraph,

    /// <summary>A clipboard holding a sheet, for pasting.</summary>
    Paste,

    /// <summary>An animal paw print.</summary>
    PawPrint,

    /// <summary>A pencil.</summary>
    Pen,

    /// <summary>A play triangle.</summary>
    Play,

    /// <summary>A play triangle inside a circle.</summary>
    PlayCircle,

    /// <summary>A plus sign.</summary>
    Plus,

    /// <summary>A square holding a plus.</summary>
    PlusSquare,

    /// <summary>A crossed-out circle, for something refused.</summary>
    Prohibited,

    /// <summary>Quotation marks, for a quoted passage.</summary>
    Quote,

    /// <summary>A filled dot inside a circle, for a marked point.</summary>
    RadioButton,

    /// <summary>A clockwise arrow, for redoing.</summary>
    Redo,

    /// <summary>Two arrows chasing in a circle, for refreshing.</summary>
    Refresh,

    /// <summary>A broken chain of the simpler kind, for removing a named position.</summary>
    RemoveAnchor,

    /// <summary>A broken chain, for removing a link.</summary>
    RemoveLink,

    /// <summary>Stacked bars, standing for a table's rows.</summary>
    Rows,

    /// <summary>A floppy disk, for saving.</summary>
    Save,

    /// <summary>A magnifying glass, for searching.</summary>
    Search,

    /// <summary>A cogwheel, for configuration.</summary>
    Settings,

    /// <summary>A shopping trolley.</summary>
    ShoppingCart,

    /// <summary>An arrow entering a bracket, for signing in.</summary>
    SignIn,

    /// <summary>A football.</summary>
    SoccerBall,

    /// <summary>Bars ordered short to long, for ascending sort.</summary>
    SortAscending,

    /// <summary>Bars ordered long to short, for descending sort.</summary>
    SortDescending,

    /// <summary>A heavy tick, for checked spelling.</summary>
    SpellCheck,

    /// <summary>A struck-through letter S, for struck-out text.</summary>
    Strikethrough,

    /// <summary>A letter with a character set below the baseline.</summary>
    Subscript,

    /// <summary>A letter with a character set above the baseline.</summary>
    Superscript,

    /// <summary>Opposing horizontal arrows, for swapping or replacing.</summary>
    Swap,

    /// <summary>A grid of cells under a header, for a table.</summary>
    Table,

    /// <summary>Lines beside ticks, for a task list.</summary>
    TaskList,

    /// <summary>A prompt and cursor, for a terminal.</summary>
    Terminal,

    /// <summary>A rightward arrow to a bar, for left-to-right reading order.</summary>
    TextDirectionLtr,

    /// <summary>A leftward arrow to a bar, for right-to-left reading order.</summary>
    TextDirectionRtl,

    /// <summary>A node branching into two, for a hierarchy.</summary>
    TreeStructure,

    /// <summary>A delivery lorry.</summary>
    Truck,

    /// <summary>A letter U above a rule, for underlined text.</summary>
    Underline,

    /// <summary>An anticlockwise arrow, for undoing.</summary>
    Undo,

    /// <summary>An upward arrow above a line, for uploading.</summary>
    Upload,

    /// <summary>A single person.</summary>
    User,

    /// <summary>A pair of people.</summary>
    Users,

    /// <summary>A strip of film, for moving pictures.</summary>
    Video,

    /// <summary>An exclamation mark inside a triangle.</summary>
    Warning,

    /// <summary>A spanner, for configuration or repair.</summary>
    Wrench,
}
