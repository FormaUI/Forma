namespace Forma.Framework.Feedback.Tests;

using Forma.Framework.Feedback;

using Xunit;

public sealed class ToastServiceTests
{
    public sealed class ShowAsync
    {
        [Fact]
        public async Task Should_add_toast_with_defaults()
        {
            var sut = new ToastService();

            await sut.ShowAsync("Saved!", cancellationToken: TestContext.Current.CancellationToken);

            var toast = Assert.Single(sut.Toasts);
            Assert.Equal("Saved!", toast.Message);
            Assert.Equal(ToastType.Success, toast.Type);
            Assert.Equal(TimeSpan.FromSeconds(3), toast.Duration);
        }

        [Fact]
        public async Task Should_add_toast_with_custom_type_and_duration()
        {
            var sut = new ToastService();

            await sut.ShowAsync("Error!", ToastType.Error, TimeSpan.FromSeconds(5), cancellationToken: TestContext.Current.CancellationToken);

            var toast = Assert.Single(sut.Toasts);
            Assert.Equal(ToastType.Error, toast.Type);
            Assert.Equal(TimeSpan.FromSeconds(5), toast.Duration);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Should_reject_invalid_message(string? message)
        {
            var sut = new ToastService();

            await Assert.ThrowsAnyAsync<ArgumentException>(() => sut.ShowAsync(message!, cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Should_reject_negative_duration()
        {
            var sut = new ToastService();

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => sut.ShowAsync("Msg", duration: TimeSpan.FromMilliseconds(-1), cancellationToken: TestContext.Current.CancellationToken));
        }
    }

    public sealed class RemoveAsync
    {
        [Fact]
        public async Task Should_remove_toast_by_id()
        {
            var sut = new ToastService();
            await sut.ShowAsync("Toast 1", cancellationToken: TestContext.Current.CancellationToken);
            var id = sut.Toasts[0].Id;

            await sut.RemoveAsync(id, TestContext.Current.CancellationToken);

            Assert.Empty(sut.Toasts);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Should_reject_invalid_id(string? id)
        {
            var sut = new ToastService();

            await Assert.ThrowsAnyAsync<ArgumentException>(() => sut.RemoveAsync(id!, TestContext.Current.CancellationToken));
        }
    }
}
