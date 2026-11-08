namespace Forma.Blazor.Browser.Tests.Components;

/// <summary>Every check page under static server rendering, which runs no event and no script.</summary>
public sealed class StaticRenderingTests(BrowserHost host)
{
    // The controls a component offers in a static render that need interactivity to act: an enabled button other than a form's
    // submit, an element of a widget's role a reader can reach that isn't marked disabled, and an editable element, whose typing
    // reaches no script. A link and a native form control keep what the browser gives them, what the page doesn't show is offered
    // to no one, and what the check page marks as its own scaffolding is no component's.
    private const string FindControlsNeedingInteractivity = """
        () => {
            const roles = ['button', 'tab', 'menuitem', 'menuitemcheckbox', 'menuitemradio', 'option', 'switch', 'checkbox', 'radio', 'slider', 'spinbutton', 'combobox', 'textbox', 'treeitem', 'gridcell'];
            const describe = element => `${element.localName}${element.id ? `#${element.id}` : ''}${element.classList.length > 0 ? `.${[...element.classList].join('.')}` : ''} "${element.textContent.trim().slice(0, 40)}"`;
            const found = [];

            for (const element of document.querySelectorAll('button, [role], [contenteditable]')) {
                if (!element.checkVisibility() || element.closest('[data-check-scaffold]') !== null || ['input', 'select', 'textarea'].includes(element.localName)) {
                    continue;
                }

                if (element.localName === 'button') {
                    if (!element.disabled && !(element.type === 'submit' && element.form !== null)) {
                        found.push(describe(element));
                    }

                    continue;
                }

                if (element.isContentEditable || (roles.includes(element.getAttribute('role')) && element.tabIndex >= 0 && element.getAttribute('aria-disabled') !== 'true')) {
                    found.push(describe(element));
                }
            }

            return found;
        }
        """;

    // What a static render shows waiting on a script that never runs: a symbol reference nothing in the document resolves, and a spinner.
    private const string FindWaits = """
        () => {
            const found = [];

            for (const use of document.querySelectorAll('use')) {
                const href = use.getAttribute('href') ?? '';

                if (href.startsWith('#') && document.getElementById(href.slice(1)) === null) {
                    found.push(`use "${href}", which nothing in the document resolves`);
                }
            }

            for (const spinner of document.querySelectorAll('.forma-spinner')) {
                if (spinner.checkVisibility() && spinner.closest('[data-check-scaffold]') === null) {
                    found.push(`spinner "${spinner.textContent.trim()}"`);
                }
            }

            return found;
        }
        """;

    /// <summary>Every check page's address after its runtime segment, read from the pages' own routes.</summary>
    public static TheoryData<string> Pages { get; } = [.. ComponentChecks.ReadPagePaths()];

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task Should_render_its_state_with_no_error_no_wait_on_a_script_and_no_enabled_control_that_needs_interactivity(string path)
    {
        // Arrange
        var (context, page) = await host.OpenPageAsync(host.ComponentsAddress);
        await using var _ = context;
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);
        page.Console += (_, message) =>
        {
            if (message.Type == "error")
            {
                errors.Add(message.Text);
            }
        };
        page.Response += (_, response) =>
        {
            if (response.Status >= 400)
            {
                errors.Add($"{response.Status} {response.Url}");
            }
        };

        // Act
        var response = await page.GotoAsync($"static/{path}");

        // Assert
        Assert.Equal(200, response!.Status);
        await Assertions.Expect(page.Locator("#runtime")).ToHaveTextAsync("Static False");
        var controls = await page.EvaluateAsync<string[]>(FindControlsNeedingInteractivity);
        var waits = await page.EvaluateAsync<string[]>(FindWaits);
        Assert.Multiple(
            () => Assert.True(errors.Count == 0, $"The page raised:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}"),
            () => Assert.True(controls.Length == 0, $"The page offers controls that need interactivity:{Environment.NewLine}{string.Join(Environment.NewLine, controls)}"),
            () => Assert.True(waits.Length == 0, $"The page shows what waits on a script that never runs:{Environment.NewLine}{string.Join(Environment.NewLine, waits)}"));
    }
}
