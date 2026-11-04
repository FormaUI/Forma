namespace Forma.Framework.Browser.Interop.Tests.Text;

using System.Text;

using Forma.Framework.Browser.Interop.Text;
using Forma.Framework.JavaScript.Interop;
using Forma.Tests.Browser;
using Forma.Tests.Interop;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using Xunit;

public sealed class DomTextEditsTests
{
    private static readonly ElementReference Field = new("field");
    private static readonly ElementReference Redrawn = new("redrawn");
    private static readonly TextEditOptions Live = new() { SendsOnInput = true };

    // The browser attaches every interaction, answers each write with the revision after the last it numbered, and resends where asked.
    private static FakeDomBrowser CreateBrowser(Func<string, object?[], object?>? invoke = null)
        => new((path, identifier, args) => (path, identifier) switch
        {
            (FakeDomBrowser.EnginePath, "attach") => true,
            (FakeDomBrowser.EnginePath, "invoke") => invoke?.Invoke((string)args[3]!, args),
            _ => null,
        });

    // The edits of the field, attached to it.
    private static async Task<DomTextEdits> FollowAsync(DomEngine engine, string? text, TextEditOptions options, Func<TextChange, Task> handler)
    {
        var edits = new DomTextEdits(engine, text, handler);
        await edits.SyncAsync(Field, text, options);

        return edits;
    }

    private static (DomReceiver Receiver, string Token) GetReceiver(FakeDomBrowser browser)
    {
        var attach = browser.Engine.Recorder.GetCalls("attach")[^1];

        return (((DotNetObjectReference<DomReceiver>)attach.Arguments[1]!).Value, (string)FakeDomBrowser.GetRequestField(attach, "token")!);
    }

    private static Task EditAsync(FakeDomBrowser browser, int revision, int start, int removed, string inserted, bool isCommit = false)
    {
        var (receiver, token) = GetReceiver(browser);

        return receiver.EditAsync(token, revision, start, removed, InlineTransfer.Of(inserted), isCommit);
    }

    private static Task ReplaceAsync(FakeDomBrowser browser, int revision, string text)
    {
        var (receiver, token) = GetReceiver(browser);

        return receiver.ReplaceAsync(token, revision, InlineTransfer.Of(text));
    }

    private static IReadOnlyList<RecordedCall> GetCommands(FakeDomBrowser browser, string command)
        => [.. browser.Engine.Recorder.GetCalls("invoke").Where(call => (string?)call.Arguments[3] == command)];

    // A command the edits send from work of their own, which resumes apart from the test.
    private static Task<RecordedCall> WaitForCommandAsync(FakeDomBrowser browser, string command)
        => browser.Engine.Recorder.WaitForCallAsync("invoke", call => (string?)call.Arguments[3] == command);

    public sealed class Following
    {
        [Fact]
        public async Task Should_ask_the_browser_to_send_the_field_s_edits_from_the_text_net_holds()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);

            // Act
            await using var edits = await FollowAsync(engine, "Ada", Live, _ => Task.CompletedTask);

            // Assert
            var attach = Assert.Single(browser.Engine.Recorder.GetCalls("attach"));
            Assert.Multiple(
                () => Assert.Equal("edits", FakeDomBrowser.GetRequestField(attach, "capability")),
                () => Assert.IsType<DotNetObjectReference<DomReceiver>>(attach.Arguments[1]),
                () => Assert.True((bool)FakeDomBrowser.GetOption(attach, "sendsOnInput")!),
                () => Assert.Equal("Ada", FakeDomBrowser.GetOption(attach, "text")),
                () => Assert.Equal(0, FakeDomBrowser.GetOption(attach, "revision")),
                () => Assert.Equal("Ada", edits.Text));
        }

        [Fact]
        public async Task Should_apply_each_edit_in_revision_order_handing_each_change_to_its_handler()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            List<TextChange> changes = [];
            await using var edits = await FollowAsync(engine, "Ada", Live, change =>
            {
                changes.Add(change);
                return Task.CompletedTask;
            });

            // Act
            await EditAsync(browser, 1, 3, 0, " L");
            await EditAsync(browser, 2, 5, 0, "ovelace");

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["Ada L", "Ada Lovelace"], changes.Select(change => change.Text)),
                () => Assert.All(changes, change => Assert.False(change.IsCommit)),
                () => Assert.Equal("Ada Lovelace", edits.Text));
        }

        [Fact]
        public async Task Should_hold_an_edit_that_arrives_ahead_of_its_predecessor_until_the_predecessor_arrives()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            List<string> texts = [];
            await using var edits = await FollowAsync(engine, string.Empty, Live, change =>
            {
                texts.Add(change.Text);
                return Task.CompletedTask;
            });

            // Act
            await EditAsync(browser, 2, 1, 0, "b");
            var held = edits.Text;
            await EditAsync(browser, 1, 0, 0, "a");

            // Assert
            Assert.Multiple(
                () => Assert.Equal(string.Empty, held),
                () => Assert.Equal(["a", "ab"], texts));
        }

        [Fact]
        public async Task Should_drop_an_edit_whose_revision_its_text_has_passed()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            await using var edits = await FollowAsync(engine, "Ada", Live, _ => Task.CompletedTask);
            await EditAsync(browser, 1, 3, 0, "!");
            await using var stale = new FakeJSStreamReference("?"u8.ToArray());
            var (receiver, token) = GetReceiver(browser);

            // Act
            await receiver.EditAsync(token, 1, 3, 0, new JSTransfer { Stream = stale, IsText = true }, isCommit: false);

            // Assert
            Assert.Multiple(
                () => Assert.Equal("Ada!", edits.Text),
                () => Assert.Null(stale.OpenedWith),
                () => Assert.True(stale.IsDisposed));
        }

        [Fact]
        public async Task Should_take_an_inserted_text_streamed_past_the_transfer_s_budget()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            await using var edits = await FollowAsync(engine, string.Empty, Live, _ => Task.CompletedTask);
            var pasted = new string('x', 100_000);
            await using var stream = new FakeJSStreamReference(Encoding.UTF8.GetBytes(pasted));
            var (receiver, token) = GetReceiver(browser);

            // Act
            await receiver.EditAsync(token, 1, 0, 0, new JSTransfer { Stream = stream, IsText = true }, isCommit: false);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(pasted, edits.Text),
                () => Assert.Equal(JSTransferOptions.Default.MaximumLength, stream.OpenedWith));
        }

        [Fact]
        public async Task Should_accept_as_much_as_the_field_s_maximum_length_allows()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            await using var edits = await FollowAsync(engine, string.Empty, Live with { MaxLength = 280 }, _ => Task.CompletedTask);
            await using var stream = new FakeJSStreamReference("Notes"u8.ToArray());
            var (receiver, token) = GetReceiver(browser);

            // Act
            await receiver.EditAsync(token, 1, 0, 0, new JSTransfer { Stream = stream, IsText = true }, isCommit: false);

            // Assert
            Assert.Equal(3 * 280, stream.OpenedWith);
        }

        [Fact]
        public async Task Should_hand_its_handler_a_commit_with_the_text_committed()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            List<TextChange> changes = [];
            await using var edits = await FollowAsync(engine, "Ada", Live with { SendsOnInput = false }, change =>
            {
                changes.Add(change);
                return Task.CompletedTask;
            });

            // Act
            await EditAsync(browser, 1, 3, 0, " Lovelace", isCommit: true);

            // Assert
            var change = Assert.Single(changes);
            Assert.Multiple(
                () => Assert.Equal("Ada Lovelace", change.Text),
                () => Assert.True(change.IsCommit));
        }

        [Fact]
        public async Task Should_go_on_applying_once_its_handler_throws_logging_the_failure()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            await using var edits = await FollowAsync(engine, string.Empty, Live, _ => throw new InvalidOperationException("The host's handler broke."));

            // Act
            await EditAsync(browser, 1, 0, 0, "a");
            await EditAsync(browser, 2, 1, 0, "b");

            // Assert
            Assert.Multiple(
                () => Assert.Equal("ab", edits.Text),
                () => Assert.Equal(2, loggerFactory.Entries.Count(entry => entry.Message.Contains("handler", StringComparison.Ordinal))));
        }

        [Fact]
        public async Task Should_apply_the_next_edit_once_its_handler_has_finished_with_the_last()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            var working = new TaskCompletionSource();
            List<string> texts = [];
            await using var edits = await FollowAsync(engine, string.Empty, Live, change =>
            {
                texts.Add(change.Text);
                return working.Task;
            });

            // Act
            var first = EditAsync(browser, 1, 0, 0, "a");
            var second = EditAsync(browser, 2, 1, 0, "b");
            var whileWorking = texts.ToList();
            working.SetResult();
            await first;
            await second;

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["a"], whileWorking),
                () => Assert.Equal(["a", "ab"], texts));
        }

        [Fact]
        public async Task Should_log_a_handler_whose_work_fails_and_go_on_applying()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            var working = new TaskCompletionSource();
            await using var edits = await FollowAsync(engine, string.Empty, Live, _ => working.Task);
            var first = EditAsync(browser, 1, 0, 0, "a");
            var second = EditAsync(browser, 2, 1, 0, "b");

            // Act
            working.SetException(new InvalidOperationException("The host's handler broke later."));
            await first;
            await second;

            // Assert
            Assert.Multiple(
                () => Assert.Equal("ab", edits.Text),
                () => Assert.Contains(loggerFactory.Entries, entry => entry.Message.Contains("handler", StringComparison.Ordinal)));
        }
    }

    public sealed class Disagreeing
    {
        [Fact]
        public async Task Should_have_the_field_send_its_whole_text_in_place_of_an_inserted_text_larger_than_it_accepts()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser((command, _) => command == "resend");
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            List<string> texts = [];
            await using var edits = await FollowAsync(engine, "Ada", Live with { MaxLength = 2 }, change =>
            {
                texts.Add(change.Text);
                return Task.CompletedTask;
            });

            // Act
            await EditAsync(browser, 1, 3, 0, " Lovelace");
            await ReplaceAsync(browser, 2, "Ada!");

            // Assert
            var resend = Assert.Single(GetCommands(browser, "resend"));
            Assert.Multiple(
                () => Assert.Equal(0, resend.Arguments[4]),
                () => Assert.Equal(["Ada!"], texts),
                () => Assert.Contains(loggerFactory.Entries, entry => entry.Message.Contains("was lost on its way", StringComparison.Ordinal)));
        }

        [Fact]
        public async Task Should_have_the_field_send_its_whole_text_in_place_of_an_edit_outside_its_text()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser((command, _) => command == "resend");
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            await using var edits = await FollowAsync(engine, "Ada", Live, _ => Task.CompletedTask);

            // Act
            await EditAsync(browser, 1, 9, 2, "x");
            await EditAsync(browser, 2, 0, 0, "y");
            await ReplaceAsync(browser, 2, "yAda");

            // Assert
            Assert.Multiple(
                () => Assert.Single(GetCommands(browser, "resend")),
                () => Assert.Equal("yAda", edits.Text),
                () => Assert.Contains(loggerFactory.Entries, entry => entry.Message.Contains("falls outside the text", StringComparison.Ordinal)));
        }

        [Fact]
        public async Task Should_carry_a_commit_lost_with_its_edit_into_the_text_that_replaces_it()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser((command, _) => command == "resend");
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            List<TextChange> changes = [];
            await using var edits = await FollowAsync(engine, "Ada", Live with { SendsOnInput = false, MaxLength = 2 }, change =>
            {
                changes.Add(change);
                return Task.CompletedTask;
            });

            // Act
            await EditAsync(browser, 1, 3, 0, " Lovelace", isCommit: true);
            await ReplaceAsync(browser, 1, "Ada");

            // Assert
            var change = Assert.Single(changes);
            Assert.True(change.IsCommit);
        }

        [Fact]
        public async Task Should_ask_again_with_the_next_edit_while_the_field_cannot_be_reached_rather_than_in_a_loop()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser((command, _) => command == "resend" ? false : null);
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            await using var edits = await FollowAsync(engine, "Ada", Live with { MaxLength = 2 }, _ => Task.CompletedTask);

            // Act
            await EditAsync(browser, 1, 3, 0, " Lovelace");
            var askedFirst = GetCommands(browser, "resend").Count;
            await EditAsync(browser, 2, 12, 0, "!");

            // Assert
            Assert.Multiple(
                () => Assert.Equal(1, askedFirst),
                () => Assert.Equal(2, GetCommands(browser, "resend").Count),
                () => Assert.Equal("Ada", edits.Text));
        }
    }

    public sealed class Writing
    {
        [Fact]
        public async Task Should_write_a_text_of_its_own_through_the_field_and_stand_at_the_revision_the_field_answers()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser((command, _) => command == "write" ? 3 : null);
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            List<string> texts = [];
            await using var edits = await FollowAsync(engine, "Ada", Live, change =>
            {
                texts.Add(change.Text);
                return Task.CompletedTask;
            });

            // Act
            await edits.SyncAsync(Field, "ADA", Live);
            await EditAsync(browser, 4, 3, 0, "!");

            // Assert
            var write = Assert.Single(GetCommands(browser, "write"));
            Assert.Multiple(
                () => Assert.Equal("ADA", write.Arguments[4]),
                () => Assert.Equal(["ADA!"], texts));
        }

        [Fact]
        public async Task Should_drop_the_edits_a_write_supersedes_that_arrive_while_it_is_under_way()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            List<string> texts = [];
            await using var edits = await FollowAsync(engine, "Ada", Live, change =>
            {
                texts.Add(change.Text);
                return Task.CompletedTask;
            });
            browser.Engine.Recorder.Hold("invoke");
            var writing = edits.SyncAsync(Field, "Grace", Live);

            // Act
            await EditAsync(browser, 1, 3, 0, " L");
            await EditAsync(browser, 2, 5, 0, "o");
            GetCommands(browser, "write")[0].Complete(3);
            await writing;

            // Assert
            Assert.Multiple(
                () => Assert.Equal("Grace", edits.Text),
                () => Assert.Empty(texts));
        }

        [Fact]
        public async Task Should_keep_a_write_the_field_did_not_take_and_make_it_at_the_next_sync()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var isReachable = false;
            var browser = CreateBrowser((command, _) => command == "write" && isReachable ? 1 : null);
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            await using var edits = await FollowAsync(engine, "Ada", Live, _ => Task.CompletedTask);
            await edits.SyncAsync(Field, "Grace", Live);
            var untaken = edits.Text;

            // Act
            isReachable = true;
            await edits.SyncAsync(Field, "Grace", Live);

            // Assert
            Assert.Multiple(
                () => Assert.Equal("Ada", untaken),
                () => Assert.Equal(2, GetCommands(browser, "write").Count),
                () => Assert.Equal("Grace", edits.Text));
        }

        [Fact]
        public async Task Should_make_no_write_of_the_text_it_already_holds()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            await using var edits = await FollowAsync(engine, "Ada", Live, _ => Task.CompletedTask);
            await EditAsync(browser, 1, 3, 0, "!");

            // Act
            await edits.SyncAsync(Field, "Ada!", Live);

            // Assert
            Assert.Empty(GetCommands(browser, "write"));
        }

        [Fact]
        public async Task Should_go_on_from_its_revision_on_an_element_drawn_in_place_of_the_last()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            await using var edits = await FollowAsync(engine, "Ada", Live, _ => Task.CompletedTask);
            await EditAsync(browser, 1, 3, 0, "!");

            // Act
            await edits.SyncAsync(Redrawn, "Ada!", Live);
            await EditAsync(browser, 2, 4, 0, "?");

            // Assert
            var attach = browser.Engine.Recorder.GetCalls("attach")[^1];
            Assert.Multiple(
                () => Assert.Equal(1, FakeDomBrowser.GetOption(attach, "revision")),
                () => Assert.Equal("Ada!", FakeDomBrowser.GetOption(attach, "text")),
                () => Assert.Equal("Ada!?", edits.Text));
        }
    }

    public sealed class Showing
    {
        [Fact]
        public async Task Should_keep_the_text_it_was_rendered_with_while_the_text_shown_is_the_one_it_holds()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            await using var edits = await FollowAsync(engine, "Ada", Live, _ => Task.CompletedTask);

            // Act
            await EditAsync(browser, 1, 3, 0, " Lovelace");

            // Assert
            Assert.Equal("Ada", edits.GetShownText("Ada Lovelace"));
        }

        [Fact]
        public async Task Should_render_a_text_of_net_s_own_as_it_is_written()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser((command, _) => command == "write" ? 1 : null);
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            await using var edits = await FollowAsync(engine, "Ada", Live, _ => Task.CompletedTask);

            // Act
            var beforeWrite = edits.GetShownText("Grace");
            await edits.SyncAsync(Field, "Grace", Live);

            // Assert
            Assert.Multiple(
                () => Assert.Equal("Grace", beforeWrite),
                () => Assert.Equal("Grace", edits.GetShownText("Grace")));
        }
    }

    public sealed class Settling
    {
        [Fact]
        public async Task Should_settle_at_once_where_every_edit_received_is_applied()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            await using var edits = await FollowAsync(engine, "Ada", Live, _ => Task.CompletedTask);
            await EditAsync(browser, 1, 3, 0, "!");

            // Act
            var settling = edits.SettleAsync();

            // Assert
            Assert.Multiple(
                () => Assert.True(settling.IsCompletedSuccessfully),
                () => Assert.True(settling.Result),
                () => Assert.Empty(GetCommands(browser, "flush")));
        }

        [Fact]
        public async Task Should_settle_once_its_handler_has_finished_with_the_edit_under_way()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            var working = new TaskCompletionSource();
            await using var edits = await FollowAsync(engine, "Ada", Live, _ => working.Task);
            var editing = EditAsync(browser, 1, 3, 0, "!");

            // Act
            var settling = edits.SettleAsync();
            var isSettledWhileWorking = settling.IsCompleted;
            working.SetResult();
            await editing;

            var isSettled = await settling;

            // Assert
            Assert.Multiple(
                () => Assert.False(isSettledWhileWorking),
                () => Assert.True(isSettled));
        }

        [Fact]
        public async Task Should_have_a_field_attaching_as_the_action_began_commit_what_it_holds_and_wait_for_it()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser((command, _) => command == "flush" ? 1 : null);
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            List<TextChange> changes = [];
            await using var edits = new DomTextEdits(engine, "Ada", change =>
            {
                changes.Add(change);
                return Task.CompletedTask;
            });
            browser.Engine.Recorder.Hold("attach");
            var syncing = edits.SyncAsync(Field, "Ada", Live with { SendsOnInput = false });
            var settling = edits.SettleAsync();
            browser.Engine.Recorder.Hold("invoke");

            // Act — the attach lands, and the field's commit reaches .NET ahead of the flush's answer
            browser.Engine.Recorder.GetCalls("attach")[0].Complete(true);
            await syncing;
            var flush = await WaitForCommandAsync(browser, "flush");
            await EditAsync(browser, 1, 3, 0, " L", isCommit: true);
            var isSettledBeforeTheAnswer = settling.IsCompleted;
            flush.Complete(1);

            var isSettled = await settling;

            // Assert
            Assert.Multiple(
                () => Assert.False(isSettledBeforeTheAnswer),
                () => Assert.True(isSettled),
                () => Assert.Equal("Ada L", Assert.Single(changes).Text));
        }

        [Fact]
        public async Task Should_have_the_field_send_its_whole_text_where_an_edit_sent_before_the_action_never_arrived()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            await using var edits = new DomTextEdits(engine, "Ada", _ => Task.CompletedTask);
            browser.Engine.Recorder.Hold("attach");
            var syncing = edits.SyncAsync(Field, "Ada", Live);
            var settling = edits.SettleAsync();
            browser.Engine.Recorder.Hold("invoke");
            browser.Engine.Recorder.GetCalls("attach")[0].Complete(true);
            await syncing;

            // Act — the flush answers revision 2, though the edit before it never arrived
            await EditAsync(browser, 2, 4, 0, "!");
            (await WaitForCommandAsync(browser, "flush")).Complete(2);
            (await WaitForCommandAsync(browser, "resend")).Complete(true);
            await ReplaceAsync(browser, 2, "Ada?!");

            var isSettled = await settling;

            // Assert
            Assert.Multiple(
                () => Assert.Single(GetCommands(browser, "resend")),
                () => Assert.True(isSettled),
                () => Assert.Equal("Ada?!", edits.Text));
        }

        [Fact]
        public async Task Should_fail_to_settle_where_the_text_can_t_follow_the_field_and_tell_its_handler()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser((command, _) => command == "resend" ? false : null);
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            List<TextChange> changes = [];
            await using var edits = await FollowAsync(engine, "Ada", Live with { MaxLength = 2 }, change =>
            {
                changes.Add(change);
                return Task.CompletedTask;
            });
            await EditAsync(browser, 1, 3, 0, " Lovelace");

            // Act
            var isSettled = await edits.SettleAsync();

            // Assert
            Assert.Multiple(
                () => Assert.False(isSettled),
                () => Assert.True(edits.IsFaulted),
                () => Assert.Equal("Ada", Assert.Single(changes, change => change.IsFaulted).Text));
        }

        [Fact]
        public async Task Should_tell_its_handler_the_text_follows_the_field_again_once_its_whole_text_arrives()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var isReachable = false;
            var browser = CreateBrowser((command, _) => command == "resend" ? isReachable : null);
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            List<TextChange> changes = [];
            await using var edits = await FollowAsync(engine, "Ada", Live with { MaxLength = 2 }, change =>
            {
                changes.Add(change);
                return Task.CompletedTask;
            });
            await EditAsync(browser, 1, 3, 0, " Lovelace");
            isReachable = true;

            // Act
            await EditAsync(browser, 2, 0, 12, "Al");
            await ReplaceAsync(browser, 2, "Al");

            var isSettled = await edits.SettleAsync();

            // Assert
            Assert.Multiple(
                () => Assert.False(edits.IsFaulted),
                () => Assert.True(isSettled),
                () => Assert.True(changes[0].IsFaulted),
                () => Assert.False(changes[^1].IsFaulted),
                () => Assert.Equal("Al", changes[^1].Text));
        }

        [Fact]
        public async Task Should_settle_at_once_where_the_field_never_attached()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = new FakeDomBrowser((_, identifier, _) => identifier == "attach" ? false : null);
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            await using var edits = await FollowAsync(engine, "Ada", Live, _ => Task.CompletedTask);

            // Act
            var isSettled = await edits.SettleAsync();

            // Assert
            Assert.True(isSettled);
        }
    }

    public sealed class Counting
    {
        [Fact]
        public async Task Should_ask_the_browser_to_count_into_the_output_with_the_most_the_field_accepts()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);

            // Act
            await using var edits = await FollowAsync(engine, string.Empty, Live with { MaxLength = 280, CountOutputId = "field-count" }, _ => Task.CompletedTask);

            // Assert
            var count = FakeDomBrowser.GetOption(Assert.Single(browser.Engine.Recorder.GetCalls("attach")), "count")!;
            Assert.Multiple(
                () => Assert.Equal("field-count", InteropPayload.GetValue(count, "output")),
                () => Assert.Equal(280, InteropPayload.GetValue(count, "maxLength")));
        }

        [Fact]
        public async Task Should_ask_for_no_count_where_the_options_name_no_output()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);

            // Act
            await using var edits = await FollowAsync(engine, string.Empty, Live, _ => Task.CompletedTask);

            // Assert
            Assert.Null(FakeDomBrowser.GetOption(Assert.Single(browser.Engine.Recorder.GetCalls("attach")), "count"));
        }

        [Fact]
        public async Task Should_refuse_an_empty_output()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => FollowAsync(engine, string.Empty, Live with { CountOutputId = " " }, _ => Task.CompletedTask));

            // Assert
            Assert.Multiple(
                () => Assert.Equal("options", exception.ParamName),
                () => Assert.Empty(browser.Runtime.Recorder.Calls));
        }
    }

    public sealed class Releasing
    {
        [Fact]
        public async Task Should_release_unread_an_edit_still_waiting_as_its_edits_are_released()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            var edits = await FollowAsync(engine, string.Empty, Live, _ => Task.CompletedTask);
            await using var waiting = new FakeJSStreamReference("b"u8.ToArray());
            var (receiver, token) = GetReceiver(browser);
            await receiver.EditAsync(token, 2, 1, 0, new JSTransfer { Stream = waiting, IsText = true }, isCommit: false);

            // Act
            await edits.DisposeAsync();

            // Assert
            Assert.Multiple(
                () => Assert.Null(waiting.OpenedWith),
                () => Assert.True(waiting.IsDisposed));
        }

        [Fact]
        public async Task Should_release_unread_an_edit_for_a_registration_it_does_not_hold()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            await using var edits = await FollowAsync(engine, string.Empty, Live, _ => Task.CompletedTask);
            await using var stream = new FakeJSStreamReference("x"u8.ToArray());
            var (receiver, _) = GetReceiver(browser);

            // Act
            await receiver.EditAsync("99", 1, 0, 0, new JSTransfer { Stream = stream, IsText = true }, isCommit: false);

            // Assert
            Assert.Multiple(
                () => Assert.Null(stream.OpenedWith),
                () => Assert.True(stream.IsDisposed),
                () => Assert.Equal(string.Empty, edits.Text));
        }

        [Fact]
        public async Task Should_refuse_a_maximum_length_below_one()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => FollowAsync(engine, string.Empty, Live with { MaxLength = 0 }, _ => Task.CompletedTask));

            // Assert
            Assert.Multiple(
                () => Assert.Equal("options", exception.ParamName),
                () => Assert.Empty(browser.Runtime.Recorder.Calls));
        }
    }
}
