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
    private const string MissingGlyph = "?";

    //A4 in points
    private const double PageWidth = 595;
    private const double PageHeight = 842;

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
        _footerText = footer;
        _footer = string.IsNullOrWhiteSpace(footer) ? [] : Wrap(footer, FooterSize, _regular, ContentWidth);
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
        foreach (var line in Wrap(text, size, font, ContentWidth))
        {
            EnsureSpace(size * LineSpacing);
            DrawLine(line, size, font, Margin, _y, ContentWidth, align, gray);
            _y -= size * LineSpacing;
        }
    }

    public void HorizontalRule(byte gray = 0, double width = 0.5)
    {
        _page.SetStrokeColor(gray, gray, gray);
        _page.DrawLine(new PdfPoint(Margin, _y), new PdfPoint(PageWidth - Margin, _y), width);
        _page.ResetColor();
    }

    /// <summary>
    ///     Draws a PNG at the current position without moving the cursor, scaled down to fit the box.
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
    }

    /// <summary>
    ///     Draws a table. Column widths are relative weights of the content width. A row that does not fit on
    ///     the page moves to the next one, repeating the header.
    /// </summary>
    public void Table(IReadOnlyList<PdfColumn> columns, IReadOnlyList<string> header,
        IEnumerable<IReadOnlyList<PdfCell>> rows, double size = 10, bool rowLines = true)
    {
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
                foreach (var line in cell.Lines)
                {
                    var lineSize = line.Small ? size - 2 : size;
                    var font = line.Bold ? _bold : _regular;
                    cells[i].AddRange(Wrap(line.Text, lineSize, font, widths[i] - 2 * CellPadding)
                        .Select(text => (text, lineSize, font)));
                }

                height = Math.Max(height, cells[i].Sum(l => l.Size * LineSpacing));
            }

            height += 2 * CellPadding;
            if (_y - height < Bottom)
            {
                NewPage(_footerText);
                if (header != null) DrawHeader(columns, widths, header, size);
            }

            var x = Margin;
            for (var i = 0; i < columns.Count; i++)
            {
                var top = _y - CellPadding;
                foreach (var (text, lineSize, font) in cells[i])
                {
                    DrawLine(text, lineSize, font, x + CellPadding, top, widths[i] - 2 * CellPadding,
                        columns[i].Align, 0);
                    top -= lineSize * LineSpacing;
                }

                x += widths[i];
            }

            _y -= height;
            if (rowLines) HorizontalRule(220);
        }
    }

    public byte[] Build()
    {
        return _builder.Build();
    }

    private void DrawHeader(IReadOnlyList<PdfColumn> columns, double[] widths, IReadOnlyList<string> header,
        double size)
    {
        var height = size * LineSpacing + 2 * CellPadding;
        EnsureSpace(height);
        _page.SetStrokeColor(128, 128, 128);
        _page.SetTextAndFillColor(128, 128, 128);
        _page.DrawRectangle(new PdfPoint(Margin, _y - height), ContentWidth, height, 0.5, true);
        _page.ResetColor();

        var x = Margin;
        for (var i = 0; i < columns.Count; i++)
        {
            var text = i < header.Count ? header[i] : string.Empty;
            var line = Wrap(text, size, _bold, widths[i] - 2 * CellPadding).FirstOrDefault();
            DrawLine(line, size, _bold, x + CellPadding, _y - CellPadding, widths[i] - 2 * CellPadding,
                columns[i].Align, 255);
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

    private List<string> Wrap(string text, double size, PdfDocumentBuilder.AddedFont font, double width)
    {
        var lines = new List<string>();
        if (string.IsNullOrEmpty(text)) return lines;

        foreach (var paragraph in Supported(text, font).Split('\n'))
        {
            var line = string.Empty;
            foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = line.Length == 0 ? word : line + " " + word;
                if (Measure(candidate, size, font) <= width)
                {
                    line = candidate;
                    continue;
                }

                if (line.Length > 0) lines.Add(line);
                line = word;
                //a single word wider than the column is broken between characters
                while (line.Length > 1 && Measure(line, size, font) > width)
                {
                    var cut = line.Length - 1;
                    while (cut > 1 && Measure(line[..cut], size, font) > width) cut--;
                    lines.Add(line[..cut]);
                    line = line[cut..];
                }
            }

            lines.Add(line);
        }

        return lines;
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
