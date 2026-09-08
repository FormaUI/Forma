namespace Forma.Framework.Windows.Tests;

using Forma.Tests.Windows;

using Xunit;

public sealed class WindowManagerTests
{
    private static async Task<FakeWindow> AddAsync(WindowManager manager, string title, IWindow? parent = null, WindowOrigin? origin = null, Action<FakeWindow>? configure = null)
    {
        var window = new FakeWindow(title, origin) { Parent = parent };
        configure?.Invoke(window);
        await manager.RegisterAsync(window);

        return window;
    }

    private static WindowSelector Only(IWindow window)
        => new() { Window = window };

    private static List<string> Titles(IEnumerable<IWindow> windows)
        => [.. windows.Select(window => window.Title)];

    public sealed class Registration
    {
        [Fact]
        public async Task Should_place_each_new_window_at_the_front()
        {
            // Arrange
            var manager = new WindowManager();

            // Act
            await AddAsync(manager, "First");
            await AddAsync(manager, "Second");

            // Assert
            Assert.Equal(["First", "Second"], Titles(manager.StackedWindows));
        }

        [Fact]
        public async Task Should_reject_a_window_registered_twice()
        {
            // Arrange
            var manager = new WindowManager();
            var window = await AddAsync(manager, "Only");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () => await manager.RegisterAsync(window));
        }

        [Fact]
        public async Task Should_reject_a_parent_from_another_workspace()
        {
            // Arrange
            var manager = new WindowManager();
            var elsewhere = await AddAsync(new WindowManager(), "Elsewhere");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () => await manager.RegisterAsync(new FakeWindow("Child") { Parent = elsewhere }));
        }

        [Fact]
        public async Task Should_hide_a_window_registered_under_a_minimized_parent()
        {
            // Arrange
            var manager = new WindowManager();
            var parent = await AddAsync(manager, "Parent", configure: window => window.IsMinimized = true);

            // Act
            var child = await AddAsync(manager, "Child", parent);

            // Assert
            Assert.Multiple(
                () => Assert.True(child.IsHidden),
                () => Assert.False(child.IsMinimized),
                () => Assert.True(manager.IsHidden(child)));
        }

        [Fact]
        public async Task Should_raise_changed_when_a_window_registers_or_refreshes()
        {
            // Arrange
            var manager = new WindowManager();
            var changes = 0;
            manager.Changed += (_, _) => changes++;

            // Act
            var window = await AddAsync(manager, "Window");
            await manager.RefreshAsync(window);

            // Assert
            Assert.Equal(2, changes);
        }

        [Fact]
        public async Task Should_look_a_window_up_by_identity()
        {
            // Arrange
            var manager = new WindowManager();
            var window = await AddAsync(manager, "Window");

            // Act & Assert
            Assert.Multiple(
                () => Assert.Same(window, manager.GetWindowById(window.Id)),
                () => Assert.True(manager.TryGetWindowById(window.Id, out _)),
                () => Assert.False(manager.TryGetWindowById(Guid.NewGuid(), out var missing) || missing is not null),
                () => Assert.Throws<KeyNotFoundException>(() => manager.GetWindowById(Guid.NewGuid())));
        }
    }

    public sealed class Parentage
    {
        [Fact]
        public async Task Should_reject_a_window_as_its_own_parent_or_its_descendants_parent()
        {
            // Arrange
            var manager = new WindowManager();
            var parent = await AddAsync(manager, "Parent");
            var child = await AddAsync(manager, "Child", parent);

            // Act
            parent.Parent = parent;
            var self = await Record.ExceptionAsync(async () => await manager.RefreshAsync(parent));
            parent.Parent = child;
            var cycle = await Record.ExceptionAsync(async () => await manager.RefreshAsync(parent));

            // Assert
            Assert.Multiple(
                () => Assert.IsType<ArgumentException>(self),
                () => Assert.IsType<ArgumentException>(cycle),
                () => Assert.Null(manager.ParentOf(parent)));
        }

        [Fact]
        public async Task Should_move_a_window_under_the_parent_it_now_declares()
        {
            // Arrange
            var manager = new WindowManager();
            var first = await AddAsync(manager, "First");
            var second = await AddAsync(manager, "Second");
            var child = await AddAsync(manager, "Child", first);

            // Act
            child.Parent = second;
            await manager.RefreshAsync(child);

            // Assert
            Assert.Multiple(
                () => Assert.Same(second, manager.ParentOf(child)),
                () => Assert.Empty(manager.ChildrenOf(first)),
                () => Assert.Equal(["Child"], Titles(manager.ChildrenOf(second))));
        }

        [Fact]
        public async Task Should_hide_a_window_moved_under_a_minimized_parent()
        {
            // Arrange
            var manager = new WindowManager();
            var minimized = await AddAsync(manager, "Minimized", configure: window => window.IsMinimized = true);
            var child = await AddAsync(manager, "Child");

            // Act
            child.Parent = minimized;
            await manager.RefreshAsync(child);

            // Assert
            Assert.True(child.IsHidden);
        }
    }

    public sealed class Refreshing
    {
        [Fact]
        public async Task Should_give_way_when_the_current_window_is_disabled()
        {
            // Arrange
            var manager = new WindowManager();
            var older = await AddAsync(manager, "Older");
            var current = await AddAsync(manager, "Current");
            await manager.ActivateAsync(Only(older));
            await manager.ActivateAsync(Only(current));

            // Act
            current.IsDisabled = true;
            await manager.RefreshAsync(current);

            // Assert
            Assert.Multiple(
                () => Assert.Same(older, manager.Current),
                () => Assert.False(current.IsActive));
        }

        [Fact]
        public async Task Should_move_a_window_that_becomes_modal_in_front_of_what_it_blocks()
        {
            // Arrange
            var manager = new WindowManager();
            var modal = await AddAsync(manager, "Modal");
            await AddAsync(manager, "Page");

            // Act
            modal.IsModal = true;
            await manager.RefreshAsync(modal);

            // Assert
            Assert.Equal(["Page", "Modal"], Titles(manager.StackedWindows));
        }
    }

    public sealed class Selection
    {
        [Fact]
        public async Task Should_match_every_window_to_an_empty_search()
        {
            // Arrange
            var manager = new WindowManager();
            await AddAsync(manager, "One");
            await AddAsync(manager, "Two");

            // Act
            var found = manager.TryFindWindows(new WindowSelector(), out var windows);

            // Assert
            Assert.Multiple(
                () => Assert.True(found),
                () => Assert.Equal(["One", "Two"], Titles(windows)));
        }

        [Fact]
        public async Task Should_require_the_title_to_match_a_known_window()
        {
            // Arrange
            var manager = new WindowManager();
            var window = await AddAsync(manager, "Properties");

            // Act
            manager.TryFindWindows(new WindowSelector { Window = window, Title = "Properties" }, out var matching);
            var mismatched = manager.TryFindWindows(new WindowSelector { Window = window, Title = "Other" }, out _);

            // Assert
            Assert.Multiple(
                () => Assert.Equal([window], matching),
                () => Assert.False(mismatched));
        }

        [Fact]
        public async Task Should_select_by_origin_and_by_instance()
        {
            // Arrange
            var manager = new WindowManager();
            var first = WindowOrigin.NewInstance("explorer", "Explorer");
            var second = WindowOrigin.NewInstance("explorer", "Explorer");
            await AddAsync(manager, "A", origin: first);
            await AddAsync(manager, "B", origin: first);
            await AddAsync(manager, "C", origin: second);
            await AddAsync(manager, "D", origin: WindowOrigin.NewInstance("settings", "Settings"));

            // Act
            manager.TryFindWindows(new WindowSelector { OriginId = "explorer" }, out var byOrigin);
            manager.TryFindWindows(new WindowSelector { InstanceId = second.InstanceId }, out var byInstance);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["A", "B", "C"], Titles(byOrigin)),
                () => Assert.Equal(["C"], Titles(byInstance)));
        }

        [Fact]
        public async Task Should_match_titles_exactly_and_return_every_match()
        {
            // Arrange
            var manager = new WindowManager();
            await AddAsync(manager, "Properties");
            await AddAsync(manager, "properties");
            await AddAsync(manager, "Properties");

            // Act
            manager.TryFindWindows(new WindowSelector { Title = "Properties" }, out var windows);

            // Assert
            Assert.Equal(2, windows.Count);
        }

        [Fact]
        public async Task Should_select_children_or_every_descendant_but_never_the_parent()
        {
            // Arrange
            var manager = new WindowManager();
            var parent = await AddAsync(manager, "Parent");
            var child = await AddAsync(manager, "Child", parent);
            await AddAsync(manager, "Grandchild", child);
            await AddAsync(manager, "Sibling");

            // Act
            manager.TryFindWindows(new WindowSelector { Parent = parent }, out var children);
            manager.TryFindWindows(new WindowSelector { Parent = parent, Recursive = true }, out var descendants);
            manager.TryFindWindows(new WindowSelector { Recursive = true }, out var everything);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["Child"], Titles(children)),
                () => Assert.Equal(["Child", "Grandchild"], Titles(descendants)),
                () => Assert.Equal(4, everything.Count));
        }

        [Fact]
        public async Task Should_resolve_the_standing_selections_from_the_workspace()
        {
            // Arrange
            var manager = new WindowManager();
            var back = await AddAsync(manager, "Back");
            var front = await AddAsync(manager, "Front");
            await manager.ActivateAsync(Only(back));
            await manager.ActivateAsync(Only(front));

            // Act
            manager.TryFindWindows(WindowSelector.All, out var all);
            manager.TryFindWindows(WindowSelector.Current, out var current);
            manager.TryFindWindows(WindowSelector.Previous, out var previous);
            manager.TryFindWindows(WindowSelector.Frontmost, out var frontmost);
            manager.TryFindWindows(WindowSelector.Backmost, out var backmost);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(2, all.Count),
                () => Assert.Equal([front], current),
                () => Assert.Equal([back], previous),
                () => Assert.Equal([front], frontmost),
                () => Assert.Equal([back], backmost));
        }

        [Fact]
        public async Task Should_reject_an_empty_operation_selector_and_a_foreign_window()
        {
            // Arrange
            var manager = new WindowManager();
            await AddAsync(manager, "Window");
            var foreign = await AddAsync(new WindowManager(), "Foreign");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () => await manager.MinimizeAsync(new WindowSelector()));
            await Assert.ThrowsAsync<ArgumentException>(async () => await manager.MinimizeAsync(new WindowSelector { Recursive = true }));
            await Assert.ThrowsAsync<ArgumentException>(async () => await manager.MinimizeAsync(Only(foreign)));
            Assert.Throws<ArgumentException>(() => manager.TryFindWindows(new OtherSelector(), out _));
        }

        private sealed class OtherSelector : IWindowSelector;

        [Fact]
        public async Task Should_scope_a_selector_to_a_window_and_its_descendants()
        {
            // Arrange
            var manager = new WindowManager();
            var window = await AddAsync(manager, "Window");
            var child = await AddAsync(manager, "Child", window);
            await AddAsync(manager, "Properties", child);
            await AddAsync(manager, "Properties");

            // Act
            manager.TryFindWindows(manager.Within(window), out var itself);
            manager.TryFindWindows(manager.Within(window, WindowSelector.All), out var everyDescendant);
            manager.TryFindWindows(manager.Within(window, new WindowSelector { Title = "Properties" }), out var titled);
            manager.TryFindWindows(manager.Within(window, new WindowSelector { Parent = child }), out var underChild);

            // Assert
            Assert.Multiple(
                () => Assert.Equal([window], itself),
                () => Assert.Equal(["Child", "Properties"], Titles(everyDescendant)),
                () => Assert.Single(titled),
                () => Assert.Equal(["Properties"], Titles(underChild)));
        }

        [Fact]
        public async Task Should_refuse_to_scope_past_the_window()
        {
            // Arrange
            var manager = new WindowManager();
            var window = await AddAsync(manager, "Window");
            var sibling = await AddAsync(manager, "Sibling");

            // Act & Assert
            Assert.Multiple(
                () => Assert.Throws<ArgumentException>(() => manager.Within(window, new WindowSelector { Parent = sibling })),
                () => Assert.Throws<ArgumentException>(() => manager.Within(window, WindowSelector.Current)));
        }
    }

    public sealed class Activation
    {
        [Fact]
        public async Task Should_report_no_match_and_ambiguity()
        {
            // Arrange
            var manager = new WindowManager();
            await AddAsync(manager, "Twin");
            await AddAsync(manager, "Twin");

            // Act
            var none = await manager.ActivateAsync(new WindowSelector { Title = "Missing" });
            var both = await manager.ActivateAsync(new WindowSelector { Title = "Twin" });

            // Assert
            Assert.Multiple(
                () => Assert.Equal(WindowOperationOutcome.NoMatch, none.Outcome),
                () => Assert.Equal(WindowOperationOutcome.Ambiguous, both.Outcome),
                () => Assert.Null(manager.Current));
        }

        [Fact]
        public async Task Should_make_the_window_current_and_bring_it_to_the_front()
        {
            // Arrange
            var manager = new WindowManager();
            var first = await AddAsync(manager, "First");
            var second = await AddAsync(manager, "Second");
            await manager.ActivateAsync(Only(second));

            // Act
            var result = await manager.ActivateAsync(Only(first));

            // Assert
            Assert.Multiple(
                () => Assert.True(result.Succeeded),
                () => Assert.Same(first, manager.Current),
                () => Assert.True(first.IsActive),
                () => Assert.False(second.IsActive),
                () => Assert.Equal(["Second", "First"], Titles(manager.StackedWindows)));
        }

        [Fact]
        public async Task Should_restore_minimized_ancestors_and_the_window_itself()
        {
            // Arrange
            var manager = new WindowManager();
            var parent = await AddAsync(manager, "Parent");
            var child = await AddAsync(manager, "Child", parent);
            await manager.MinimizeAsync(Only(child));
            await manager.MinimizeAsync(Only(parent));

            // Act
            await manager.ActivateAsync(Only(child));

            // Assert
            Assert.Multiple(
                () => Assert.False(parent.IsMinimized),
                () => Assert.False(child.IsMinimized),
                () => Assert.False(child.IsHidden),
                () => Assert.Same(child, manager.Current));
        }

        [Fact]
        public async Task Should_redirect_to_the_modal_that_controls_the_window()
        {
            // Arrange
            var manager = new WindowManager();
            var parent = await AddAsync(manager, "Parent");
            var modal = await AddAsync(manager, "Modal", parent, configure: window => window.IsModal = true);
            var sibling = await AddAsync(manager, "Sibling");

            // Act
            var fromParent = await manager.ActivateAsync(Only(parent));
            var fromSibling = await manager.ActivateAsync(Only(sibling));

            // Assert
            Assert.Multiple(
                () => Assert.Equal([modal], fromParent.Windows),
                () => Assert.Same(sibling, manager.Current),
                () => Assert.True(fromSibling.Succeeded),
                () => Assert.True(manager.IsBlocked(parent)),
                () => Assert.False(manager.IsBlocked(sibling)));
        }

        [Fact]
        public async Task Should_redirect_a_blocked_window_to_its_blocker()
        {
            // Arrange
            var manager = new WindowManager();
            var blocked = await AddAsync(manager, "Blocked");
            var modal = await AddAsync(manager, "Modal", configure: window => window.IsModal = true);

            // Act
            var result = await manager.ActivateAsync(Only(blocked));

            // Assert
            Assert.Multiple(
                () => Assert.Equal([modal], result.Windows),
                () => Assert.Same(modal, manager.Current));
        }

        [Fact]
        public async Task Should_tell_the_windows_a_modal_blocks_and_release_them_when_it_goes()
        {
            // Arrange
            var manager = new WindowManager();
            var parent = await AddAsync(manager, "Parent");
            var sibling = await AddAsync(manager, "Sibling");
            var modal = new FakeWindow("Modal") { Parent = parent, IsModal = true };

            // Act
            await manager.RegisterAsync(modal);
            var blockedWhileShown = (parent.IsBlocked, sibling.IsBlocked);
            await manager.MinimizeAsync(Only(modal));
            var blockedWhileMinimized = parent.IsBlocked;
            await manager.RestoreAsync(Only(modal));
            await manager.CloseAsync(Only(modal));

            // Assert
            Assert.Multiple(
                () => Assert.Equal((true, false), blockedWhileShown),
                () => Assert.False(blockedWhileMinimized),
                () => Assert.False(parent.IsBlocked));
        }

        [Fact]
        public async Task Should_take_a_window_a_registering_modal_blocks_out_of_current()
        {
            // Arrange
            var manager = new WindowManager();
            var page = await AddAsync(manager, "Page");
            await manager.ActivateAsync(Only(page));

            // Act
            await AddAsync(manager, "Modal", configure: window => window.IsModal = true);

            // Assert
            Assert.Multiple(
                () => Assert.False(page.IsActive),
                () => Assert.NotEqual(page, manager.Current));
        }

        [Fact]
        public async Task Should_block_the_whole_workspace_behind_a_root_modal()
        {
            // Arrange
            var manager = new WindowManager();
            var one = await AddAsync(manager, "One");
            var two = await AddAsync(manager, "Two");

            // Act
            var modal = await AddAsync(manager, "Modal", configure: window => window.IsModal = true);

            // Assert
            Assert.Multiple(
                () => Assert.True(one.IsBlocked),
                () => Assert.True(two.IsBlocked),
                () => Assert.False(modal.IsBlocked));
        }

        [Fact]
        public async Task Should_not_activate_a_disabled_window()
        {
            // Arrange
            var manager = new WindowManager();
            var window = await AddAsync(manager, "Window", configure: window => window.IsDisabled = true);

            // Act
            var result = await manager.ActivateAsync(Only(window));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(WindowOperationOutcome.Ineligible, result.Outcome),
                () => Assert.Null(manager.Current));
        }

        [Fact]
        public async Task Should_fall_back_to_the_parent_then_to_the_most_recent_window()
        {
            // Arrange
            var manager = new WindowManager();
            var older = await AddAsync(manager, "Older");
            var parent = await AddAsync(manager, "Parent");
            var child = await AddAsync(manager, "Child", parent);
            await manager.ActivateAsync(Only(older));
            await manager.ActivateAsync(Only(child));

            // Act
            await manager.CloseAsync(Only(child));
            var afterChild = manager.Current;
            await manager.MinimizeAsync(Only(parent));
            var afterParent = manager.Current;
            await manager.MinimizeAsync(Only(older));

            // Assert
            Assert.Multiple(
                () => Assert.Same(parent, afterChild),
                () => Assert.Same(older, afterParent),
                () => Assert.Null(manager.Current));
        }
    }

    public sealed class Stacking
    {
        [Fact]
        public async Task Should_move_a_group_keeping_its_order()
        {
            // Arrange
            var manager = new WindowManager();
            var origin = WindowOrigin.NewInstance("explorer", "Explorer");
            await AddAsync(manager, "A", origin: origin);
            await AddAsync(manager, "B");
            await AddAsync(manager, "C", origin: origin);
            await AddAsync(manager, "D");

            // Act
            await manager.BringToFrontAsync(new WindowSelector { OriginId = "explorer" });
            var forward = Titles(manager.StackedWindows);
            await manager.SendToBackAsync(new WindowSelector { OriginId = "explorer" });

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["B", "D", "A", "C"], forward),
                () => Assert.Equal(["A", "C", "B", "D"], Titles(manager.StackedWindows)));
        }

        [Fact]
        public async Task Should_keep_the_registration_order_while_the_stacking_order_changes()
        {
            // Arrange
            var manager = new WindowManager();
            var first = await AddAsync(manager, "First");
            await AddAsync(manager, "Second");

            // Act
            await manager.ActivateAsync(Only(first));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["Second", "First"], Titles(manager.StackedWindows)),
                () => Assert.Equal(["First", "Second"], Titles(manager.Windows)));
        }

        [Fact]
        public async Task Should_keep_a_visible_modal_in_front_of_what_it_blocks()
        {
            // Arrange
            var manager = new WindowManager();
            await AddAsync(manager, "Page");
            var modal = await AddAsync(manager, "Modal", configure: window => window.IsModal = true);

            // Act
            await manager.SendToBackAsync(Only(modal));

            // Assert
            Assert.Equal(["Page", "Modal"], Titles(manager.StackedWindows));
        }

        [Fact]
        public async Task Should_raise_visible_floating_windows_back_to_front()
        {
            // Arrange
            var manager = new WindowManager();
            var docked = await AddAsync(manager, "Docked", configure: window => window.IsFloating = false);
            var back = await AddAsync(manager, "Back");
            var front = await AddAsync(manager, "Front");
            back.Log.Clear();
            front.Log.Clear();

            // Act
            await manager.BringToFrontAsync(Only(front));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["raise"], back.Log),
                () => Assert.Equal(["raise"], front.Log),
                () => Assert.DoesNotContain("raise", docked.Log));
        }

        [Fact]
        public async Task Should_tolerate_a_window_registering_while_another_is_raised()
        {
            // Arrange
            var manager = new WindowManager();
            var first = await AddAsync(manager, "First");
            await AddAsync(manager, "Second");
            first.WhileRaising = async () => await manager.RegisterAsync(new FakeWindow("Late"));

            // Act
            var result = await manager.ActivateAsync(Only(first));

            // Assert
            Assert.Multiple(
                () => Assert.True(result.Succeeded),
                () => Assert.Equal(3, manager.StackedWindows.Count));
        }

        [Fact]
        public async Task Should_bring_forward_without_activating()
        {
            // Arrange
            var manager = new WindowManager();
            var window = await AddAsync(manager, "Window");
            await AddAsync(manager, "Other");

            // Act
            await manager.BringToFrontAsync(Only(window));

            // Assert
            Assert.Multiple(
                () => Assert.Null(manager.Current),
                () => Assert.False(window.IsActive),
                () => Assert.Equal("Window", manager.StackedWindows[^1].Title));
        }
    }

    public sealed class Minimization
    {
        [Fact]
        public async Task Should_hide_every_descendant_without_minimizing_it()
        {
            // Arrange
            var manager = new WindowManager();
            var parent = await AddAsync(manager, "Parent");
            var child = await AddAsync(manager, "Child", parent);
            var grandchild = await AddAsync(manager, "Grandchild", child);

            // Act
            var result = await manager.MinimizeAsync(Only(parent));

            // Assert
            Assert.Multiple(
                () => Assert.Equal([parent], result.Windows),
                () => Assert.True(parent.IsMinimized),
                () => Assert.True(child.IsHidden),
                () => Assert.True(grandchild.IsHidden),
                () => Assert.False(child.IsMinimized),
                () => Assert.False(manager.IsEligible(grandchild)));
        }

        [Fact]
        public async Task Should_leave_independently_minimized_descendants_minimized_on_restoration()
        {
            // Arrange
            var manager = new WindowManager();
            var parent = await AddAsync(manager, "Parent");
            var child = await AddAsync(manager, "Child", parent);
            var grandchild = await AddAsync(manager, "Grandchild", child);
            await manager.MinimizeAsync(Only(child));
            await manager.MinimizeAsync(Only(parent));

            // Act
            await manager.RestoreAsync(Only(parent));

            // Assert
            Assert.Multiple(
                () => Assert.False(parent.IsMinimized),
                () => Assert.True(child.IsMinimized),
                () => Assert.False(child.IsHidden),
                () => Assert.True(grandchild.IsHidden));
        }

        [Fact]
        public async Task Should_make_a_lone_restored_window_current_but_not_a_group()
        {
            // Arrange
            var manager = new WindowManager();
            var one = await AddAsync(manager, "One");
            var two = await AddAsync(manager, "Two");
            await manager.MinimizeAsync(WindowSelector.All);

            // Act
            await manager.RestoreAsync(Only(one));
            var afterOne = manager.Current;
            await manager.MinimizeAsync(WindowSelector.All);
            await manager.RestoreAsync(WindowSelector.All);

            // Assert
            Assert.Multiple(
                () => Assert.Same(one, afterOne),
                () => Assert.Null(manager.Current),
                () => Assert.False(two.IsMinimized));
        }

        [Fact]
        public async Task Should_report_already_minimized_windows_as_ineligible()
        {
            // Arrange
            var manager = new WindowManager();
            var window = await AddAsync(manager, "Window");
            await manager.MinimizeAsync(Only(window));

            // Act
            var again = await manager.MinimizeAsync(Only(window));

            // Assert
            Assert.Equal(WindowOperationOutcome.Ineligible, again.Outcome);
        }
    }

    public sealed class Closing
    {
        [Fact]
        public async Task Should_ask_the_whole_subtree_before_closing_descendants_first()
        {
            // Arrange
            var manager = new WindowManager();
            var parent = await AddAsync(manager, "Parent");
            var child = await AddAsync(manager, "Child", parent);
            var grandchild = await AddAsync(manager, "Grandchild", child);
            var log = new List<string>();
            foreach (var window in new[] { parent, child, grandchild })
            {
                window.WhileClosing = () => { log.Add($"ask {window.Title}"); return ValueTask.CompletedTask; };
            }

            // Act
            var result = await manager.CloseAsync(Only(parent));

            // Assert
            Assert.Multiple(
                () => Assert.True(result.Succeeded),
                () => Assert.Equal(["ask Parent", "ask Child", "ask Grandchild"], log),
                () => Assert.True(grandchild.IsClosed && child.IsClosed && parent.IsClosed),
                () => Assert.Empty(manager.StackedWindows));
        }

        [Fact]
        public async Task Should_leave_every_window_open_when_one_declines()
        {
            // Arrange
            var manager = new WindowManager();
            var parent = await AddAsync(manager, "Parent");
            var child = await AddAsync(manager, "Child", parent, configure: window => window.AcceptsClose = false);
            var other = await AddAsync(manager, "Other");

            // Act
            var result = await manager.CloseAsync(new WindowSelector { Title = "Parent" });
            var several = await manager.CloseAsync(WindowSelector.All);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(WindowOperationOutcome.Declined, result.Outcome),
                () => Assert.Equal([child], result.Windows),
                () => Assert.Equal(WindowOperationOutcome.Declined, several.Outcome),
                () => Assert.False(parent.IsClosed || child.IsClosed || other.IsClosed),
                () => Assert.Equal(3, manager.StackedWindows.Count));
        }

        [Fact]
        public async Task Should_process_an_overlapping_selection_once_per_window()
        {
            // Arrange
            var manager = new WindowManager();
            var parent = await AddAsync(manager, "Parent");
            var child = await AddAsync(manager, "Child", parent);

            // Act
            var result = await manager.CloseAsync(new WindowSelector { OriginId = child.Origin.OriginId, Title = "Child" } with { Window = null });
            await AddAsync(manager, "Child", parent);
            var both = await manager.CloseAsync(WindowSelector.All);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(1, child.CloseRequests),
                () => Assert.Equal(1, parent.CloseRequests),
                () => Assert.Equal(2, both.Windows.Count),
                () => Assert.True(result.Succeeded));
        }

        [Fact]
        public async Task Should_refuse_a_new_child_while_the_parent_is_being_asked()
        {
            // Arrange
            var manager = new WindowManager();
            var parent = await AddAsync(manager, "Parent");
            Exception? caught = null;
            parent.WhileClosing = async () =>
            {
                try
                {
                    await manager.RegisterAsync(new FakeWindow("Late") { Parent = parent });
                }
                catch (InvalidOperationException exception)
                {
                    caught = exception;
                }
            };

            // Act
            await manager.CloseAsync(Only(parent));

            // Assert
            Assert.Multiple(
                () => Assert.NotNull(caught),
                () => Assert.Empty(manager.StackedWindows));
        }

        [Fact]
        public async Task Should_close_descendants_without_asking_when_the_host_removes_a_parent()
        {
            // Arrange
            var manager = new WindowManager();
            var parent = await AddAsync(manager, "Parent");
            var child = await AddAsync(manager, "Child", parent);
            await manager.ActivateAsync(Only(child));

            // Act
            await manager.UnregisterAsync(parent);

            // Assert
            Assert.Multiple(
                () => Assert.True(child.IsClosed),
                () => Assert.Equal(0, child.CloseRequests),
                () => Assert.Empty(manager.StackedWindows),
                () => Assert.Null(manager.Current));
        }
    }
}
