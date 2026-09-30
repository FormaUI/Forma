namespace Forma.Framework.Resources.Office.Converters;

using System.Net;
using System.Text;

using Forma.Framework.Localization;

using OfficeIMO.Excel;

/// <summary>Converts an Excel workbook into markup, one table per sheet over its used range.</summary>
internal sealed class WorkbookConverter(IStringLocale locale) : IOfficeConverter
{
    // The limits hold a runaway used range to markup a viewer can render.
    private const int MaxColumns = 50;
    private const int MaxRows = 500;

    /// <inheritdoc />
    public string Extension
        => ".xlsx";

    /// <inheritdoc />
    public async ValueTask<string> ConvertAsync(Stream content, CancellationToken cancellationToken)
    {
        using var workbook = await ExcelDocument.LoadAsync(content, cancellationToken: cancellationToken).ConfigureAwait(false);

        var markup = new StringBuilder();

        foreach (var sheet in workbook.Sheets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AppendSheet(markup, sheet);
        }

        return markup.ToString();
    }

    private static void AppendRow(StringBuilder markup, ExcelSheet sheet, int row, int firstColumn, int columnLimit, bool heading)
    {
        markup.Append("<tr>");

        for (var column = firstColumn; column <= columnLimit; column++)
        {
            var cell = sheet.TryGetCellText(row, column, out var text) ? text : string.Empty;

            markup.Append(heading ? "<th>" : "<td>")
                .Append(WebUtility.HtmlEncode(cell))
                .Append(heading ? "</th>" : "</td>");
        }

        markup.Append("</tr>");
    }

    private void AppendSheet(StringBuilder markup, ExcelSheet sheet)
    {
        markup.Append("<section><h2>").Append(WebUtility.HtmlEncode(sheet.Name)).Append("</h2>");

        var usedRange = sheet.UsedRangeA1;

        if (string.IsNullOrWhiteSpace(usedRange))
        {
            markup.Append("</section>");

            return;
        }

        var (firstRow, firstColumn, lastRow, lastColumn) = sheet.GetRangeBounds(usedRange);
        var rowLimit = Math.Min(lastRow, firstRow + MaxRows - 1);
        var columnLimit = Math.Min(lastColumn, firstColumn + MaxColumns - 1);

        markup.Append("<table><thead>");
        AppendRow(markup, sheet, firstRow, firstColumn, columnLimit, heading: true);
        markup.Append("</thead><tbody>");

        for (var row = firstRow + 1; row <= rowLimit; row++)
        {
            AppendRow(markup, sheet, row, firstColumn, columnLimit, heading: false);
        }

        if (lastRow > rowLimit || lastColumn > columnLimit)
        {
            var rows = rowLimit - firstRow + 1;
            var columns = columnLimit - firstColumn + 1;

            markup.Append("<tr><td>")
                .Append(WebUtility.HtmlEncode(locale["Showing the first {Rows} rows and {Columns} columns.", rows, columns].Value))
                .Append("</td></tr>");
        }

        markup.Append("</tbody></table></section>");
    }
}
