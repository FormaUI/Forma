namespace Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>A renderer reporting the given <see cref="RendererInfo"/>, which renders a fragment as its one root and records what its components threw.</summary>
/// <param name="services">The services the rendered components are injected from.</param>
/// <param name="rendererInfo">The renderer the components see themselves rendered by.</param>
public sealed class FakeRenderer(IServiceProvider services, RendererInfo rendererInfo) : Renderer(services, NullLoggerFactory.Instance)
{
    private readonly FragmentRoot _root = new();
    private int? _rootId;

    /// <summary>What the rendered components threw, in order.</summary>
    public List<Exception> Exceptions { get; } = [];

    /// <inheritdoc />
    public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

    /// <inheritdoc />
    protected override RendererInfo RendererInfo
        => rendererInfo;

    /// <summary>Renders <paramref name="fragment"/> as the root, in place of the fragment rendered before it.</summary>
    public Task RenderAsync(RenderFragment fragment)
        => Dispatcher.InvokeAsync(() =>
        {
            _rootId ??= AssignRootComponentId(_root);

            return RenderRootComponentAsync(_rootId.Value, ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(FragmentRoot.Fragment)] = fragment }));
        });

    /// <inheritdoc />
    protected override void HandleException(Exception exception)
        => Exceptions.Add(exception);

    /// <inheritdoc />
    protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
        => Task.CompletedTask;

    private sealed class FragmentRoot : IComponent
    {
        private RenderHandle _renderHandle;

        public RenderFragment? Fragment { get; private set; }

        public void Attach(RenderHandle renderHandle)
            => _renderHandle = renderHandle;

        public Task SetParametersAsync(ParameterView parameters)
        {
            Fragment = parameters.GetValueOrDefault<RenderFragment>(nameof(Fragment));
            _renderHandle.Render(builder => builder.AddContent(0, Fragment));

            return Task.CompletedTask;
        }
    }
}
