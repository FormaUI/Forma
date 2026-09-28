namespace Forma.Blazor.Components.DataView;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

/// <summary>A column bound to a member, titled and sorted by it, which swaps to an editor while its row is being edited.</summary>
/// <typeparam name="TItem">The item a row shows.</typeparam>
/// <typeparam name="TValue">The member's value.</typeparam>
public sealed class FormaPropertyColumn<TItem, TValue> : FormaColumnBase<TItem>
    where TItem : notnull
{
    private static readonly Type? _editorType = ResolveEditorType();

    private Expression<Func<TItem, TValue>>? _boundProperty;
    private string? _boundFormat;
    private DataViewSort<TItem>? _derivedSort;
    private Func<TItem, TValue> _getter = default!;
    private Func<TItem, string?> _displayTextSelector = default!;
    private Action<TItem, TValue>? _setter;
    private TItem? _cachedDraft;
    private Expression<Func<TValue>>? _cachedValueExpression;

    /// <summary>The member this column reads, and writes back to while editing.</summary>
    [Parameter, EditorRequired] public Expression<Func<TItem, TValue>> Property { get; set; } = default!;

    /// <summary>Format string applied to the displayed value. Requires a value type implementing <see cref="IFormattable"/>.</summary>
    [Parameter] public string? Format { get; set; }

    /// <summary>Whether this column offers an editor while its row is being edited. A member with no setter is never editable.</summary>
    [Parameter] public bool Editable { get; set; } = true;

    /// <summary>Editor rendered in place of the built-in one, receiving the row's draft copy.</summary>
    [Parameter] public RenderFragment<TItem>? EditTemplate { get; set; }

    /// <inheritdoc />
    [Parameter] public override DataViewSort<TItem>? SortBy { get; set; }

    [CascadingParameter] private RowEditSession<TItem>? EditSession { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Property);

        var propertyChanged = !ReferenceEquals(_boundProperty, Property);

        if (propertyChanged)
        {
            _boundProperty = Property;
            _getter = Property.Compile();
            _setter = BuildSetter(Property);

            // Only replace a sort this column derived itself, so a caller-supplied one survives.
            if (SortBy is null || ReferenceEquals(SortBy, _derivedSort))
            {
                _derivedSort = DataViewSort<TItem>.ByAscending(Property);
                SortBy = _derivedSort;
            }

            Title ??= ResolveTitle(Property);
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
            // Editors refuse a context swap, so they get one fixed to this edit and keyed to it; the next edit builds a new pair rather than re-parameterizing this one.
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
        // Checked ahead of the template so opting a column out holds however its editor would have been produced.
        if (Editable)
        {
            if (EditTemplate is not null)
            {
                builder.AddContent(0, EditTemplate(draft));
                return;
            }

            if (_setter is { } setter && _editorType is { } editorType)
            {
                RenderEditor(builder, draft, setter, editorType);
                return;
            }
        }

        // A read-only column still reads from the draft, so a value derived from an edited one keeps up while the row is open.
        builder.AddContent(1, _displayTextSelector(draft));
    }

    private static Type? ResolveEditorType()
    {
        if (typeof(TValue) == typeof(string))
        {
            return typeof(InputText);
        }

        if (typeof(TValue) == typeof(bool))
        {
            return typeof(InputCheckbox);
        }

        var underlying = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);

        if (underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset)
            || underlying == typeof(DateOnly) || underlying == typeof(TimeOnly))
        {
            return typeof(InputDate<>).MakeGenericType(typeof(TValue));
        }

        if (underlying == typeof(int) || underlying == typeof(long) || underlying == typeof(short)
            || underlying == typeof(float) || underlying == typeof(double) || underlying == typeof(decimal))
        {
            return typeof(InputNumber<>).MakeGenericType(typeof(TValue));
        }

        // Everything else, an enum or a complex type, needs an edit template.
        return null;
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

    private void RenderEditor(RenderTreeBuilder builder, TItem draft, Action<TItem, TValue> setter, Type editorType)
    {
        builder.OpenComponent(3, editorType);
        builder.AddComponentParameter(4, "Value", _getter(draft));
        builder.AddComponentParameter(5, "ValueChanged", EventCallback.Factory.Create<TValue>(this, value => setter(draft, value)));
        builder.AddComponentParameter(6, "ValueExpression", GetValueExpression(draft));
        builder.AddComponentParameter(7, "class", "forma-data-grid-editor");
        builder.CloseComponent();
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
}
