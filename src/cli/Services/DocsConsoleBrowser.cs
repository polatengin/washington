using System.Text;
using System.Text.RegularExpressions;

namespace Washington.Services;

internal sealed class DocsConsoleBrowser
{
    private const string AltBufferOff = "\u001b[?1049l";
    private const string AltBufferOn = "\u001b[?1049h";
    private const string Bold = "\u001b[1m";
    private const string Clear = "\u001b[2J";
    private const string Dim = "\u001b[2m";
    private const string HideCursor = "\u001b[?25l";
    private const string Home = "\u001b[H";
    private const string Invert = "\u001b[7m";
    private const string Reset = "\u001b[0m";
    private const string ShowCursor = "\u001b[?25h";

    private readonly DocsClient _client;
    private readonly IReadOnlyList<DocsDocument> _documents;
    private readonly TextWriter _outputWriter;
    private readonly Dictionary<string, string> _pageCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<string>> _wrappedPageCache = new(StringComparer.OrdinalIgnoreCase);

    private int _pageScroll;
    private int _selectedIndex;
    private DocsView _view = DocsView.Browse;

    public DocsConsoleBrowser(DocsClient client, IReadOnlyList<DocsDocument> documents, TextWriter outputWriter)
    {
        _client = client;
        _outputWriter = outputWriter;
        _documents = documents
            .OrderBy(static document => document.Href == "/" ? 0 : 1)
            .ThenBy(static document => document.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static document => document.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static document => document.Href, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var introductionIndex = Array.FindIndex(_documents.ToArray(), static document => document.Href == "/");
        _selectedIndex = introductionIndex >= 0 ? introductionIndex : 0;
    }

    public async Task<int> RunAsync()
    {
        await _outputWriter.WriteAsync($"{AltBufferOn}{HideCursor}");
        await _outputWriter.FlushAsync();

        try
        {
            await RenderAsync();

            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                if (IsQuitKey(key))
                {
                    return 0;
                }

                if (_view == DocsView.Browse)
                {
                    HandleBrowseKey(key);
                }
                else
                {
                    HandlePageKey(key);
                }

                await RenderAsync();
            }
        }
        finally
        {
            await _outputWriter.WriteAsync($"{Reset}{ShowCursor}{AltBufferOff}");
            await _outputWriter.FlushAsync();
        }
    }

    private DocsDocument SelectedDocument => _documents[_selectedIndex];

    private static bool IsQuitKey(ConsoleKeyInfo key)
        => key.Key == ConsoleKey.Q || key.Key == ConsoleKey.Escape && key.Modifiers == ConsoleModifiers.Control;

    private static int GetWindowWidth()
    {
        try
        {
            return Math.Max(Console.WindowWidth, 60);
        }
        catch
        {
            return 100;
        }
    }

    private static int GetWindowHeight()
    {
        try
        {
            return Math.Max(Console.WindowHeight, 12);
        }
        catch
        {
            return 30;
        }
    }

    private void HandleBrowseKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.DownArrow:
                _selectedIndex = Math.Min(_documents.Count - 1, _selectedIndex + 1);
                break;
            case ConsoleKey.UpArrow:
                _selectedIndex = Math.Max(0, _selectedIndex - 1);
                break;
            case ConsoleKey.Enter:
                _view = DocsView.Page;
                _pageScroll = 0;
                break;
        }
    }

    private void HandlePageKey(ConsoleKeyInfo key)
    {
        var height = GetWindowHeight();
        var visibleRows = Math.Max(1, height - 2);

        switch (key.Key)
        {
            case ConsoleKey.Escape:
                _view = DocsView.Browse;
                _pageScroll = 0;
                return;
            case ConsoleKey.DownArrow:
                _pageScroll += 1;
                break;
            case ConsoleKey.UpArrow:
                _pageScroll = Math.Max(0, _pageScroll - 1);
                break;
            case ConsoleKey.PageDown:
                _pageScroll += visibleRows;
                break;
            case ConsoleKey.PageUp:
                _pageScroll = Math.Max(0, _pageScroll - visibleRows);
                break;
        }
    }

    private async Task RenderAsync()
    {
        var columns = GetWindowWidth();
        var rows = GetWindowHeight();
        var buffer = _view == DocsView.Browse
            ? await RenderBrowseAsync(columns, rows)
            : await RenderPageAsync(columns, rows);

        await _outputWriter.WriteAsync($"{Home}{Clear}{buffer}");
        await _outputWriter.FlushAsync();
    }

    private async Task<string> RenderBrowseAsync(int columns, int rows)
    {
        var selected = SelectedDocument;
        var navWidth = Math.Clamp(columns / 3, 26, 42);
        var previewWidth = Math.Max(18, columns - navWidth - 3);
        var bodyRows = Math.Max(1, rows - 2);
        var browseStart = Math.Clamp(_selectedIndex - (bodyRows / 2), 0, Math.Max(0, _documents.Count - bodyRows));
        var previewLines = (await GetPageLinesAsync(selected.Href, previewWidth)).Take(bodyRows).ToArray();
        var builder = new StringBuilder();

        for (var row = 0; row < bodyRows; row++)
        {
            var docIndex = browseStart + row;
            var left = docIndex < _documents.Count
                ? FormatBrowseEntry(_documents[docIndex], docIndex == _selectedIndex, navWidth)
                : new string(' ', navWidth);
            var right = row < previewLines.Length
                ? DocsConsoleText.PadOrClip(previewLines[row], previewWidth)
                : new string(' ', previewWidth);

            builder.Append(left);
            builder.Append(' ');
            builder.Append(Dim);
            builder.Append('|');
            builder.Append(Reset);
            builder.Append(' ');
            builder.Append(right);

            if (row < bodyRows - 1)
            {
                builder.AppendLine();
            }
        }

        builder.AppendLine();
        builder.AppendLine();

        builder.AppendLine(DocsConsoleText.PadOrClip(
            $"{Bold}Bicep Cost Estimator docs{Reset}  |  Up/Down move  |  Enter open  |  q quit",
            columns));


        return builder.ToString();
    }

    private async Task<string> RenderPageAsync(int columns, int rows)
    {
        var selected = SelectedDocument;
        var pageLines = await GetPageLinesAsync(selected.Href, columns);
        var bodyRows = Math.Max(1, rows - 2);
        var maxScroll = Math.Max(0, pageLines.Count - bodyRows);
        _pageScroll = Math.Clamp(_pageScroll, 0, maxScroll);

        var builder = new StringBuilder();
        builder.AppendLine(DocsConsoleText.PadOrClip(
            $"{Bold}{selected.Title}{Reset}  {selected.Href}  Esc back  q quit",
            columns));

        for (var row = 0; row < bodyRows; row++)
        {
            var pageIndex = _pageScroll + row;
            if (pageIndex < pageLines.Count)
            {
                builder.Append(pageLines[pageIndex]);
            }

            if (row < bodyRows - 1)
            {
                builder.AppendLine();
            }
        }

        builder.AppendLine();
        builder.Append(DocsConsoleText.PadOrClip(
            $"Up/Down scroll  PageUp/PageDown move faster  Esc back  lines {_pageScroll + 1}-{Math.Min(pageLines.Count, _pageScroll + bodyRows)} of {pageLines.Count}",
            columns));

        return builder.ToString();
    }

    private async Task<IReadOnlyList<string>> GetPageLinesAsync(string route, int width)
    {
        var cacheKey = $"{route}:{width}:{ShouldRenderDocColors()}";
        if (_wrappedPageCache.TryGetValue(cacheKey, out var cachedLines))
        {
            return cachedLines;
        }

        if (!_pageCache.TryGetValue(route, out var rawPage))
        {
            rawPage = await _client.GetPageAsync(route) ?? $"No docs page found for {route}.";
            _pageCache[route] = rawPage;
        }

        var renderedPage = ShouldRenderDocColors() ? rawPage : DocsConsoleText.StripAnsi(rawPage);
        var lines = new List<string>();
        foreach (var line in renderedPage.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            lines.AddRange(DocsConsoleText.WrapPageLine(line, width));
        }

        while (lines.Count > 0 && string.IsNullOrWhiteSpace(DocsConsoleText.StripAnsi(lines[0])))
        {
            lines.RemoveAt(0);
        }

        var result = (IReadOnlyList<string>)(lines.Count == 0 ? new[] { string.Empty } : lines.ToArray());
        _wrappedPageCache[cacheKey] = result;
        return result;
    }

    private static string FormatBrowseEntry(DocsDocument document, bool selected, int width)
    {
        var label = document.Href == "/"
            ? document.Title
            : $"[{document.Category}] {document.Title}";
        var clipped = DocsConsoleText.PadOrClip(label, width);
        return selected ? $"{Invert}{clipped}{Reset}" : clipped;
    }

    private static bool ShouldRenderDocColors()
        => string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"));

    private enum DocsView
    {
        Browse,
        Page,
    }
}

internal static class DocsConsoleText
{
    private static readonly Regex AnsiPattern = new("\\x1B\\[[0-?]*[ -/]*[@-~]", RegexOptions.Compiled);
    private static readonly Regex AnsiSequencePattern = new("(\\x1B\\][\\s\\S]*?(?:\\x07|\\x1B\\\\)|\\x1B\\[[0-?]*[ -/]*[@-~])", RegexOptions.Compiled);
    private static readonly Regex OscPattern = new("\\x1B\\][\\s\\S]*?(?:\\x07|\\x1B\\\\)", RegexOptions.Compiled);

    internal static string StripAnsi(string text)
        => AnsiPattern.Replace(OscPattern.Replace(text, string.Empty), string.Empty);

    internal static string PadOrClip(string text, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        var length = VisibleLength(text);
        if (length <= width)
        {
            return text + new string(' ', width - length);
        }

        if (width <= 3)
        {
            return ClipVisibleText(text, width);
        }

        return ClipVisibleText(text, width - 3) + "...";
    }

    internal static List<string> WrapTextBlock(string text, int width)
    {
        var lines = new List<string>();
        foreach (var line in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            lines.AddRange(WrapPlainText(line, width));
        }

        return lines;
    }

    internal static IReadOnlyList<string> WrapPageLine(string line, int width)
    {
        if (width <= 0)
        {
            return new[] { string.Empty };
        }

        var sanitized = StripUnsupportedTerminalSequences(line).Replace("\t", "  ").TrimEnd();
        if (string.IsNullOrWhiteSpace(StripAnsi(sanitized)))
        {
            return new[] { string.Empty };
        }

        if (sanitized.Contains('\u001b') || ContainsBoxDrawing(sanitized))
        {
            return new[] { PadOrClip(sanitized, width) };
        }

        return WrapPlainText(sanitized, width);
    }

    private static bool ContainsBoxDrawing(string value)
        => value.IndexOfAny(new[] { '┌', '┐', '└', '┘', '│', '─' }) >= 0;

    private static int VisibleLength(string text) => StripAnsi(text).Length;

    private static string StripUnsupportedTerminalSequences(string text)
        => OscPattern.Replace(text, string.Empty);

    private static string ClipVisibleText(string text, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        var visible = 0;
        var result = new StringBuilder();
        var lastIndex = 0;
        var sawAnsi = false;

        foreach (Match match in AnsiSequencePattern.Matches(text))
        {
            var plainChunk = text[lastIndex..match.Index];
            foreach (var character in plainChunk)
            {
                if (visible >= width)
                {
                    return FinalizeClipped(result.ToString(), sawAnsi);
                }

                result.Append(character);
                visible++;
            }

            result.Append(match.Value);
            sawAnsi = true;
            lastIndex = match.Index + match.Length;
        }

        foreach (var character in text[lastIndex..])
        {
            if (visible >= width)
            {
                return FinalizeClipped(result.ToString(), sawAnsi);
            }

            result.Append(character);
            visible++;
        }

        return result.ToString();
    }

    private static string FinalizeClipped(string text, bool sawAnsi)
    {
        return sawAnsi && !text.EndsWith("\u001b[0m", StringComparison.Ordinal)
            ? text + "\u001b[0m"
            : text;
    }

    private static IReadOnlyList<string> WrapPlainText(string text, int width)
    {
        var normalized = text.Trim();
        if (normalized.Length == 0)
        {
            return new[] { string.Empty };
        }

        if (width <= 4)
        {
            return new[] { PadOrClip(normalized, width) };
        }

        var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var current = string.Empty;

        foreach (var word in words)
        {
            var next = current.Length == 0 ? word : $"{current} {word}";
            if (next.Length <= width)
            {
                current = next;
                continue;
            }

            if (current.Length > 0)
            {
                lines.Add(current);
            }

            if (word.Length <= width)
            {
                current = word;
                continue;
            }

            var remaining = word;
            while (remaining.Length > width)
            {
                lines.Add(remaining[..(width - 3)] + "...");
                remaining = remaining[(width - 3)..];
            }

            current = remaining;
        }

        if (current.Length > 0)
        {
            lines.Add(current);
        }

        return lines;
    }
}