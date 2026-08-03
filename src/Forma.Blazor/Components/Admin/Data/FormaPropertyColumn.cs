using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.QuickGrid;
using Microsoft.AspNetCore.Components.Rendering;

namespace Forma.Blazor.Components.Admin.Data;

/// <summary>A grid column bound to a property, which swaps to an editor while its row is being edited.</summary>
public sealed class FormaPropertyColumn<TGridItem, TProp> : ColumnBase<TGridItem>
    where TGridItem : notnull
{
    private static readonly Type? _editorType = ResolveEditorType();

    private Expression<Func<TGridItem, TProp>>? _boundProperty;
    private string? _boundFormat;
    private GridSort<TGridItem>? _derivedSort;
    private Func<TGridItem, TProp> _getter = default!;
    private Func<TGridItem, string?> _display = default!;
    private Action<TGridItem, TProp>? _setter;
    private TGridItem? _cachedDraft;
    private Expression<Func<TProp>>? _cachedValueExpression;

    /// <summary>The property this column reads, and writes back to while editing.</summary>
    [Parameter, EditorRequired] public Expression<Func<TGridItem, TProp>> Property { get; set; } = default!;

    /// <summary>Format string applied to the displayed value. Requires a property type implementing <see cref="IFormattable"/>.</summary>
    [Parameter] public string? Format { get; set; }

    /// <summary>Whether this column offers an editor while its row is being edited. A property with no setter is never editable.</summary>
    [Parameter] public bool Editable { get; set; } = true;

    /// <summary>Editor rendered in place of the built-in one, receiving the row's draft copy.</summary>
    [Parameter] public RenderFragment<TGridItem>? EditTemplate { get; set; }

    /// <inheritdoc />
    [Parameter] public override GridSort<TGridItem>? SortBy { get; set; }

    [CascadingParameter] private RowEditSession<TGridItem>? EditSession { get; set; }

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
                _derivedSort = GridSort<TGridItem>.ByAscending(Property);
                SortBy = _derivedSort;
            }

            Title ??= ResolveTitle(Property);
        }

        // Rebuilt on a property change too: the display closes over the getter, so keeping it
        // would read through the previous property.
        if (_display is null || propertyChanged || _boundFormat != Format)
        {
            _boundFormat = Format;
            _display = BuildDisplay(_getter, Format);
        }
    }

    /// <inheritdoc />
    protected override void CellContent(RenderTreeBuilder builder, TGridItem item)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (EditSession is { Draft: { } draft, EditContext: { } editContext } session && session.IsEditing(item))
        {
            // Editors refuse a context swap, so they get one fixed to this edit and keyed to
            // it — the next edit builds a new pair rather than re-parameterising this one.
            builder.OpenComponent<CascadingValue<EditContext>>(0);
            builder.SetKey(editContext);
            builder.AddComponentParameter(1, "Value", editContext);
            builder.AddComponentParameter(2, "IsFixed", true);
            builder.AddComponentParameter(
                3, "ChildContent", (RenderFragment)(content => EditingContent(content, draft)));
            builder.CloseComponent();

            return;
        }

        builder.AddContent(4, _display(item));
    }

    /// <inheritdoc />
    protected override bool IsSortableByDefault()
        => SortBy is not null;

    private void EditingContent(RenderTreeBuilder builder, TGridItem draft)
    {
        // Checked ahead of the template so opting a column out holds however its editor
        // would have been produced.
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

        // A read-only column still reads from the draft, so a value derived from an edited
        // one keeps up while the row is open.
        builder.AddContent(1, _display(draft));
    }

    private static Type? ResolveEditorType()
    {
        if (typeof(TProp) == typeof(string))
        {
            return typeof(InputText);
        }

        if (typeof(TProp) == typeof(bool))
        {
            return typeof(InputCheckbox);
        }

        var underlying = Nullable.GetUnderlyingType(typeof(TProp)) ?? typeof(TProp);

        if (underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset)
            || underlying == typeof(DateOnly) || underlying == typeof(TimeOnly))
        {
            return typeof(InputDate<>).MakeGenericType(typeof(TProp));
        }

        if (underlying == typeof(int) || underlying == typeof(long) || underlying == typeof(short)
            || underlying == typeof(float) || underlying == typeof(double) || underlying == typeof(decimal))
        {
            return typeof(InputNumber<>).MakeGenericType(typeof(TProp));
        }

        // Everything else (enums, complex types) needs an EditTemplate.
        return null;
    }

    private static Action<TGridItem, TProp>? BuildSetter(Expression<Func<TGridItem, TProp>> property)
    {
        if (property.Body is not MemberExpression member)
        {
            return null;
        }

        if (member.Member is PropertyInfo { CanWrite: false } or FieldInfo { IsInitOnly: true })
        {
            return null;
        }

        var value = Expression.Parameter(typeof(TProp), "value");

        return Expression.Lambda<Action<TGridItem, TProp>>(
            Expression.Assign(member, value), property.Parameters[0], value).Compile();
    }

    private static Func<TGridItem, string?> BuildDisplay(Func<TGridItem, TProp> getter, string? format)
    {
        if (string.IsNullOrEmpty(format))
        {
            return item => getter(item)?.ToString();
        }

        if (!typeof(IFormattable).IsAssignableFrom(Nullable.GetUnderlyingType(typeof(TProp)) ?? typeof(TProp)))
        {
            throw new InvalidOperationException(
                $"{nameof(Format)} requires a property type implementing {nameof(IFormattable)}, but {typeof(TProp)} does not.");
        }

        return item => ((IFormattable?)getter(item))?.ToString(format, CultureInfo.CurrentCulture);
    }

    private static string? ResolveTitle(Expression<Func<TGridItem, TProp>> property)
    {
        if (property.Body is not MemberExpression member)
        {
            return null;
        }

        return member.Member.GetCustomAttribute<DisplayAttribute>()?.Name
            ?? member.Member.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
            ?? member.Member.Name;
    }

    private void RenderEditor(
        RenderTreeBuilder builder, TGridItem draft, Action<TGridItem, TProp> setter, Type editorType)
    {
        builder.OpenComponent(3, editorType);
        builder.AddComponentParameter(4, "Value", _getter(draft));
        builder.AddComponentParameter(
            5, "ValueChanged", EventCallback.Factory.Create<TProp>(this, value => setter(draft, value)));
        builder.AddComponentParameter(6, "ValueExpression", ValueExpressionFor(draft));
        builder.AddComponentParameter(7, "class", "forma-data-grid-editor");
        builder.CloseComponent();
    }

    // Rewrites `item => item.Member` into `() => draft.Member` so the field the EditContext
    // validates is the draft's, not the untouched row's.
    private Expression<Func<TProp>> ValueExpressionFor(TGridItem draft)
    {
        if (_cachedValueExpression is not null && ReferenceEquals(_cachedDraft, draft))
        {
            return _cachedValueExpression;
        }

        var body = new ParameterReplacer(Property.Parameters[0], Expression.Constant(draft, typeof(TGridItem)))
            .Visit(Property.Body);

        _cachedDraft = draft;
        _cachedValueExpression = Expression.Lambda<Func<TProp>>(body);

        return _cachedValueExpression;
    }

    private sealed class ParameterReplacer(ParameterExpression parameter, Expression replacement) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == parameter ? replacement : base.VisitParameter(node);
    }
}
