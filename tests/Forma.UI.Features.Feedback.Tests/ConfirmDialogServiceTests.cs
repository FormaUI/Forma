namespace Forma.UI.Features.Feedback.Tests;

using Forma.UI.Features.Feedback;

using Xunit;

public sealed class ConfirmDialogServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_reject_invalid_message(string? message)
    {
        var sut = new ConfirmDialogService();

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => sut.ConfirmAsync(message!, "OK", "Cancel", TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_reject_invalid_confirm_text(string? confirmText)
    {
        var sut = new ConfirmDialogService();

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => sut.ConfirmAsync("Delete?", confirmText!, "Cancel", TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_reject_invalid_cancel_text(string? cancelText)
    {
        var sut = new ConfirmDialogService();

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => sut.ConfirmAsync("Delete?", "OK", cancelText!, TestContext.Current.CancellationToken));
    }
}
