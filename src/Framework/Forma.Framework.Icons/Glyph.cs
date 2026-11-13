namespace Forma.Framework.Icons;

/// <summary>A mark the library draws.</summary>
/// <remarks>A glyph is named for its recognizable form, or for that form's conventional meaning where the literal name would be obscure. A glyph whose form points along the reading direction carries <see cref="GlyphDirectionSensitiveAttribute"/> naming the direction it adapts for, and is drawn turned over when text runs that way.</remarks>
public enum Glyph
{
    /// <summary>No glyph.</summary>
    None = 0,

    /// <summary>A figure in a wheelchair, for accessibility.</summary>
    Accessibility,

    /// <summary>Columns with one added to the left.</summary>
    AddColumnLeft,

    /// <summary>Columns with one added to the right.</summary>
    AddColumnRight,

    /// <summary>Rows with one added above.</summary>
    AddRowAbove,

    /// <summary>Rows with one added below.</summary>
    AddRowBelow,

    /// <summary>An airplane flying down the page.</summary>
    AirplaneDown,

    /// <summary>An airplane flying left.</summary>
    AirplaneLeft,

    /// <summary>An airplane flying right.</summary>
    AirplaneRight,

    /// <summary>An airplane flying up the page.</summary>
    AirplaneUp,

    /// <summary>A clock with bells, for an alarm.</summary>
    AlarmClock,

    /// <summary>Stacked lines centered on a common axis, for center alignment.</summary>
    AlignCenter,

    /// <summary>Stacked lines flush to both edges, for justified alignment.</summary>
    AlignJustify,

    /// <summary>Stacked lines flush to the start edge, for left alignment.</summary>
    AlignLeft,

    /// <summary>Stacked lines flush to the end edge, for right alignment.</summary>
    AlignRight,

    /// <summary>A ship's anchor, for a named position in a document.</summary>
    Anchor,

    /// <summary>Nine dots in a square, for the applications launcher.</summary>
    Apps,

    /// <summary>A storage box, for archiving.</summary>
    Archive,

    /// <summary>A downward arrow.</summary>
    ArrowDown,

    /// <summary>A leftward arrow.</summary>
    [GlyphDirectionSensitive("rtl")]
    ArrowLeft,

    /// <summary>A rightward arrow.</summary>
    [GlyphDirectionSensitive("rtl")]
    ArrowRight,

    /// <summary>An arrow turning down and then right, for a line return.</summary>
    [GlyphDirectionSensitive("rtl")]
    ArrowTurnDownRight,

    /// <summary>An upward arrow.</summary>
    ArrowUp,

    /// <summary>An at sign, for an address or a mention.</summary>
    AtSign,

    /// <summary>A paper clip, for an attachment.</summary>
    Attachment,

    /// <summary>An eighth note, for audio.</summary>
    Audio,

    /// <summary>A lightning bolt, for an automatic mode.</summary>
    Auto,

    /// <summary>Bars of uneven spacing, for a barcode.</summary>
    Barcode,

    /// <summary>An empty battery.</summary>
    Battery,

    /// <summary>A battery filled to its end.</summary>
    BatteryFull,

    /// <summary>A bell, for notifications.</summary>
    Bell,

    /// <summary>A bell struck through, for notifications silenced.</summary>
    BellSlash,

    /// <summary>The Bluetooth rune.</summary>
    Bluetooth,

    /// <summary>A heavy letter B, for bold text.</summary>
    Bold,

    /// <summary>A ribbon, for a saved place.</summary>
    Bookmark,

    /// <summary>A square divided into four cells, for borders.</summary>
    Borders,

    /// <summary>A sun with a half-shaded disc, for brightness.</summary>
    Brightness,

    /// <summary>An antenna with waves either side, for broadcasting.</summary>
    Broadcast,

    /// <summary>An insect, for a defect.</summary>
    Bug,

    /// <summary>A handheld calculator.</summary>
    Calculator,

    /// <summary>A camera body with its lens.</summary>
    Camera,

    /// <summary>A shopping cart.</summary>
    Cart,

    /// <summary>A shopping cart wearing a check mark badge, for an order confirmed.</summary>
    CartCheck,

    /// <summary>A shopping cart wearing a cross badge, for an order canceled.</summary>
    CartCross,

    /// <summary>A shopping cart wearing a plus badge, for adding to it.</summary>
    CartPlus,

    /// <summary>A screen with waves rising from its corner, for casting to a display.</summary>
    Cast,

    /// <summary>A label tag, for a category.</summary>
    Category,

    /// <summary>A cent sign.</summary>
    Cent,

    /// <summary>A capital A beside a small a, for changing case.</summary>
    ChangeCase,

    /// <summary>A check mark.</summary>
    Check,

    /// <summary>A checked square.</summary>
    CheckSquare,

    /// <summary>A downward chevron.</summary>
    ChevronDown,

    /// <summary>A leftward chevron.</summary>
    [GlyphDirectionSensitive("rtl")]
    ChevronLeft,

    /// <summary>A rightward chevron.</summary>
    [GlyphDirectionSensitive("rtl")]
    ChevronRight,

    /// <summary>An upward chevron.</summary>
    ChevronUp,

    /// <summary>A chip with its pins, for hardware.</summary>
    Chip,

    /// <summary>A circle holding a check mark.</summary>
    CircleCheck,

    /// <summary>A circle holding a cross.</summary>
    CircleCross,

    /// <summary>A circle holding an exclamation mark, for a failure.</summary>
    CircleError,

    /// <summary>A circle holding a question mark, for help.</summary>
    CircleHelp,

    /// <summary>A circle holding a letter i, for information.</summary>
    CircleInfo,

    /// <summary>A circle holding a play triangle.</summary>
    CirclePlay,

    /// <summary>A ring holding a filled disc, for recording.</summary>
    CircleRecord,

    /// <summary>A circle holding a filled triangle, for running.</summary>
    CircleRun,

    /// <summary>A struck-through letter T, for clearing formatting.</summary>
    ClearFormatting,

    /// <summary>A clock face.</summary>
    Clock,

    /// <summary>A cloud.</summary>
    Cloud,

    /// <summary>A cloud with an upward arrow, for uploading.</summary>
    CloudUpload,

    /// <summary>Opposing angle brackets, for source code.</summary>
    Code,

    /// <summary>A line of nodes with one branching off, for a code branch.</summary>
    CodeBranch,

    /// <summary>A node on a line, for a code commit.</summary>
    CodeCommit,

    /// <summary>Two arrows turning past each other, for comparing code.</summary>
    CodeCompare,

    /// <summary>Two nodes joining into one, for a code fork.</summary>
    CodeFork,

    /// <summary>A branch curving back into its line, for a code merge.</summary>
    CodeMerge,

    /// <summary>A line of nodes beside an arrow turning into it, for a pull request.</summary>
    CodePullRequest,

    /// <summary>A coin, its face marked with an S.</summary>
    Coin,

    /// <summary>Two coins, one behind the other.</summary>
    Coins,

    /// <summary>Two arrows pointing at each other, for collapsing a section.</summary>
    Collapse,

    /// <summary>A square holding a minus, for collapsing a node.</summary>
    CollapseMinus,

    /// <summary>An eyedropper over a drop of what it picked, for picking a color.</summary>
    ColorPicker,

    /// <summary>A pane divided top to bottom, standing for a table's columns.</summary>
    Columns,

    /// <summary>A speech bubble, for a remark.</summary>
    Comment,

    /// <summary>A zipped file, for compressing into an archive.</summary>
    Compress,

    /// <summary>A bound book with a person on its cover, for contacts.</summary>
    Contacts,

    /// <summary>Two stacked sheets, for duplicating.</summary>
    Copy,

    /// <summary>The digits 1 and 2 followed by an ellipsis, for counting.</summary>
    Count,

    /// <summary>A solid box showing the digits 0, 1 and a 2 rolling to 3, for a counter.</summary>
    CountBox,

    /// <summary>A <see cref="CountBox"/> with a divider between each digit.</summary>
    CountDividedBox,

    /// <summary>A payment card.</summary>
    CreditCard,

    /// <summary>Two overlapping corner marks, for cropping.</summary>
    Crop,

    /// <summary>A cross.</summary>
    Cross,

    /// <summary>Crosshairs, for pinpointing a position.</summary>
    Crosshair,

    /// <summary>A crown with three points.</summary>
    Crown,

    /// <summary>A pointer arrow, for the cursor.</summary>
    Cursor,

    /// <summary>A pair of shears, for cutting.</summary>
    Cut,

    /// <summary>Four tiles of unequal height, for a dashboard.</summary>
    Dashboard,

    /// <summary>A folder with a star on it, for data.</summary>
    Data,

    /// <summary>A cylinder of stacked discs, for a database.</summary>
    Database,

    /// <summary>A beetle, for debugging.</summary>
    Debug,

    /// <summary>A waste bin, for discarding.</summary>
    Delete,

    /// <summary>A monitor on a stand, for a desktop computer.</summary>
    Desktop,

    /// <summary>Rows with columns, for a table-like view of items.</summary>
    DetailsView,

    /// <summary>A laptop beside a phone, for devices.</summary>
    Devices,

    /// <summary>A dollar sign, for money in dollars.</summary>
    DollarSign,

    /// <summary>A downward arrow above a line, for downloading.</summary>
    Download,

    /// <summary>A tray with a downward arrow, for the downloads folder.</summary>
    Downloads,

    /// <summary>Text lines beside a pencil, for a draft.</summary>
    Draft,

    /// <summary>Six raised dots, for a drag handle.</summary>
    DragHandle,

    /// <summary>A globe, for the world.</summary>
    Earth,

    /// <summary>A pencil over a note, for editing or renaming.</summary>
    Edit,

    /// <summary>A smiling face, for picking an emoji.</summary>
    Emoji,

    /// <summary>A circle struck through, for a container with nothing in it.</summary>
    Empty,

    /// <summary>A solid tilted eraser over the line it rubs out.</summary>
    Eraser,

    /// <summary>A euro sign, for money in euros.</summary>
    EuroSign,

    /// <summary>A calendar with marked days, for events.</summary>
    Events,

    /// <summary>A door with an arrow leaving it, for signing out.</summary>
    [GlyphDirectionSensitive("rtl")]
    Exit,

    /// <summary>Two arrows pointing away from each other, for expanding a section.</summary>
    Expand,

    /// <summary>A square holding a plus, for expanding a node.</summary>
    ExpandPlus,

    /// <summary>A jigsaw piece, for extensions.</summary>
    Extensions,

    /// <summary>A box with an arrow rising out of it, for extracting from an archive.</summary>
    Extract,

    /// <summary>An open eye.</summary>
    Eye,

    /// <summary>A solid eyedropper on the diagonal.</summary>
    Eyedropper,

    /// <summary>A struck-through eye.</summary>
    EyeSlash,

    /// <summary>Two triangles against a bar, for skipping ahead.</summary>
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

    /// <summary>Lines with a magnifying glass, for finding and replacing text.</summary>
    FindAndReplace,

    /// <summary>A pennant on a pole.</summary>
    Flag,

    /// <summary>A flashlight with light rays.</summary>
    Flashlight,

    /// <summary>A closed folder.</summary>
    Folder,

    /// <summary>A folder with a plus, for adding one.</summary>
    FolderAdd,

    /// <summary>An open folder.</summary>
    FolderOpen,

    /// <summary>A node branching into two, for a folder tree.</summary>
    FolderTree,

    /// <summary>Two letter As, for typeface.</summary>
    Font,

    /// <summary>A letter A over a bar, for the text color.</summary>
    FontColor,

    /// <summary>A letter T, for text size.</summary>
    FontSize,

    /// <summary>A letter A beside a downward arrow, for a smaller font size.</summary>
    FontSizeDecrease,

    /// <summary>A letter A beside an upward arrow, for a larger font size.</summary>
    FontSizeIncrease,

    /// <summary>A fork and knife.</summary>
    ForkKnife,

    /// <summary>An arrow bending up and to the right, for forwarding.</summary>
    [GlyphDirectionSensitive("rtl")]
    Forward,

    /// <summary>A large four-pointed star with a small one beside it, for generative AI.</summary>
    GenerativeAI,

    /// <summary>A pair of glasses with squared lenses.</summary>
    Glasses,

    /// <summary>A globe drawn as meridian and parallels, for language.</summary>
    Globe,

    /// <summary>A mortarboard, for education.</summary>
    Graduation,

    /// <summary>A square divided into cells, for a tile layout.</summary>
    GridView,

    /// <summary>Stacked layers, for grouping.</summary>
    Group,

    /// <summary>A fist with the little finger extended, for a call.</summary>
    HandCall,

    /// <summary>An open hand with impact marks, for applause.</summary>
    HandClap,

    /// <summary>A hand with two fingers raised, for peace.</summary>
    HandPeace,

    /// <summary>A hand holding a pen, for writing.</summary>
    HandPen,

    /// <summary>A hand pointing down.</summary>
    HandPointDown,

    /// <summary>A hand pointing left.</summary>
    HandPointLeft,

    /// <summary>A hand pointing right.</summary>
    HandPointRight,

    /// <summary>A hand pointing up.</summary>
    HandPointUp,

    /// <summary>An open left hand, palm forward.</summary>
    HandRaisedLeft,

    /// <summary>An open right hand, palm forward.</summary>
    HandRaisedRight,

    /// <summary>Two hands clasped, for an agreement.</summary>
    Handshake,

    /// <summary>A fist with the thumb turned down, for disapproval.</summary>
    HandThumbDown,

    /// <summary>A fist with the thumb turned up, for approval.</summary>
    HandThumbUp,

    /// <summary>An open hand with motion marks, for a greeting.</summary>
    HandWave,

    /// <summary>A hash sign, for a tag or a number.</summary>
    HashtagSign,

    /// <summary>A letter H, for a heading.</summary>
    Heading,

    /// <summary>A heart.</summary>
    Heart,

    /// <summary>A marker pen, for highlighted text.</summary>
    Highlight,

    /// <summary>A clock with a counterclockwise arrow, for recent items.</summary>
    History,

    /// <summary>A house, for the home page.</summary>
    Home,

    /// <summary>Two chain links side by side, for a link drawn across.</summary>
    HorizontalLink,

    /// <summary>An hourglass, for waiting.</summary>
    Hourglass,

    /// <summary>An hourglass with sand running through it, for progress.</summary>
    HourglassProgress,

    /// <summary>A framed picture.</summary>
    Image,

    /// <summary>Two overlapping framed pictures.</summary>
    Images,

    /// <summary>A hat and glasses, for private browsing.</summary>
    Incognito,

    /// <summary>Lines with a triangle pointing into them, for increasing indentation.</summary>
    [GlyphDirectionSensitive("rtl")]
    Indent,

    /// <summary>A magnifying glass with an eye in its lens, for inspecting.</summary>
    Inspect,

    /// <summary>A slanted letter I, for italic text.</summary>
    Italic,

    /// <summary>A key.</summary>
    Key,

    /// <summary>A line stepping down to a second line, the option key.</summary>
    KeyAlt,

    /// <summary>A tag pointing left with a cross inside, the backspace key.</summary>
    [GlyphDirectionSensitive("rtl")]
    KeyBackspace,

    /// <summary>A keyboard.</summary>
    Keyboard,

    /// <summary>A keyboard with a triangle at its left end, for docking it left.</summary>
    KeyboardLeft,

    /// <summary>A keyboard with a triangle at its right end, for docking it right.</summary>
    KeyboardRight,

    /// <summary>The shift arrow over a bar, the caps lock key.</summary>
    KeyCapsLock,

    /// <summary>A square with a loop at each corner, the command key.</summary>
    KeyCmd,

    /// <summary>A wide chevron pointing up, the control key.</summary>
    KeyCtrl,

    /// <summary>A hollow up arrow with a broad stem, the shift key.</summary>
    KeyShift,

    /// <summary>Four panes in a square, the Windows key.</summary>
    KeyWin,

    /// <summary>An open laptop.</summary>
    Laptop,

    /// <summary>A rope loop with a trailing end, for free selection.</summary>
    Lasso,

    /// <summary>A frame with a header and a sidebar, for the layout as a whole.</summary>
    Layout,

    /// <summary>A pane docked at the bottom.</summary>
    LayoutBottom,

    /// <summary>A pane docked on the left.</summary>
    LayoutLeft,

    /// <summary>A pane docked on the right.</summary>
    LayoutRight,

    /// <summary>A pane docked at the top.</summary>
    LayoutTop,

    /// <summary>A light bulb.</summary>
    Lightbulb,

    /// <summary>Opposing vertical arrows, for line height.</summary>
    LineHeight,

    /// <summary>Stacked lines beside a two-headed vertical arrow, for line spacing.</summary>
    LineSpacing,

    /// <summary>Two joined chain links.</summary>
    Link,

    /// <summary>Stacked lines, for a list.</summary>
    [GlyphDirectionSensitive("rtl")]
    List,

    /// <summary>Lines beside bullets, for an unordered list.</summary>
    [GlyphDirectionSensitive("rtl")]
    ListBullet,

    /// <summary>Lines with bullets at stepped indents, for a multilevel list.</summary>
    [GlyphDirectionSensitive("rtl")]
    ListMultilevel,

    /// <summary>Lines beside numerals, for an ordered list.</summary>
    ListNumbered,

    /// <summary>A navigation arrow, for the current location.</summary>
    LocationArrow,

    /// <summary>A location arrow inside a circle, for the current position.</summary>
    LocationArrowCircle,

    /// <summary>A closed padlock, for something locked.</summary>
    Lock,

    /// <summary>An open padlock, for something unlocked.</summary>
    LockOpen,

    /// <summary>A wand throwing sparkles, for an automatic fix or format.</summary>
    MagicWand,

    /// <summary>A mailbox holding a pencil, for a mail draft.</summary>
    MailDraft,

    /// <summary>An open mailbox with nothing in it.</summary>
    MailEmpty,

    /// <summary>A mailbox with an arrow settling into it, for a mail inbox.</summary>
    MailInbox,

    /// <summary>A mailbox holding a prohibited sign, for junk mail.</summary>
    MailJunk,

    /// <summary>A mailbox with an arrow rising from it, for a mail outbox.</summary>
    MailOutbox,

    /// <summary>A wrench over a gear, for maintenance.</summary>
    Maintenance,

    /// <summary>A division sign.</summary>
    MathDivide,

    /// <summary>An equals sign.</summary>
    MathEquals,

    /// <summary>A greater-than sign.</summary>
    MathGreaterThan,

    /// <summary>A greater-than-or-equal sign.</summary>
    MathGreaterThanOrEqual,

    /// <summary>An infinity sign.</summary>
    MathInfinity,

    /// <summary>A less-than sign.</summary>
    MathLessThan,

    /// <summary>A less-than-or-equal sign.</summary>
    MathLessThanOrEqual,

    /// <summary>A multiplication sign.</summary>
    MathMultiply,

    /// <summary>A not-equal sign.</summary>
    MathNotEqual,

    /// <summary>A percent sign.</summary>
    MathPercent,

    /// <summary>The letter pi.</summary>
    MathPi,

    /// <summary>A plus-or-minus sign.</summary>
    MathPlusMinus,

    /// <summary>A capital sigma, for a sum.</summary>
    MathSigma,

    /// <summary>A square-root sign.</summary>
    MathSquareRoot,

    /// <summary>Arrows pointing outward to the four corners, for entering a full-viewport state.</summary>
    Maximize,

    /// <summary>A medal hanging from its ribbon.</summary>
    Medal,

    /// <summary>A speech bubble with text lines, for a message.</summary>
    Message,

    /// <summary>A microphone, for voice input.</summary>
    Microphone,

    /// <summary>Arrows pointing inward from the four corners, for leaving a full-viewport state.</summary>
    Minimize,

    /// <summary>A minus sign.</summary>
    Minus,

    /// <summary>A mobile phone.</summary>
    Mobile,

    /// <summary>Three dots in a row, for more actions.</summary>
    More,

    /// <summary>Three stacked dots, for more actions.</summary>
    MoreVertical,

    /// <summary>A computer mouse.</summary>
    Mouse,

    /// <summary>Four arrows from one center, for moving.</summary>
    Move,

    /// <summary>A speaker struck through, for muting.</summary>
    Mute,

    /// <summary>Three equal bars, for opening the site's navigation.</summary>
    NavigationMenu,

    /// <summary>Nodes joined by lines, for a network.</summary>
    Network,

    /// <summary>Nodes joined in layers, for a neural network.</summary>
    NeuralNetwork,

    /// <summary>A square with an arrow leaving it, for opening elsewhere.</summary>
    [GlyphDirectionSensitive("rtl")]
    Open,

    /// <summary>Lines with a triangle pointing away from them, for decreasing indentation.</summary>
    [GlyphDirectionSensitive("rtl")]
    Outdent,

    /// <summary>An open carton, for something being packed or handled.</summary>
    Package,

    /// <summary>A paintbrush.</summary>
    Paintbrush,

    /// <summary>A paint can under its handle, paint dripping down its side, for filling.</summary>
    PaintBucket,

    /// <summary>A solid paint bucket tipped on its side with a drop beside it, for pouring.</summary>
    PaintBucketPour,

    /// <summary>A painter's palette, for theming.</summary>
    Palette,

    /// <summary>A pilcrow, for a paragraph block.</summary>
    Paragraph,

    /// <summary>A pilcrow beside a rightward arrow, for a left-to-right paragraph.</summary>
    ParagraphDirectionLtr,

    /// <summary>A pilcrow beside a leftward arrow, for a right-to-left paragraph.</summary>
    ParagraphDirectionRtl,

    /// <summary>A field with masked characters, for a password.</summary>
    PasswordField,

    /// <summary>A clipboard holding a sheet, for pasting.</summary>
    Paste,

    /// <summary>Two solid upright bars, for pausing.</summary>
    Pause,

    /// <summary>An animal paw print.</summary>
    PawPrint,

    /// <summary>A pencil.</summary>
    Pen,

    /// <summary>A solid pen nib pointing down, with its breather hole and slit, for paths.</summary>
    PenNib,

    /// <summary>A telephone handset, for a call.</summary>
    Phone,

    /// <summary>A telephone handset struck through, for no calls.</summary>
    PhoneSlash,

    /// <summary>A push pin, for pinning in place.</summary>
    Pin,

    /// <summary>Columns of cards, for planning.</summary>
    Plan,

    /// <summary>A plus sign.</summary>
    Plus,

    /// <summary>Bars of differing heights, for a poll.</summary>
    Poll,

    /// <summary>A pound sign, for money in pounds sterling.</summary>
    PoundSign,

    /// <summary>A power symbol.</summary>
    Power,

    /// <summary>A page with an eye on it, for previewing a document.</summary>
    Preview,

    /// <summary>A printer, for printing.</summary>
    Print,

    /// <summary>A crossed-out circle, for something refused.</summary>
    Prohibited,

    /// <summary>Sliders, for an item's properties.</summary>
    Properties,

    /// <summary>A rocket, for publishing.</summary>
    Publish,

    /// <summary>An opening and a closing quotation mark, for a quoted passage.</summary>
    [GlyphDirectionSensitive("rtl")]
    Quote,

    /// <summary>A filled dot inside a circle, for a marked point.</summary>
    RadioButton,

    /// <summary>A podium, for a ranking.</summary>
    Ranking,

    /// <summary>An opened envelope, for something read.</summary>
    Read,

    /// <summary>A clockwise arrow, for redoing.</summary>
    [GlyphDirectionSensitive("rtl")]
    Redo,

    /// <summary>Two arrows chasing in a circle, for refreshing.</summary>
    Refresh,

    /// <summary>A broken chain of the simpler kind, for removing a named position.</summary>
    RemoveAnchor,

    /// <summary>Two chain links side by side pulled apart, for removing a link drawn across.</summary>
    RemoveHorizontalLink,

    /// <summary>A broken chain, for removing a link.</summary>
    RemoveLink,

    /// <summary>An arrow bending up and to the left, for replying.</summary>
    [GlyphDirectionSensitive("rtl")]
    Reply,

    /// <summary>Two triangles pointing left against a bar, for skipping back.</summary>
    Rewind,

    /// <summary>A single clockwise arrow, for rotating.</summary>
    Rotate,

    /// <summary>Stacked bars, standing for a table's rows.</summary>
    Rows,

    /// <summary>A ruler with its scale marks.</summary>
    Ruler,

    /// <summary>A life ring, for rules.</summary>
    Rules,

    /// <summary>A floppy disk, for saving.</summary>
    Save,

    /// <summary>A magnifying glass, for searching.</summary>
    Search,

    /// <summary>A shield with a check mark, for security.</summary>
    Security,

    /// <summary>A dashed selection frame, for selecting.</summary>
    Select,

    /// <summary>A selection frame around every item, for selecting all.</summary>
    SelectAll,

    /// <summary>A selection frame struck through, for clearing a selection.</summary>
    SelectNone,

    /// <summary>A paper plane, for sending.</summary>
    [GlyphDirectionSensitive("rtl")]
    Send,

    /// <summary>A person inside corner brackets, for the current session.</summary>
    Session,

    /// <summary>A cogwheel, for configuration.</summary>
    Settings,

    /// <summary>A tilted paint can with a drip, for shading.</summary>
    Shading,

    /// <summary>Three joined nodes, for sharing.</summary>
    Share,

    /// <summary>An arrow rising out of an open box, for sharing.</summary>
    ShareBox,

    /// <summary>A shekel sign, for money in shekels.</summary>
    ShekelSign,

    /// <summary>Four rising bars, for signal strength.</summary>
    Signal,

    /// <summary>An arrow entering a bracket, for signing in.</summary>
    [GlyphDirectionSensitive("rtl")]
    SignIn,

    /// <summary>A box over three boxes, for a site map.</summary>
    Sitemap,

    /// <summary>A hand with its index finger pointing down and to the left, for smudging.</summary>
    Smudge,

    /// <summary>A bell with a sleeping mark, for snoozing.</summary>
    Snooze,

    /// <summary>A soccer ball.</summary>
    SoccerBall,

    /// <summary>Paired arrows, for sorting.</summary>
    Sort,

    /// <summary>An A over a Z beside a downward arrow, for sorting text ascending.</summary>
    SortAlphaAscending,

    /// <summary>A Z over an A beside an upward arrow, for sorting text descending.</summary>
    SortAlphaDescending,

    /// <summary>A 0 over a 9 beside a downward arrow, for sorting numbers ascending.</summary>
    SortNumericAscending,

    /// <summary>A 9 over a 0 beside an upward arrow, for sorting numbers descending.</summary>
    SortNumericDescending,

    /// <summary>A speaker with sound waves, for audio output.</summary>
    SoundOutput,

    /// <summary>A loudspeaker cabinet, for an audio device.</summary>
    Speaker,

    /// <summary>A heavy check mark, for checked spelling.</summary>
    SpellCheck,

    /// <summary>A frame divided in two, for splitting a view.</summary>
    Split,

    /// <summary>A rubber stamp on its base.</summary>
    Stamp,

    /// <summary>A five-point star, for a favorite.</summary>
    Star,

    /// <summary>A stopwatch with its crown and start lever, its hand on a marked face.</summary>
    Stopwatch,

    /// <summary>A stopwatch with its crown and start lever, showing a digit on a digital face.</summary>
    StopwatchDigital,

    /// <summary>A struck-through letter S, for struck-out text.</summary>
    Strikethrough,

    /// <summary>A letter with a character set below the baseline.</summary>
    Subscript,

    /// <summary>A letter with a character set above the baseline.</summary>
    Superscript,

    /// <summary>Opposing horizontal arrows, for swapping or replacing.</summary>
    Swap,

    /// <summary>Two arrows chasing counterclockwise, for syncing.</summary>
    Sync,

    /// <summary>A grid of cells under a header, for a table.</summary>
    Table,

    /// <summary>A tablet.</summary>
    Tablet,

    /// <summary>Lines beside check marks, for a task list.</summary>
    [GlyphDirectionSensitive("rtl")]
    TaskList,

    /// <summary>A prompt and cursor, for a terminal.</summary>
    Terminal,

    /// <summary>Lines of text, for plain text.</summary>
    [GlyphDirectionSensitive("rtl")]
    Text,

    /// <summary>A rightward arrow to a bar, for left-to-right reading order.</summary>
    TextDirectionLtr,

    /// <summary>A leftward arrow to a bar, for right-to-left reading order.</summary>
    TextDirectionRtl,

    /// <summary>A letter A with a sparkle, for text effects.</summary>
    TextGlow,

    /// <summary>A toolbox.</summary>
    Toolbox,

    /// <summary>A frame with a handle at each corner and an arrow inside, for transforming.</summary>
    Transform,

    /// <summary>A Han character beside a Latin A, for translation.</summary>
    Translation,

    /// <summary>A solid triangle pointing down.</summary>
    TriangleDown,

    /// <summary>A solid triangle pointing left.</summary>
    TriangleLeft,

    /// <summary>A solid triangle pointing right.</summary>
    TriangleRight,

    /// <summary>A solid triangle pointing up.</summary>
    TriangleUp,

    /// <summary>A delivery truck.</summary>
    Truck,

    /// <summary>A letter U above a rule, for underlined text.</summary>
    Underline,

    /// <summary>A counterclockwise arrow, for undoing.</summary>
    [GlyphDirectionSensitive("rtl")]
    Undo,

    /// <summary>A speaker with one wave, for unmuting.</summary>
    Unmute,

    /// <summary>A push pin struck through, for unpinning.</summary>
    Unpin,

    /// <summary>A closed envelope, for something unread.</summary>
    Unread,

    /// <summary>An upward arrow above a line, for uploading.</summary>
    Upload,

    /// <summary>A single person.</summary>
    User,

    /// <summary>A pair of people.</summary>
    Users,

    /// <summary>A strip of film, for moving pictures.</summary>
    Video,

    /// <summary>A wallet with its fold, pocket and clasp.</summary>
    Wallet,

    /// <summary>An exclamation mark inside a triangle.</summary>
    Warning,

    /// <summary>A spiral, for warping.</summary>
    Warp,

    /// <summary>Arcs over a dot, for a wireless network.</summary>
    Wifi,

    /// <summary>Corner brackets, for maximizing a window.</summary>
    WindowMaximize,

    /// <summary>An arrow down onto a line, for minimizing a window.</summary>
    WindowMinimize,

    /// <summary>A wrench, for configuration or repair.</summary>
    Wrench,

    /// <summary>The Aquarius sign.</summary>
    ZodiacAquarius,

    /// <summary>The Aries sign.</summary>
    ZodiacAries,

    /// <summary>The Cancer sign.</summary>
    ZodiacCancer,

    /// <summary>The Capricorn sign.</summary>
    ZodiacCapricorn,

    /// <summary>The Gemini sign.</summary>
    ZodiacGemini,

    /// <summary>The Leo sign.</summary>
    ZodiacLeo,

    /// <summary>The Libra sign.</summary>
    ZodiacLibra,

    /// <summary>The Pisces sign.</summary>
    ZodiacPisces,

    /// <summary>The Sagittarius sign.</summary>
    ZodiacSagittarius,

    /// <summary>The Scorpio sign.</summary>
    ZodiacScorpio,

    /// <summary>The Taurus sign.</summary>
    ZodiacTaurus,

    /// <summary>The Virgo sign.</summary>
    ZodiacVirgo,

    /// <summary>A magnifying glass with a plus, for zooming in.</summary>
    ZoomIn,

    /// <summary>A magnifying glass with a minus, for zooming out.</summary>
    ZoomOut,
}
