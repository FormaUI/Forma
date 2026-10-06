namespace Forma.Blazor.Components.DataView;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

using Forma.Blazor.Components.Inputs;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

/// <summary>A column bound to a member, titled and sorted by it, which swaps to an editor while its row is being edited.</summary>
/// <typeparam name="TItem">The item a row shows.</typeparam>
/// <typeparam name="TValue">The member's value.</typeparam>
public sealed class FormaPropertyColumn<TItem, TValue> : FormaColumnBase<TItem>
    where TItem : notnull
{
    private static readonly EditorKind _editorKind = ResolveEditorKind();

    private Expression<Func<TItem, TValue>>? _boundProperty;
    private string? _boundFormat;
    private DataViewSort<TItem>? _derivedSort;
    private Func<TItem, TValue> _getter = default!;
    private Func<TItem, string?> _displayTextSelector = default!;
    private Action<TItem, TValue>? _setter;
    private TItem? _cachedDraft;
    private Expression<Func<TValue>>? _cachedValueExpression;
    private TItem? _dateFieldDraft;
    private Expression<Func<DateOnly?>>? _dateFieldExpression;

    /// <summary>The member this column reads, and writes back to while editing.</summary>
    [Parameter, EditorRequired] public Expression<Func<TItem, TValue>> Property { get; set; } = default!;

    /// <summary>The format string applied to the displayed value.</summary>
    [Parameter] public string? Format { get; set; }

    /// <summary>Whether this column offers an editor while its row is being edited.</summary>
    [Parameter] public bool Editable { get; set; } = true;

    /// <summary>The editor rendered in place of the built-in one, given the row's draft copy.</summary>
    [Parameter] public RenderFragment<TItem>? EditTemplate { get; set; }

    /// <inheritdoc />
    [Parameter] public override DataViewSort<TItem>? SortBy { get; set; }

    [CascadingParameter] private RowEditSession<TItem>? EditSession { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Property);

        var propertyChanged = _boundProperty is null || !IsSameProperty(_boundProperty, Property);

        if (propertyChanged)
        {
            _boundProperty = Property;
            _getter = Property.Compile();
            _setter = BuildSetter(Property);

            Title ??= ResolveTitle(Property);
            SortName ??= ResolveSortName(Property);

            // Only a sort the column derived itself is replaced; a caller's sort is kept, and a property with no path derives none.
            if (SortBy is null || ReferenceEquals(SortBy, _derivedSort))
            {
                _derivedSort = SortName is null ? null : DataViewSort<TItem>.ByAscending(Property);
                SortBy = _derivedSort;
            }
        }

        // Rebuilt on a property change too: the display closes over the getter, so keeping it would read through the previous property.
        if (_displayTextSelector is null || propertyChanged || _boundFormat != Format)
        {
            _boundFormat = Format;
            _displayTextSelector = BuildDisplay(_getter, Format);
        }

        base.OnParametersSet();
    }

    /// <inheritdoc />
    protected internal override void CellContent(RenderTreeBuilder builder, TItem item)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (EditSession is { Draft: { } draft, EditContext: { } editContext } session && session.IsEditing(item))
        {
            // Editors refuse a context swap, so they get one fixed to this edit and keyed to it.
            builder.OpenComponent<CascadingValue<EditContext>>(0);
            builder.SetKey(editContext);
            builder.AddComponentParameter(1, "Value", editContext);
            builder.AddComponentParameter(2, "IsFixed", true);
            builder.AddComponentParameter(
                3, "ChildContent", (RenderFragment)(content => EditingContent(content, draft)));
            builder.CloseComponent();

            return;
        }

        builder.AddContent(4, _displayTextSelector(item));
    }

    /// <inheritdoc />
    protected override bool IsSortableByDefault()
        => SortBy is not null;

    private void EditingContent(RenderTreeBuilder builder, TItem draft)
    {
        // Checked ahead of the template, so a column opted out stays read-only with a template too.
        if (Editable)
        {
            if (EditTemplate is not null)
            {
                builder.AddContent(0, EditTemplate(draft));
                return;
            }

            if (_setter is { } setter && _editorKind != EditorKind.None)
            {
                RenderEditor(builder, draft, setter);
                return;
            }
        }

        // A read-only column still reads from the draft, so a value derived from an edited one keeps up while the row is open.
        builder.AddContent(1, _displayTextSelector(draft));
    }

    // The library's input for the value's type where it has one, else the framework's in the library's look.
    private static EditorKind ResolveEditorKind()
    {
        var type = typeof(TValue);

        if (type == typeof(string))
        {
            return EditorKind.TextBox;
        }

        if (type == typeof(bool))
        {
            return EditorKind.CheckBox;
        }

        if (type == typeof(DateOnly?))
        {
            return EditorKind.NullableDatePicker;
        }

        if (type == typeof(DateOnly))
        {
            return EditorKind.DatePicker;
        }

        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset) || underlying == typeof(TimeOnly))
        {
            return EditorKind.DateInput;
        }

        if (underlying == typeof(int) || underlying == typeof(long) || underlying == typeof(short)
            || underlying == typeof(float) || underlying == typeof(double) || underlying == typeof(decimal))
        {
            return EditorKind.NumberInput;
        }

        // Everything else, an enum or a complex type, needs an edit template.
        return EditorKind.None;
    }

    private static Action<TItem, TValue>? BuildSetter(Expression<Func<TItem, TValue>> property)
    {
        if (property.Body is not MemberExpression member)
        {
            return null;
        }

        if (member.Member is PropertyInfo { CanWrite: false } or FieldInfo { IsInitOnly: true })
        {
            return null;
        }

        var value = Expression.Parameter(typeof(TValue), "value");

        return Expression.Lambda<Action<TItem, TValue>>(
            Expression.Assign(member, value), property.Parameters[0], value).Compile();
    }

    private static Func<TItem, string?> BuildDisplay(Func<TItem, TValue> getter, string? format)
    {
        if (string.IsNullOrEmpty(format))
        {
            return item => getter(item)?.ToString();
        }

        if (!typeof(IFormattable).IsAssignableFrom(Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue)))
        {
            throw new InvalidOperationException(
                $"{nameof(Format)} requires a value type implementing {nameof(IFormattable)}, but {typeof(TValue)} does not.");
        }

        return item => ((IFormattable?)getter(item))?.ToString(format, CultureInfo.CurrentCulture);
    }

    /// <inheritdoc />
    protected override object? ProjectParameter(ParameterValue parameter)
        => parameter.Name == nameof(Property) && parameter.Value is Expression<Func<TItem, TValue>> property ? (object?)ResolveSortName(property) ?? property : parameter.Value;

    // The same member read again: a path of members is the same by its names, where any other expression is the same only as the one given before,
    // since the host builds a new expression each time it renders.
    private static bool IsSameProperty(Expression<Func<TItem, TValue>> bound, Expression<Func<TItem, TValue>> given)
        => ReferenceEquals(bound, given) || (ResolveSortName(bound) is { } path && path == ResolveSortName(given));

    // The property's path, such as Customer.Name, and null for a property that is not a path of members.
    private static string? ResolveSortName(Expression<Func<TItem, TValue>> property)
    {
        var names = new List<string>();
        var body = property.Body;

        while (body is MemberExpression member)
        {
            names.Add(member.Member.Name);
            body = member.Expression;
        }

        if (body is not ParameterExpression || names.Count == 0)
        {
            return null;
        }

        names.Reverse();

        return string.Join('.', names);
    }

    private static string? ResolveTitle(Expression<Func<TItem, TValue>> property)
    {
        if (property.Body is not MemberExpression member)
        {
            return null;
        }

        return member.Member.GetCustomAttribute<DisplayAttribute>()?.Name
            ?? member.Member.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
            ?? member.Member.Name;
    }

    private void RenderEditor(RenderTreeBuilder builder, TItem draft, Action<TItem, TValue> setter)
    {
        var value = _getter(draft);
        var valueChanged = EventCallback.Factory.Create<TValue>(this, changed => setter(draft, changed));

        switch (_editorKind)
        {
            case EditorKind.TextBox:
                RenderInput(builder, typeof(FormaTextBox), value, valueChanged, GetValueExpression(draft));
                break;
            case EditorKind.CheckBox:
                RenderInput(builder, typeof(FormaCheckBox), value, valueChanged, GetValueExpression(draft));
                break;
            case EditorKind.NullableDatePicker:
                RenderDatePicker(builder, value, valueChanged, GetValueExpression(draft));
                break;
            case EditorKind.DatePicker:
                // The picker takes a nullable date, so a date member is bound through a conversion each way and identified through a stand-in.
                RenderDatePicker(
                    builder,
                    (DateOnly?)(object)value!,
                    EventCallback.Factory.Create<DateOnly?>(this, changed => setter(draft, (TValue)(object)(changed ?? default))),
                    GetDateFieldExpression(draft));
                break;
            case EditorKind.NumberInput:
                RenderFrameworkInput(builder, typeof(InputNumber<>).MakeGenericType(typeof(TValue)), value, valueChanged, GetValueExpression(draft));
                break;
            case EditorKind.DateInput:
                RenderFrameworkInput(builder, typeof(InputDate<>).MakeGenericType(typeof(TValue)), value, valueChanged, GetValueExpression(draft));
                break;
            default:
                throw new InvalidOperationException($"'{_editorKind}' is not a known {nameof(EditorKind)}.");
        }
    }

    // A library input takes the editor's class as its own and names itself by the column, since a cell carries no label.
    private void RenderInput(RenderTreeBuilder builder, Type inputType, object? value, object valueChanged, object valueExpression)
    {
        builder.OpenComponent(3, inputType);
        builder.AddComponentParameter(4, "Value", value);
        builder.AddComponentParameter(5, "ValueChanged", valueChanged);
        builder.AddComponentParameter(6, "ValueExpression", valueExpression);
        builder.AddComponentParameter(7, "Class", EditorClass);
        builder.AddComponentParameter(8, "aria-label", Title);
        builder.CloseComponent();
    }

    // The picker is a field and asks for its label, the column's title, which the cell hides while it stays the picker's name.
    private void RenderDatePicker(RenderTreeBuilder builder, object? value, object valueChanged, object valueExpression)
    {
        builder.OpenComponent<FormaDatePicker>(15);
        builder.AddComponentParameter(16, nameof(FormaDatePicker.Value), value);
        builder.AddComponentParameter(17, nameof(FormaDatePicker.ValueChanged), valueChanged);
        builder.AddComponentParameter(18, nameof(FormaDatePicker.ValueExpression), valueExpression);
        builder.AddComponentParameter(19, nameof(FormaDatePicker.Label), Title);
        builder.AddComponentParameter(20, nameof(FormaDatePicker.Class), EditorClass);
        builder.CloseComponent();
    }

    // A framework input wears the text box's look.
    private void RenderFrameworkInput(RenderTreeBuilder builder, Type inputType, object? value, object valueChanged, object valueExpression)
    {
        builder.OpenComponent(9, inputType);
        builder.AddComponentParameter(10, "Value", value);
        builder.AddComponentParameter(11, "ValueChanged", valueChanged);
        builder.AddComponentParameter(12, "ValueExpression", valueExpression);
        builder.AddComponentParameter(13, "class", $"forma-text-box {EditorClass}");
        builder.AddComponentParameter(14, "aria-label", Title);
        builder.CloseComponent();
    }

    private const string EditorClass = "forma-data-grid-editor";

    // The picker identifies its field through a nullable date member, which a date member is not; the stand-in lends it one, per draft.
    private Expression<Func<DateOnly?>> GetDateFieldExpression(TItem draft)
    {
        if (_dateFieldExpression is not null && ReferenceEquals(_dateFieldDraft, draft))
        {
            return _dateFieldExpression;
        }

        var standIn = new DateFieldStandIn();
        _dateFieldDraft = draft;
        _dateFieldExpression = () => standIn.Value;

        return _dateFieldExpression;
    }

    // Validation targets the draft, so the member expression is rewritten from the row to the copy being edited; cached per draft, since the expression identifies the field.
    private Expression<Func<TValue>> GetValueExpression(TItem draft)
    {
        if (_cachedValueExpression is not null && ReferenceEquals(_cachedDraft, draft))
        {
            return _cachedValueExpression;
        }

        var body = new ParameterReplacer(Property.Parameters[0], Expression.Constant(draft, typeof(TItem))).Visit(Property.Body);
        _cachedDraft = draft;
        _cachedValueExpression = Expression.Lambda<Func<TValue>>(body);

        return _cachedValueExpression;
    }

    private sealed class ParameterReplacer(ParameterExpression parameter, Expression replacement) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == parameter ? replacement : base.VisitParameter(node);
    }

    /// <summary>The input a cell draws while its row is edited, decided by the member's type.</summary>
    private enum EditorKind
    {
        None,
        TextBox,
        CheckBox,
        DatePicker,
        NullableDatePicker,
        NumberInput,
        DateInput,
    }

    // The member the date picker identifies its field by, in the picker's own type, standing in for a date member.
    private sealed class DateFieldStandIn
    {
        public DateOnly? Value { get; set; }
    }
}
