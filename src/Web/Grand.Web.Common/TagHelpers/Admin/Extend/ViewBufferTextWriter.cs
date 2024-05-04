// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Html;
using System.Text.Encodings.Web;

namespace Grand.Web.Common.TagHelpers.Admin.Extend;

internal class ViewBufferTextWriter : TextWriter
{
    private readonly HtmlEncoder _htmlEncoder;
    private readonly TextWriter _inner;

    /// <summary>
    ///     Creates a new instance of <see cref="ViewBufferTextWriter" />.
    /// </summary>
    /// <param name="buffer">The <see cref="ViewBuffer" /> for buffered output.</param>
    /// <param name="encoding">The <see cref="System.Text.Encoding" />.</param>
    public ViewBufferTextWriter(ViewBuffer buffer, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(encoding);

        Buffer = buffer;
        Encoding = encoding;
    }

    /// <summary>
    ///     Creates a new instance of <see cref="ViewBufferTextWriter" />.
    /// </summary>
    /// <param name="buffer">The <see cref="ViewBuffer" /> for buffered output.</param>
    /// <param name="encoding">The <see cref="System.Text.Encoding" />.</param>
    /// <param name="htmlEncoder">The HTML encoder.</param>
    /// <param name="inner">
    ///     The inner <see cref="TextWriter" /> to write output to when this instance is no longer buffering.
    /// </param>
    public ViewBufferTextWriter(ViewBuffer buffer, Encoding encoding, HtmlEncoder htmlEncoder, TextWriter inner)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(encoding);
        ArgumentNullException.ThrowIfNull(htmlEncoder);
        ArgumentNullException.ThrowIfNull(inner);

        Buffer = buffer;
        Encoding = encoding;
        _htmlEncoder = htmlEncoder;
        _inner = inner;
    }

    /// <inheritdoc />
    public override Encoding Encoding { get; }

    /// <inheritdoc />
    public bool IsBuffering { get; private set; } = true;

    /// <summary>
    ///     Gets the <see cref="ViewBuffer" />.
    /// </summary>
    public ViewBuffer Buffer { get; }

    /// <inheritdoc />
    public override void Write(char value)
    {
        if (IsBuffering)
            Buffer.AppendHtml(value.ToString());
        else
            _inner.Write(value);
    }

    /// <inheritdoc />
    public override void Write(char[] buffer, int index, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (index < 0 || index >= buffer.Length) throw new ArgumentOutOfRangeException(nameof(index));

        if (count < 0 || buffer.Length - index < count) throw new ArgumentOutOfRangeException(nameof(count));

        if (IsBuffering)
            Buffer.AppendHtml(new string(buffer, index, count));
        else
            _inner.Write(buffer, index, count);
    }

    /// <inheritdoc />
    public override void Write(string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        if (IsBuffering)
            Buffer.AppendHtml(value);
        else
            _inner.Write(value);
    }

    /// <inheritdoc />
    public override void Write(object value)
    {
        if (value == null) return;

        if (value is IHtmlContentContainer container)
            Write(container);
        else if (value is IHtmlContent htmlContent)
            Write(htmlContent);
        else
            Write(value.ToString());
    }

    /// <summary>
    ///     Writes an <see cref="IHtmlContent" /> value.
    /// </summary>
    /// <param name="value">The <see cref="IHtmlContent" /> value.</param>
    public void Write(IHtmlContent value)
    {
        if (value == null) return;

        if (IsBuffering)
            Buffer.AppendHtml(value);
        else
            value.WriteTo(_inner, _htmlEncoder);
    }

    /// <summary>
    ///     Writes an <see cref="IHtmlContentContainer" /> value.
    /// </summary>
    /// <param name="value">The <see cref="IHtmlContentContainer" /> value.</param>
    public void Write(IHtmlContentContainer value)
    {
        if (value == null) return;

        if (IsBuffering)
            value.MoveTo(Buffer);
        else
            value.WriteTo(_inner, _htmlEncoder);
    }

    /// <inheritdoc />
    public override void WriteLine(object value)
    {
        if (value == null) return;

        if (value is IHtmlContentContainer container)
        {
            Write(container);
            Write(NewLine);
        }
        else if (value is IHtmlContent htmlContent)
        {
            Write(htmlContent);
            Write(NewLine);
        }
        else
        {
            Write(value.ToString());
            Write(NewLine);
        }
    }

    /// <inheritdoc />
    public override Task WriteAsync(char value)
    {
        if (IsBuffering)
        {
            Buffer.AppendHtml(value.ToString());
            return Task.CompletedTask;
        }

        return _inner.WriteAsync(value);
    }

    /// <inheritdoc />
    public override Task WriteAsync(char[] buffer, int index, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        if (count < 0 || buffer.Length - index < count) throw new ArgumentOutOfRangeException(nameof(count));

        if (IsBuffering)
        {
            Buffer.AppendHtml(new string(buffer, index, count));
            return Task.CompletedTask;
        }

        return _inner.WriteAsync(buffer, index, count);
    }

    /// <inheritdoc />
    public override Task WriteAsync(string value)
    {
        if (IsBuffering)
        {
            Buffer.AppendHtml(value);
            return Task.CompletedTask;
        }

        return _inner.WriteAsync(value);
    }

    /// <inheritdoc />
    public override void WriteLine()
    {
        if (IsBuffering)
            Buffer.AppendHtml(NewLine);
        else
            _inner.WriteLine();
    }

    /// <inheritdoc />
    public override void WriteLine(string value)
    {
        if (IsBuffering)
        {
            Buffer.AppendHtml(value);
            Buffer.AppendHtml(NewLine);
        }
        else
        {
            _inner.WriteLine(value);
        }
    }

    /// <inheritdoc />
    public override Task WriteLineAsync(char value)
    {
        if (IsBuffering)
        {
            Buffer.AppendHtml(value.ToString());
            Buffer.AppendHtml(NewLine);
            return Task.CompletedTask;
        }

        return _inner.WriteLineAsync(value);
    }

    /// <inheritdoc />
    public override Task WriteLineAsync(char[] value, int start, int offset)
    {
        if (IsBuffering)
        {
            Buffer.AppendHtml(new string(value, start, offset));
            Buffer.AppendHtml(NewLine);
            return Task.CompletedTask;
        }

        return _inner.WriteLineAsync(value, start, offset);
    }

    /// <inheritdoc />
    public override Task WriteLineAsync(string value)
    {
        if (IsBuffering)
        {
            Buffer.AppendHtml(value);
            Buffer.AppendHtml(NewLine);
            return Task.CompletedTask;
        }

        return _inner.WriteLineAsync(value);
    }

    /// <inheritdoc />
    public override Task WriteLineAsync()
    {
        if (IsBuffering)
        {
            Buffer.AppendHtml(NewLine);
            return Task.CompletedTask;
        }

        return _inner.WriteLineAsync();
    }

    /// <summary>
    ///     Copies the buffered content to the unbuffered writer and invokes flush on it.
    ///     Additionally causes this instance to no longer buffer and direct all write operations
    ///     to the unbuffered writer.
    /// </summary>
    public override void Flush()
    {
        if (_inner == null || _inner is ViewBufferTextWriter) return;

        if (IsBuffering)
        {
            IsBuffering = false;
            Buffer.WriteTo(_inner, _htmlEncoder);
            Buffer.Clear();
        }

        _inner.Flush();
    }

    /// <summary>
    ///     Copies the buffered content to the unbuffered writer and invokes flush on it.
    ///     Additionally causes this instance to no longer buffer and direct all write operations
    ///     to the unbuffered writer.
    /// </summary>
    /// <returns>A <see cref="Task" /> that represents the asynchronous copy and flush operations.</returns>
    public override async Task FlushAsync()
    {
        if (_inner == null || _inner is ViewBufferTextWriter) return;

        if (IsBuffering)
        {
            IsBuffering = false;
            await Buffer.WriteToAsync(_inner, _htmlEncoder);
            Buffer.Clear();
        }

        await _inner.FlushAsync();
    }
}