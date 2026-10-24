using System.Reflection;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Writer;

namespace Grand.Business.Common.Services.Pdf;

/// <summary>
///     Lays out text, tables and images top-down on A4 pages. PdfPig only writes PDF objects at absolute
///     positions; wrapping, columns and page breaks are done here.
/// </summary>
internal sealed class PdfComposer : IDisposable
{
    private const double Margin = 40;
    private const double LineSpacing = 1.3;
    private const double CellPadding = 4;
    private const double FooterSize = 8;
    private const double ImageGap = 10;
    private const string MissingGlyph = "?";

    //A4 in points
    private const double PageWidth = 595;
    private const double PageHeight = 842;

    //the footer is cut to a quarter of the page, so a long footer cannot leave no room for the content
    private static readonly int MaxFooterLines = (int)((PageHeight - 2 * Margin) / 4 / (FooterSize * LineSpacing));

    private static readonly Lazy<byte[]> RegularFontData = new(() => ReadFont("NotoSans-Regular.ttf"));
    private static readonly Lazy<byte[]> BoldFontData = new(() => ReadFont("NotoSans-Bold.ttf"));

    private readonly PdfDocumentBuilder _builder = new();
    private readonly PdfDocumentBuilder.AddedFont _regular;
    private readonly PdfDocumentBuilder.AddedFont _bold;
    private readonly Dictionary<(bool Bold, int Rune), bool> _glyphs = new();
    private readonly Dictionary<byte[], PdfPageBuilder.AddedImage> _images = new(ReferenceEqualityComparer.Instance);

    private PdfPageBuilder _page;
    private double _y;
    private string _footerText;
    private IReadOnlyList<string> _footer = [];

    //a right-aligned image narrows the text beside it down to this y
    private double _asideBottom = double.MaxValue;
    private double _asideWidth;

    public PdfComposer()
    {
        _regular = _builder.AddTrueTypeFont(RegularFontData.Value);
        _bold = _builder.AddTrueTypeFont(BoldFontData.Value);
    }

    public double ContentWidth => PageWidth - 2 * Margin;

    private double Bottom => Margin + (_footer.Count == 0 ? 0 : _footer.Count * FooterSize * LineSpacing + 8);

    public void Dispose()
    {
        _builder.Dispose();
    }

    /// <summary>
    ///     Starts a new page. The footer is repeated on the pages a table or text overflows to.
    /// </summary>
    public void NewPage(string footer = null)
    {
        _page = _builder.AddPage(PageWidth, PageHeight);
        _y = PageHeight - Margin;
        _asideBottom = double.MaxValue;
        _footerText = footer;
        _footer = string.IsNullOrWhiteSpace(footer)
            ? []
            : Wrap(footer, FooterSize, _regular, _ => ContentWidth).Take(MaxFooterLines).ToList();
        DrawFooter();
    }

    public void Space(double height)
    {
        _y -= height;
    }

    public void Text(string text, double size = 10, bool bold = false, PdfAlign align = PdfAlign.Left,
        byte gray = 0)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var font = bold ? _bold : _regular;
        var lineHeight = size * LineSpacing;
        //lines that start above the bottom of a right-aligned image are wrapped short of it
        var besideImage = _y > _asideBottom ? (int)Math.Ceiling((_y - _asideBottom) / lineHeight) : 0;
        foreach (var line in Wrap(text, size, font,
                     i => i < besideImage ? ContentWidth - _asideWidth : ContentWidth))
        {
            EnsureSpace(lineHeight);
            DrawLine(line, size, font, Margin, _y, ContentWidth, align, gray);
            _y -= lineHeight;
        }
    }

    public void HorizontalRule(byte gray = 0, double width = 0.5)
    {
        ClearImage();
        _page.SetStrokeColor(gray, gray, gray);
        _page.DrawLine(new PdfPoint(Margin, _y), new PdfPoint(PageWidth - Margin, _y), width);
        _page.ResetColor();
    }

    /// <summary>
    ///     Draws a PNG at the current position without moving the cursor, scaled down to fit the box. Text that
    ///     follows a right-aligned image wraps beside it until <see cref="ClearImage" />, a rule or a table.
    /// </summary>
    public void Image(byte[] png, double maxWidth, double maxHeight, PdfAlign align)
    {
        if (!TryReadPngSize(png, out var width, out var height)) return;

        var scale = Math.Min(1, Math.Min(maxWidth / width, maxHeight / height));
        var w = width * scale;
        var h = height * scale;
        var x = align == PdfAlign.Right ? PageWidth - Margin - w : Margin;
        var rectangle = new PdfRectangle(x, _y - h, x + w, _y);
        //the image is written into the document once and every later page refers to it
        if (_images.TryGetValue(png, out var added))
            _page.AddImage(added, rectangle);
        else
            _images[png] = _page.AddPng(png, rectangle);

        if (align != PdfAlign.Right) return;
        _asideBottom = _y - h;
        _asideWidth = w + ImageGap;
    }

    /// <summary>
    ///     Moves the cursor below the image drawn beside the text, if it is not already there.
    /// </summary>
    public void ClearImage()
    {
        if (_asideBottom == double.MaxValue) return;
        _y = Math.Min(_y, _asideBottom);
        _asideBottom = double.MaxValue;
    }

    /// <summary>
    ///     Draws a table. Column widths are relative weights of the content width. A row that does not fit on
    ///     the page moves to the next one, repeating the header; a row taller than a whole page is split
    ///     between its lines.
    /// </summary>
    public void Table(IReadOnlyList<PdfColumn> columns, IReadOnlyList<string> header,
        IEnumerable<IReadOnlyList<PdfCell>> rows, double size = 10, bool rowLines = true)
    {
        ClearImage();
        var totalWeight = columns.Sum(c => c.Weight);
        var widths = columns.Select(c => ContentWidth * c.Weight / totalWeight).ToArray();

        if (header != null) DrawHeader(columns, widths, header, size);

        foreach (var row in rows)
        {
            var cells = new List<(string Text, double Size, PdfDocumentBuilder.AddedFont Font)>[columns.Count];
            var height = 0d;
            for (var i = 0; i < columns.Count; i++)
            {
                cells[i] = [];
                var cell = i < row.Count ? row[i] : PdfCell.Empty;
                var width = widths[i] - 2 * CellPadding;
                foreach (var line in cell.Lines)
                {
                    var lineSize = line.Small ? size - 2 : size;
                    var font = line.Bold ? _bold : _regular;
                    cells[i].AddRange(Wrap(line.Text, lineSize, font, _ => width)
                        .Select(text => (text, lineSize, font)));
                }

                height = Math.Max(height, cells[i].Sum(l => l.Size * LineSpacing));
            }

            height += 2 * CellPadding;
            //a row that fits on an empty page is kept whole
            var pageRoom = PageHeight - Margin - Bottom - HeaderHeight(header, size);
            if (_y - height < Bottom && height <= pageRoom)
                NewTablePage(columns, widths, header, size);

            var next = new int[columns.Count];
            while (true)
            {
                var room = _y - 2 * CellPadding - Bottom;
                var used = 0d;
                var done = true;
                var x = Margin;
                for (var i = 0; i < columns.Count; i++)
                {
                    var top = _y - CellPadding;
                    var cellHeight = 0d;
                    while (next[i] < cells[i].Count)
                    {
                        var (text, lineSize, font) = cells[i][next[i]];
                        var lineHeight = lineSize * LineSpacing;
                        if (cellHeight + lineHeight > room) break;

                        DrawLine(text, lineSize, font, x + CellPadding, top, widths[i] - 2 * CellPadding,
                            columns[i].Align, 0);
                        top -= lineHeight;
                        cellHeight += lineHeight;
                        next[i]++;
                    }

                    done &= next[i] == cells[i].Count;
                    used = Math.Max(used, cellHeight);
                    x += widths[i];
                }

                if (done)
                {
                    _y -= used + 2 * CellPadding;
                    break;
                }

                //not even one line fits at the top of a page: stop rather than add pages forever
                if (used == 0 && room >= pageRoom - 2 * CellPadding) break;

                NewTablePage(columns, widths, header, size);
            }

            if (rowLines) HorizontalRule(220);
        }
    }

    public byte[] Build()
    {
        //a PDF needs at least one page, also when there was nothing to print
        if (_page == null) NewPage();
        return _builder.Build();
    }

    /// <summary>
    ///     Checks that PdfPig can embed the PNG, so a file its decoder rejects is found before the document is
    ///     laid out instead of failing it half way.
    /// </summary>
    public static bool CanEmbedPng(byte[] png)
    {
        if (!TryReadPngSize(png, out _, out _)) return false;
        try
        {
            using var builder = new PdfDocumentBuilder();
            builder.AddPage(PageWidth, PageHeight).AddPng(png, new PdfRectangle(0, 0, 10, 10));
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void NewTablePage(IReadOnlyList<PdfColumn> columns, double[] widths, IReadOnlyList<string> header,
        double size)
    {
        NewPage(_footerText);
        if (header != null) DrawHeader(columns, widths, header, size);
    }

    private static double HeaderHeight(IReadOnlyList<string> header, double size)
    {
        return header == null ? 0 : size * LineSpacing + 2 * CellPadding;
    }

    private void DrawHeader(IReadOnlyList<PdfColumn> columns, double[] widths, IReadOnlyList<string> header,
        double size)
    {
        var height = HeaderHeight(header, size);
        EnsureSpace(height);
        _page.SetStrokeColor(128, 128, 128);
        _page.SetTextAndFillColor(128, 128, 128);
        _page.DrawRectangle(new PdfPoint(Margin, _y - height), ContentWidth, height, 0.5, true);
        _page.ResetColor();

        var x = Margin;
        for (var i = 0; i < columns.Count; i++)
        {
            var text = i < header.Count ? header[i] : string.Empty;
            var width = widths[i] - 2 * CellPadding;
            var line = Wrap(text, size, _bold, _ => width).FirstOrDefault();
            DrawLine(line, size, _bold, x + CellPadding, _y - CellPadding, width, columns[i].Align, 255);
            x += widths[i];
        }

        _y -= height;
    }

    private void DrawFooter()
    {
        var top = Margin + _footer.Count * FooterSize * LineSpacing;
        foreach (var line in _footer)
        {
            DrawLine(line, FooterSize, _regular, Margin, top, ContentWidth, PdfAlign.Left, 90);
            top -= FooterSize * LineSpacing;
        }
    }

    private void EnsureSpace(double height)
    {
        if (_y - height < Bottom) NewPage(_footerText);
    }

    /// <summary>
    ///     Draws one already wrapped line with its top at <paramref name="top" />.
    /// </summary>
    private void DrawLine(string text, double size, PdfDocumentBuilder.AddedFont font, double x, double top,
        double width, PdfAlign align, byte gray)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (align == PdfAlign.Right) x += width - Measure(text, size, font);
        if (gray != 0) _page.SetTextAndFillColor(gray, gray, gray);
        _page.AddText(text, size, new PdfPoint(x, top - size), font);
        if (gray != 0) _page.ResetColor();
    }

    /// <summary>
    ///     Breaks text into lines, <paramref name="width" /> giving the width of the n-th line. Each word is
    ///     measured once and a line is as wide as its words and the spaces between them.
    /// </summary>
    private List<string> Wrap(string text, double size, PdfDocumentBuilder.AddedFont font, Func<int, double> width)
    {
        var lines = new List<string>();
        if (string.IsNullOrEmpty(text)) return lines;

        var space = Measure(" ", size, font);
        foreach (var paragraph in Supported(text, font).Split('\n'))
        {
            var line = new StringBuilder();
            var lineWidth = 0d;
            foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var wordWidth = Measure(word, size, font);
                if (line.Length > 0 && lineWidth + space + wordWidth <= width(lines.Count))
                {
                    line.Append(' ').Append(word);
                    lineWidth += space + wordWidth;
                    continue;
                }

                if (line.Length > 0) lines.Add(line.ToString());
                var rest = word;
                //a single word wider than the column is broken between characters
                if (rest.Length > 1 && wordWidth > width(lines.Count))
                    (rest, wordWidth) = BreakWord(rest, size, font, width, lines);

                line.Clear().Append(rest);
                lineWidth = wordWidth;
            }

            lines.Add(line.ToString());
        }

        return lines;
    }

    /// <summary>
    ///     Adds the full-width pieces of a word wider than the column to <paramref name="lines" /> and returns the
    ///     last piece with its width. The word is measured once, so a very long token (a pasted text attribute,
    ///     a URL) costs linear time instead of a measurement per piece.
    /// </summary>
    private (string Tail, double Width) BreakWord(string word, double size, PdfDocumentBuilder.AddedFont font,
        Func<int, double> width, List<string> lines)
    {
        var letters = _page.MeasureText(word, size, new PdfPoint(0, 0), font);
        //one letter per rune; charEnds[i] is where rune i ends in the string
        var charEnds = new List<int>(word.Length);
        foreach (var rune in word.EnumerateRunes())
            charEnds.Add((charEnds.Count == 0 ? 0 : charEnds[^1]) + rune.Utf16SequenceLength);
        if (letters.Count != charEnds.Count) return (word, Measure(word, size, font));

        var start = 0;
        var offset = 0d;
        while (letters.Count - start > 1 && letters[^1].EndBaseLine.X - offset > width(lines.Count))
        {
            var limit = width(lines.Count);
            var end = start + 1;
            while (end < letters.Count && letters[end].EndBaseLine.X - offset <= limit) end++;
            lines.Add(word[(start == 0 ? 0 : charEnds[start - 1])..charEnds[end - 1]]);
            offset = letters[end - 1].EndBaseLine.X;
            start = end;
        }

        return (word[(start == 0 ? 0 : charEnds[start - 1])..], letters[^1].EndBaseLine.X - offset);
    }

    private double Measure(string text, double size, PdfDocumentBuilder.AddedFont font)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var letters = _page.MeasureText(text, size, new PdfPoint(0, 0), font);
        return letters.Count == 0 ? 0 : letters[^1].EndBaseLine.X;
    }

    /// <summary>
    ///     PdfPig throws on a character the font has no glyph for, so such characters are replaced up front.
    /// </summary>
    private string Supported(string text, PdfDocumentBuilder.AddedFont font)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var rune in text.Replace("\r", string.Empty).Replace('\t', ' ').EnumerateRunes())
        {
            if (rune.Value is '\n' or ' ')
            {
                sb.Append((char)rune.Value);
                continue;
            }

            if (Rune.IsControl(rune)) continue;
            var key = (ReferenceEquals(font, _bold), rune.Value);
            if (!_glyphs.TryGetValue(key, out var supported))
            {
                try
                {
                    _page.MeasureText(rune.ToString(), 10, new PdfPoint(0, 0), font);
                    supported = true;
                }
                catch (InvalidOperationException)
                {
                    supported = false;
                }

                _glyphs[key] = supported;
            }

            sb.Append(supported ? rune.ToString() : MissingGlyph);
        }

        return sb.ToString();
    }

    private static bool TryReadPngSize(byte[] png, out int width, out int height)
    {
        width = height = 0;
        //8-byte signature, then the IHDR chunk: length, type, width, height (big-endian)
        if (png is not { Length: >= 24 } || png[0] != 0x89 || png[1] != 'P' || png[2] != 'N' || png[3] != 'G')
            return false;

        width = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
        height = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
        return width > 0 && height > 0;
    }

    private static byte[] ReadFont(string fileName)
    {
        using var stream = Assembly.GetExecutingAssembly()
                               .GetManifestResourceStream($"Grand.Business.Common.Pdf.Fonts.{fileName}")
                           ?? throw new InvalidOperationException($"Embedded font {fileName} is missing");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}

internal enum PdfAlign
{
    Left,
    Right
}

internal sealed record PdfColumn(double Weight, PdfAlign Align = PdfAlign.Left);

internal sealed record PdfLine(string Text, bool Bold = false, bool Small = false);

internal sealed record PdfCell(IReadOnlyList<PdfLine> Lines)
{
    public static readonly PdfCell Empty = new([]);

    public static PdfCell Of(string text, bool bold = false)
    {
        return new PdfCell([new PdfLine(text ?? string.Empty, bold)]);
    }
}
