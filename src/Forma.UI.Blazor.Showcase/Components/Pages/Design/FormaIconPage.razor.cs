namespace Forma.UI.Blazor.Showcase.Components.Pages.Design;

using Forma.UI.Design.Icons;

public partial class FormaIconPage
{
    private static readonly IReadOnlyList<Brand> Brands =
        [.. Enum.GetValues<Brand>().Where(brand => brand is not Brand.None)];

    private static readonly IReadOnlyList<GlyphSection> Named =
    [
        new("Arrows and navigation",
        [
            Glyph.ArrowUp, Glyph.ArrowDown, Glyph.ArrowLeft, Glyph.ArrowRight,
            Glyph.ChevronUp, Glyph.ChevronDown, Glyph.ChevronLeft, Glyph.ChevronRight,
            Glyph.TriangleUp, Glyph.TriangleDown, Glyph.TriangleLeft, Glyph.TriangleRight,
            Glyph.Expand, Glyph.Collapse, Glyph.ExpandPlus, Glyph.CollapseMinus, Glyph.Maximize, Glyph.Minimize,
            Glyph.Swap, Glyph.Sync, Glyph.Refresh, Glyph.Rotate, Glyph.Undo, Glyph.Redo, Glyph.Forward, Glyph.Reply, Glyph.Rewind, Glyph.FastForward,
            Glyph.Download, Glyph.Upload, Glyph.Exit, Glyph.SignIn, Glyph.LocationArrow, Glyph.LocationArrowCircle, Glyph.Cursor, Glyph.Move, Glyph.DragHandle, Glyph.ArrowTurnDownRight,
            Glyph.Sort, Glyph.SortAlphaAscending, Glyph.SortAlphaDescending, Glyph.SortNumericAscending, Glyph.SortNumericDescending, Glyph.ZoomIn, Glyph.ZoomOut, Glyph.NavigationMenu, Glyph.Home, Glyph.More, Glyph.MoreVertical,
        ]),
        new("Actions",
        [
            Glyph.Check, Glyph.CheckSquare, Glyph.Cross, Glyph.Plus, Glyph.Minus,
            Glyph.Select, Glyph.SelectAll, Glyph.SelectNone, Glyph.RadioButton,
            Glyph.Save, Glyph.Delete, Glyph.Copy, Glyph.Cut, Glyph.Paste, Glyph.Edit, Glyph.Draft, Glyph.Pen, Glyph.Print,
            Glyph.Share, Glyph.ShareBox, Glyph.Send, Glyph.Publish, Glyph.Open, Glyph.Read, Glyph.Unread, Glyph.Glasses,
            Glyph.Search, Glyph.Filter, Glyph.Crosshair, Glyph.Pin, Glyph.Unpin, Glyph.Flag, Glyph.Bookmark, Glyph.Star, Glyph.Heart,
            Glyph.Eye, Glyph.EyeSlash, Glyph.Lock, Glyph.LockOpen, Glyph.Key, Glyph.PasswordField,
            Glyph.Settings, Glyph.Wrench, Glyph.Toolbox, Glyph.Maintenance, Glyph.Extract, Glyph.Compress, Glyph.Crop,
            Glyph.MagicWand, Glyph.Lasso, Glyph.Transform, Glyph.Warp, Glyph.Paintbrush, Glyph.PaintBucket, Glyph.PaintBucketPour, Glyph.Eraser, Glyph.Stamp, Glyph.Smudge, Glyph.PenNib, Glyph.Eyedropper, Glyph.ColorPicker, Glyph.Palette, Glyph.Emoji,
            Glyph.Rules, Glyph.Security, Glyph.Incognito, Glyph.Properties, Glyph.Extensions, Glyph.Apps, Glyph.Dashboard,
            Glyph.Plan, Glyph.Poll, Glyph.Ranking, Glyph.Count, Glyph.CountBox, Glyph.CountDividedBox, Glyph.History, Glyph.Snooze, Glyph.Hourglass, Glyph.HourglassProgress, Glyph.Clock, Glyph.AlarmClock, Glyph.Stopwatch, Glyph.StopwatchDigital,
            Glyph.Auto, Glyph.Power, Glyph.Prohibited, Glyph.Empty,
        ]),
        new("Status and feedback",
        [
            Glyph.CircleCheck, Glyph.CircleCross, Glyph.CircleError, Glyph.CircleHelp, Glyph.CircleInfo,
            Glyph.CirclePlay, Glyph.CircleRecord, Glyph.CircleRun,
            Glyph.Warning, Glyph.Bug, Glyph.Debug, Glyph.Bell, Glyph.BellSlash, Glyph.Lightbulb, Glyph.Session, Glyph.Events,
        ]),
        new("Text formatting",
        [
            Glyph.Bold, Glyph.Italic, Glyph.Underline, Glyph.Strikethrough, Glyph.Subscript, Glyph.Superscript,
            Glyph.Font, Glyph.FontSize, Glyph.FontSizeIncrease, Glyph.FontSizeDecrease, Glyph.FontColor, Glyph.TextGlow,
            Glyph.ChangeCase, Glyph.ClearFormatting, Glyph.Heading, Glyph.Text, Glyph.Paragraph,
            Glyph.ParagraphDirectionLtr, Glyph.ParagraphDirectionRtl, Glyph.TextDirectionLtr, Glyph.TextDirectionRtl,
            Glyph.AlignLeft, Glyph.AlignCenter, Glyph.AlignRight, Glyph.AlignJustify, Glyph.Indent, Glyph.Outdent,
            Glyph.LineHeight, Glyph.LineSpacing, Glyph.List, Glyph.ListBullet, Glyph.ListNumbered, Glyph.ListMultilevel, Glyph.TaskList,
            Glyph.Quote, Glyph.Code, Glyph.Link, Glyph.RemoveLink, Glyph.HorizontalLink, Glyph.RemoveHorizontalLink,
            Glyph.Anchor, Glyph.RemoveAnchor, Glyph.Borders, Glyph.Shading, Glyph.Highlight,
            Glyph.SpellCheck, Glyph.FindAndReplace, Glyph.Translation, Glyph.Ruler,
        ]),
        new("Tables and layout",
        [
            Glyph.Table, Glyph.Rows, Glyph.Columns, Glyph.AddColumnLeft, Glyph.AddColumnRight, Glyph.AddRowAbove, Glyph.AddRowBelow, Glyph.Split,
            Glyph.Layout, Glyph.LayoutTop, Glyph.LayoutBottom, Glyph.LayoutLeft, Glyph.LayoutRight,
            Glyph.GridView, Glyph.DetailsView, Glyph.WindowMaximize, Glyph.WindowMinimize,
        ]),
        new("Files and media",
        [
            Glyph.File, Glyph.FileText, Glyph.FileCode, Glyph.FileArchive,
            Glyph.Folder, Glyph.FolderOpen, Glyph.FolderAdd, Glyph.FolderTree, Glyph.Archive, Glyph.Package, Glyph.Downloads, Glyph.Attachment,
            Glyph.Image, Glyph.Images, Glyph.Camera, Glyph.Video, Glyph.Audio, Glyph.Microphone,
            Glyph.Speaker, Glyph.SoundOutput, Glyph.Mute, Glyph.Unmute,
        ]),
        new("Communication",
        [
            Glyph.MailEmpty, Glyph.MailInbox, Glyph.MailOutbox, Glyph.MailDraft, Glyph.MailJunk,
            Glyph.Message, Glyph.Comment, Glyph.Phone, Glyph.PhoneSlash, Glyph.AtSign, Glyph.HashtagSign,
            Glyph.Broadcast, Glyph.Signal, Glyph.Wifi, Glyph.Bluetooth, Glyph.Cast,
            Glyph.Contacts, Glyph.User, Glyph.Users, Glyph.Group, Glyph.Category,
        ]),
        new("Devices and data",
        [
            Glyph.Desktop, Glyph.Laptop, Glyph.Tablet, Glyph.Mobile, Glyph.Devices, Glyph.Keyboard, Glyph.KeyboardLeft, Glyph.KeyboardRight, Glyph.Mouse,
            Glyph.KeyWin, Glyph.KeyCmd, Glyph.KeyAlt, Glyph.KeyCtrl, Glyph.KeyShift, Glyph.KeyCapsLock, Glyph.KeyBackspace,
            Glyph.Chip, Glyph.Database, Glyph.Data, Glyph.Network, Glyph.NeuralNetwork, Glyph.GenerativeAI,
            Glyph.CodeCommit, Glyph.CodeBranch, Glyph.CodeMerge, Glyph.CodeFork, Glyph.CodeCompare, Glyph.CodePullRequest, Glyph.Sitemap, Glyph.Barcode,
            Glyph.Cloud, Glyph.CloudUpload, Glyph.Terminal, Glyph.Battery, Glyph.BatteryFull, Glyph.Brightness, Glyph.Flashlight, Glyph.Calculator,
        ]),
        new("People and hands",
        [
            Glyph.Accessibility, Glyph.Graduation,
            Glyph.HandRaisedLeft, Glyph.HandRaisedRight, Glyph.HandWave, Glyph.HandPeace, Glyph.HandCall, Glyph.HandClap, Glyph.HandPen,
            Glyph.HandPointUp, Glyph.HandPointDown, Glyph.HandPointLeft, Glyph.HandPointRight,
            Glyph.HandThumbUp, Glyph.HandThumbDown, Glyph.Handshake,
        ]),
        new("Commerce and travel",
        [
            Glyph.Cart, Glyph.CartPlus, Glyph.CartCheck, Glyph.CartCross, Glyph.CreditCard, Glyph.Wallet, Glyph.Coin, Glyph.Coins, Glyph.Cent,
            Glyph.DollarSign, Glyph.EuroSign, Glyph.PoundSign, Glyph.ShekelSign, Glyph.Truck,
            Glyph.AirplaneUp, Glyph.AirplaneDown, Glyph.AirplaneLeft, Glyph.AirplaneRight, Glyph.Earth, Glyph.Globe,
            Glyph.Medal, Glyph.Crown, Glyph.ForkKnife, Glyph.SoccerBall, Glyph.PawPrint,
        ]),
        new("Mathematics",
        [
            Glyph.MathMultiply, Glyph.MathDivide, Glyph.MathEquals, Glyph.MathNotEqual, Glyph.MathPercent, Glyph.MathPlusMinus,
            Glyph.MathSquareRoot, Glyph.MathInfinity, Glyph.MathLessThan, Glyph.MathGreaterThan,
            Glyph.MathLessThanOrEqual, Glyph.MathGreaterThanOrEqual, Glyph.MathSigma, Glyph.MathPi,
        ]),
        new("Zodiac",
        [
            Glyph.ZodiacAries, Glyph.ZodiacTaurus, Glyph.ZodiacGemini, Glyph.ZodiacCancer, Glyph.ZodiacLeo, Glyph.ZodiacVirgo,
            Glyph.ZodiacLibra, Glyph.ZodiacScorpio, Glyph.ZodiacSagittarius, Glyph.ZodiacCapricorn, Glyph.ZodiacAquarius, Glyph.ZodiacPisces,
        ]),
    ];

    // Every glyph the sections above leave out lands in a last section, so nothing is hidden by omission.
    private static readonly IReadOnlyList<GlyphSection> Sections = Complete(Named);

    private static IReadOnlyList<GlyphSection> Complete(IReadOnlyList<GlyphSection> named)
    {
        var placed = named.SelectMany(section => section.Glyphs).ToHashSet();
        var other = Enum.GetValues<Glyph>().Where(glyph => glyph is not Glyph.None && !placed.Contains(glyph)).ToArray();

        return other.Length is 0 ? named : [.. named, new GlyphSection("Other", other)];
    }

    private sealed record GlyphSection(string Title, IReadOnlyList<Glyph> Glyphs);
}
