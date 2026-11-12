namespace Forma.Tests.Blazor;

using AngleSharp.Dom;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>A bUnit invocation handler that holds each call to <c>identifier</c>, which answers with an object reference as a script's attach does, until the test answers it.</summary>
public sealed class HeldObjectReferenceHandler(string identifier)
    : JSRuntimeInvocationHandlerBase<IJSObjectReference>(invocation => invocation.Identifier == identifier, isCatchAllHandler: false)
{
    // Plain, so the caller's continuation runs inline where the answer completes it.
    private readonly List<(JSRuntimeInvocation Call, TaskCompletionSource<IJSObjectReference> Answer)> _heldCalls = [];

    /// <summary>The calls held so far, in order.</summary>
    public IReadOnlyList<JSRuntimeInvocation> HeldCalls
        => [.. _heldCalls.Select(held => held.Call)];

    /// <summary>The call held for <paramref name="target"/>, the element the call names first.</summary>
    public JSRuntimeInvocation GetHeldCall(IElement target)
    {
        ArgumentNullException.ThrowIfNull(target);

        var id = target.GetAttribute("blazor:elementReference");

        return _heldCalls.Single(held => held.Call.Arguments[0] is ElementReference element && element.Id == id).Call;
    }

    /// <summary>Answers <paramref name="call"/>, one of the calls held, with <paramref name="reference"/>.</summary>
    public void Answer(JSRuntimeInvocation call, IJSObjectReference reference)
        => _heldCalls.Single(held => held.Call.Equals(call)).Answer.SetResult(reference);

    /// <inheritdoc />
    protected override Task<IJSObjectReference> HandleAsync(JSRuntimeInvocation invocation)
    {
        var answer = new TaskCompletionSource<IJSObjectReference>();
        _heldCalls.Add((invocation, answer));

        return answer.Task;
    }
}
