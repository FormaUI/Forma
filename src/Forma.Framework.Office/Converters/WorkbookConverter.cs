namespace Forma.Framework.Office.Converters;

using System.Net;
using System.Text;

using Forma.Framework.Localization;

using OfficeIMO.Excel;

/// <summary>Projects a workbook's used range into one table per sheet, since OfficeIMO ships no HTML converter for it.</summary>
internal sealed class WorkbookConverter(IStringLocale localizer) : IOfficeConverter
{
    // A sheet addresses a million rows, so a runaway used range would otherwise produce unrenderable markup.
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

        // The used range's first row heads the table, which is what a sheet's own layout almost always means.
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
                .Append(WebUtility.HtmlEncode(localizer[$"Showing the first {rows} rows and {columns} columns."].Value))
                .Append("</td></tr>");
        }

        markup.Append("</tbody></table></section>");
    }
}
