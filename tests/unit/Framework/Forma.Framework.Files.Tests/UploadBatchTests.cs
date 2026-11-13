namespace Forma.Framework.Files.Tests;

using Forma.Framework.Resources;

using Xunit;

public sealed class UploadBatchTests
{
    private static UploadBatch CreateSet(params (string Name, long Size)[] files)
    {
        var set = new UploadBatch();

        foreach (var (name, size) in files)
        {
            set.Add(name, size);
        }

        return set;
    }

    [Fact]
    public void Should_hold_its_files_pending_in_order()
    {
        // Act
        var set = CreateSet(("one.png", 100), ("two.png", 200));

        // Assert
        Assert.Multiple(
            () => Assert.Equal(2, set.Count),
            () => Assert.True(set.HasPending),
            () => Assert.Equal(0, set.StartedCount),
            () => Assert.Equal(["one.png", "two.png"], set.Files.Select(file => file.Name)),
            () => Assert.Equal(300, set.TotalBytes));
    }

    [Fact]
    public void Should_aggregate_what_each_file_has_transferred()
    {
        // Arrange
        var set = CreateSet(("one.png", 100), ("two.png", 200));

        // Act
        set.UpdateFileProgress(0, percent: 50, bytesTransferred: 50);
        set.UpdateFileProgress(1, percent: 25, bytesTransferred: 50);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(100, set.BytesTransferred),
            () => Assert.Equal(50, set[0].ProgressPercentage));
    }

    [Fact]
    public void Should_start_a_file_clock_on_entering_uploading_and_freeze_it_on_leaving()
    {
        // Arrange
        var set = CreateSet(("one.png", 100));

        // Act
        set.SetFileState(0, UploadState.Uploading, percent: 0);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(UploadState.Uploading, set[0].State),
            () => Assert.True(set[0].StartTimestamp > 0),
            () => Assert.Null(set[0].Duration),
            () => Assert.Equal(1, set.StartedCount));

        set.SetFileState(0, UploadState.Completed, percent: 100);
        Assert.Multiple(
            () => Assert.Equal(UploadState.Completed, set[0].State),
            () => Assert.NotNull(set[0].Duration),
            () => Assert.Equal(1, set.CompletedCount),
            () => Assert.False(set.HasPending));
    }

    [Fact]
    public void Should_hold_why_the_store_refused_a_file()
    {
        // Arrange
        var set = CreateSet(("one.png", 100), ("two.png", 100));
        var refusal = new FileRefusal(FileRefusalReason.TooLarge, 64);
        set.SetFileState(0, UploadState.Uploading, percent: 0);

        // Act
        set.RefuseFile(0, refusal);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(UploadState.Refused, set[0].State),
            () => Assert.Same(refusal, set[0].Refusal),
            () => Assert.NotNull(set[0].Duration),
            () => Assert.Equal(0, set.CompletedCount),
            () => Assert.Null(set[1].Refusal));
    }

    [Fact]
    public void Should_mark_only_the_still_pending_files_from_a_position()
    {
        // Arrange
        var set = CreateSet(("one.png", 100), ("two.png", 100), ("three.png", 100));
        set.SetFileState(0, UploadState.Completed);

        // Act
        set.TransitionPendingFiles(0, UploadState.Canceled);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(UploadState.Completed, set[0].State),
            () => Assert.Equal(UploadState.Canceled, set[1].State),
            () => Assert.Equal(UploadState.Canceled, set[2].State));
    }

    [Fact]
    public void Should_run_its_clock_between_begin_and_end()
    {
        // Arrange
        var set = CreateSet(("one.png", 100));
        Assert.Equal(TimeSpan.Zero, set.Elapsed);

        // Act
        set.BeginUpload();
        set.EndUpload();

        // Assert
        var frozen = set.Elapsed;
        Assert.Multiple(
            () => Assert.True(frozen >= TimeSpan.Zero),
            () => Assert.Equal(frozen, set.Elapsed));
    }

    [Fact]
    public void Should_hold_nothing_after_being_cleared()
    {
        // Arrange
        var set = CreateSet(("one.png", 100));
        set.BeginUpload();
        set.EndUpload();

        // Act
        set.Clear();

        // Assert
        Assert.Multiple(
            () => Assert.Equal(0, set.Count),
            () => Assert.Equal(TimeSpan.Zero, set.Elapsed),
            () => Assert.False(set.HasPending));
    }

    [Fact]
    public void Should_estimate_only_while_a_rate_and_a_remainder_exist()
    {
        // Arrange
        var untouched = new UploadFile("one.png", 100);

        // Act
        var moving = untouched with { StartTimestamp = 1, BytesTransferred = 50 };
        var done = untouched with { StartTimestamp = 1, BytesTransferred = 100 };

        // Assert
        Assert.Multiple(
            () => Assert.Null(untouched.EstimatedTimeRemaining),
            () => Assert.NotNull(moving.EstimatedTimeRemaining),
            () => Assert.Null(done.EstimatedTimeRemaining));
    }
}
