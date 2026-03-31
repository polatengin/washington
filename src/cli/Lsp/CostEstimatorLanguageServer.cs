using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Washington.Cache;
using Washington.Mappers;
using Washington.Models;
using Washington.Services;

namespace Washington.Lsp;

public class CostEstimatorLanguageServer
{
    private readonly CostEstimationService _costService;
    private readonly Stream _input;
    private readonly Stream _output;
    private readonly ConcurrentDictionary<string, string> _openDocuments = new();
    private readonly ConcurrentDictionary<string, CostReport> _documentReports = new();
    private CancellationTokenSource? _debounceCts;
    private readonly int _debounceMs;
    private bool _shutdown;
    private string? _rootUri;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public CostEstimatorLanguageServer(Stream input, Stream output, int debounceMs = 300)
    {
        _input = input;
        _output = output;
        _debounceMs = debounceMs;

        var cache = new FilePricingCache();
        var compiler = new BicepCompiler();
        var extractor = new ResourceExtractor();
        var pricingClient = new PricingApiClient(cache);
        var mapperRegistry = new MapperRegistry();
        var aggregator = new CostAggregator(mapperRegistry, pricingClient);
        _costService = new CostEstimationService(compiler, extractor, aggregator);
    }

    public async Task RunAsync()
    {
        using var reader = new StreamReader(_input);

        while (!_shutdown)
        {
            var message = await ReadMessageAsync(reader);
            if (message == null)
                break;

            try
            {
                var request = JsonSerializer.Deserialize<LspRequest>(message, _jsonOptions);
                if (request?.Method != null)
                {
                    await HandleMessageAsync(request);
                }
            }
            catch (Exception ex)
            {
                await LogAsync($"Error processing message: {ex.Message}");
            }
        }
    }

    private async Task HandleMessageAsync(LspRequest request)
    {
        switch (request.Method)
        {
            case "initialize":
                await HandleInitializeAsync(request);
                break;
            case "initialized":
                // No-op, client is ready
                break;
            case "textDocument/didOpen":
                await HandleDidOpenAsync(request);
                break;
            case "textDocument/didSave":
                await HandleDidSaveAsync(request);
                break;
            case "textDocument/didChange":
                await HandleDidChangeAsync(request);
                break;
            case "textDocument/didClose":
                HandleDidClose(request);
                break;
            case "textDocument/codeLens":
                await HandleCodeLensAsync(request);
                break;
            case "codeLens/resolve":
                await HandleCodeLensResolveAsync(request);
                break;
            case "textDocument/hover":
                await HandleHoverAsync(request);
                break;
            case "washington/estimateFile":
                await HandleEstimateFileAsync(request);
                break;
            case "washington/estimateWorkspace":
                await HandleEstimateWorkspaceAsync(request);
                break;
            case "washington/clearCache":
                await HandleClearCacheAsync(request);
                break;
            case "shutdown":
                _shutdown = true;
                await SendResponseAsync(request.Id, null);
                break;
            case "exit":
                return;
        }
    }

    private async Task HandleInitializeAsync(LspRequest request)
    {
        if (request.Params.HasValue)
        {
            var initParams = JsonSerializer.Deserialize<InitializeParams>(request.Params.Value, _jsonOptions);
            _rootUri = initParams?.RootUri;
        }

        var result = new InitializeResult
        {
            Capabilities = new ServerCapabilities
            {
                TextDocumentSync = 1, // Full
                CodeLensProvider = new CodeLensOptions { ResolveProvider = true },
                HoverProvider = true
            }
        };

        await SendResponseAsync(request.Id, result);
    }

    private async Task HandleDidOpenAsync(LspRequest request)
    {
        if (!request.Params.HasValue) return;

        var p = JsonSerializer.Deserialize<DidOpenTextDocumentParams>(request.Params.Value, _jsonOptions);
        if (p == null) return;

        var uri = p.TextDocument.Uri;
        _openDocuments[uri] = p.TextDocument.Text;

        await EstimateAndPublishAsync(uri);
    }

    private async Task HandleDidSaveAsync(LspRequest request)
    {
        if (!request.Params.HasValue) return;

        var p = JsonSerializer.Deserialize<DidSaveTextDocumentParams>(request.Params.Value, _jsonOptions);
        if (p == null) return;

        await EstimateAndPublishAsync(p.TextDocument.Uri);
    }

    private async Task HandleDidChangeAsync(LspRequest request)
    {
        if (!request.Params.HasValue) return;

        var p = JsonSerializer.Deserialize<DidChangeTextDocumentParams>(request.Params.Value, _jsonOptions);
        if (p == null) return;

        var uri = p.TextDocument.Uri;

        // Debounce: cancel previous estimation, wait before starting new one
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_debounceMs, token);
                if (!token.IsCancellationRequested)
                    await EstimateAndPublishAsync(uri);
            }
            catch (TaskCanceledException) { }
        });
    }

    private void HandleDidClose(LspRequest request)
    {
        if (!request.Params.HasValue) return;

        var p = JsonSerializer.Deserialize<DidCloseTextDocumentParams>(request.Params.Value, _jsonOptions);
        if (p == null) return;

        _openDocuments.TryRemove(p.TextDocument.Uri, out _);
        _documentReports.TryRemove(p.TextDocument.Uri, out _);
    }

    private async Task HandleCodeLensAsync(LspRequest request)
    {
        if (!request.Params.HasValue)
        {
            await SendResponseAsync(request.Id, Array.Empty<CodeLens>());
            return;
        }

        var p = JsonSerializer.Deserialize<CodeLensParams>(request.Params.Value, _jsonOptions);
        if (p == null)
        {
            await SendResponseAsync(request.Id, Array.Empty<CodeLens>());
            return;
        }

        var uri = p.TextDocument.Uri;
        var codeLenses = new List<CodeLens>();

        if (_documentReports.TryGetValue(uri, out var report))
        {
            var filePath = UriToPath(uri);
            var fileContent = File.Exists(filePath) ? await File.ReadAllTextAsync(filePath) : "";
            var lines = fileContent.Split('\n');

            foreach (var line in report.Lines)
            {
                var lineIndex = FindResourceLine(lines, line.ResourceName, line.ResourceType);
                var costText = line.MonthlyCost > 0
                    ? $"💰 {line.ResourceName}: ${line.MonthlyCost:N2}/mo — {line.PricingDetails}"
                    : $"💰 {line.ResourceName}: no pricing available";

                codeLenses.Add(new CodeLens
                {
                    Range = new Range
                    {
                        Start = new Position { Line = lineIndex, Character = 0 },
                        End = new Position { Line = lineIndex, Character = 0 }
                    },
                    Command = new LspCommand
                    {
                        Title = costText,
                        CommandId = "washington.showCostDetails"
                    }
                });
            }

            // Add total CodeLens at file top
            if (report.Lines.Count > 0)
            {
                codeLenses.Insert(0, new CodeLens
                {
                    Range = new Range
                    {
                        Start = new Position { Line = 0, Character = 0 },
                        End = new Position { Line = 0, Character = 0 }
                    },
                    Command = new LspCommand
                    {
                        Title = $"💰 Estimated Monthly Total: ${report.GrandTotal:N2} {report.Currency}",
                        CommandId = "washington.showCostDetails"
                    }
                });
            }
        }

        await SendResponseAsync(request.Id, codeLenses);
    }

    private async Task HandleCodeLensResolveAsync(LspRequest request)
    {
        if (!request.Params.HasValue)
        {
            await SendResponseAsync(request.Id, null);
            return;
        }

        // CodeLens items are already resolved with commands
        var codeLens = JsonSerializer.Deserialize<CodeLens>(request.Params.Value, _jsonOptions);
        await SendResponseAsync(request.Id, codeLens);
    }

    private async Task HandleHoverAsync(LspRequest request)
    {
        if (!request.Params.HasValue)
        {
            await SendResponseAsync(request.Id, null);
            return;
        }

        var p = JsonSerializer.Deserialize<HoverParams>(request.Params.Value, _jsonOptions);
        if (p == null)
        {
            await SendResponseAsync(request.Id, null);
            return;
        }

        var uri = p.TextDocument.Uri;
        if (!_documentReports.TryGetValue(uri, out var report))
        {
            await SendResponseAsync(request.Id, null);
            return;
        }

        var filePath = UriToPath(uri);
        var fileContent = File.Exists(filePath) ? await File.ReadAllTextAsync(filePath) : "";
        var fileLines = fileContent.Split('\n');
        var hoveredLine = p.Position.Line;

        // Find which resource the user is hovering over
        foreach (var line in report.Lines)
        {
            var resourceLine = FindResourceLine(fileLines, line.ResourceName, line.ResourceType);
            var resourceEndLine = FindResourceEndLine(fileLines, resourceLine);

            if (hoveredLine >= resourceLine && hoveredLine <= resourceEndLine)
            {
                var markdown = $"### 💰 Cost Estimate: {line.ResourceName}\n\n" +
                               $"| Field | Value |\n|-------|-------|\n" +
                               $"| **Type** | {line.ResourceType} |\n" +
                               $"| **Details** | {line.PricingDetails} |\n" +
                               $"| **Monthly Cost** | ${line.MonthlyCost:N2} |\n";

                await SendResponseAsync(request.Id, new Hover
                {
                    Contents = new MarkupContent { Kind = "markdown", Value = markdown }
                });
                return;
            }
        }

        await SendResponseAsync(request.Id, null);
    }

    private async Task HandleEstimateFileAsync(LspRequest request)
    {
        if (!request.Params.HasValue)
        {
            await SendResponseAsync(request.Id, null);
            return;
        }

        var p = JsonSerializer.Deserialize<EstimateFileParams>(request.Params.Value, _jsonOptions);
        if (p == null)
        {
            await SendResponseAsync(request.Id, null);
            return;
        }

        var filePath = UriToPath(p.Uri);
        if (!File.Exists(filePath))
        {
            await SendResponseAsync(request.Id, null);
            return;
        }

        try
        {
            var report = await _costService.EstimateFromBicepAsync(filePath, currency: p.Currency);
            _documentReports[p.Uri] = report;
            await SendResponseAsync(request.Id, report);
        }
        catch (Exception ex)
        {
            await SendErrorResponseAsync(request.Id, -32603, ex.Message);
        }
    }

    private async Task HandleEstimateWorkspaceAsync(LspRequest request)
    {
        if (_rootUri == null)
        {
            await SendResponseAsync(request.Id, null);
            return;
        }

        var currency = "USD";
        if (request.Params.HasValue)
        {
            var p = JsonSerializer.Deserialize<EstimateWorkspaceParams>(request.Params.Value, _jsonOptions);
            if (p != null) currency = p.Currency;
        }

        var rootPath = UriToPath(_rootUri);
        var bicepFiles = Directory.GetFiles(rootPath, "*.bicep", SearchOption.AllDirectories);

        var allLines = new List<ResourceCostLine>();
        var allWarnings = new List<string>();
        var grandTotal = 0m;

        foreach (var file in bicepFiles)
        {
            try
            {
                var report = await _costService.EstimateFromBicepAsync(file, currency: currency);
                allLines.AddRange(report.Lines);
                allWarnings.AddRange(report.Warnings);
                grandTotal += report.GrandTotal;
            }
            catch (Exception ex)
            {
                allWarnings.Add($"⚠ Failed to estimate {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        var workspaceReport = new CostReport(allLines, grandTotal, currency, allWarnings);
        await SendResponseAsync(request.Id, workspaceReport);
    }

    private async Task HandleClearCacheAsync(LspRequest request)
    {
        var cache = new FilePricingCache();
        cache.Clear();
        await SendResponseAsync(request.Id, new { success = true });
    }

    private async Task EstimateAndPublishAsync(string uri)
    {
        var filePath = UriToPath(uri);
        if (!File.Exists(filePath) || !filePath.EndsWith(".bicep"))
            return;

        try
        {
            var report = await _costService.EstimateFromBicepAsync(filePath);
            _documentReports[uri] = report;

            // Publish diagnostics for warnings
            var diagnostics = report.Warnings.Select(w => new Diagnostic
            {
                Range = new Range
                {
                    Start = new Position { Line = 0, Character = 0 },
                    End = new Position { Line = 0, Character = 1 }
                },
                Severity = 2, // Warning
                Source = "washington",
                Message = w
            }).ToList();

            await SendNotificationAsync("textDocument/publishDiagnostics", new PublishDiagnosticsParams
            {
                Uri = uri,
                Diagnostics = diagnostics
            });
        }
        catch (Exception ex)
        {
            await LogAsync($"Estimation failed for {uri}: {ex.Message}");
        }
    }

    private static int FindResourceLine(string[] lines, string resourceName, string resourceType)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.StartsWith("resource ") && (line.Contains(resourceName) || line.Contains(resourceType)))
                return i;
        }
        return 0;
    }

    private static int FindResourceEndLine(string[] lines, int startLine)
    {
        int braceCount = 0;
        for (int i = startLine; i < lines.Length; i++)
        {
            foreach (var ch in lines[i])
            {
                if (ch == '{') braceCount++;
                if (ch == '}') braceCount--;
            }
            if (braceCount == 0 && i > startLine) return i;
        }
        return Math.Min(startLine + 1, lines.Length - 1);
    }

    private static string UriToPath(string uri)
    {
        if (uri.StartsWith("file:///"))
            return Uri.UnescapeDataString(uri[7..]);
        if (uri.StartsWith("file://"))
            return Uri.UnescapeDataString(uri[7..]);
        return uri;
    }

    // LSP message I/O
    private async Task<string?> ReadMessageAsync(StreamReader reader)
    {
        // Read headers
        int contentLength = 0;
        string? headerLine;
        while ((headerLine = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrEmpty(headerLine))
                break;

            if (headerLine.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
            {
                var value = headerLine["Content-Length:".Length..].Trim();
                contentLength = int.Parse(value);
            }
        }

        if (contentLength == 0)
            return null;

        var buffer = new char[contentLength];
        var bytesRead = 0;
        while (bytesRead < contentLength)
        {
            var read = await reader.ReadAsync(buffer, bytesRead, contentLength - bytesRead);
            if (read == 0) return null;
            bytesRead += read;
        }

        return new string(buffer, 0, contentLength);
    }

    private async Task SendResponseAsync(object? id, object? result)
    {
        var response = new LspResponse
        {
            Id = id,
            Result = result
        };
        await WriteMessageAsync(response);
    }

    private async Task SendErrorResponseAsync(object? id, int code, string message)
    {
        var response = new LspResponse
        {
            Id = id,
            Error = new LspError { Code = code, Message = message }
        };
        await WriteMessageAsync(response);
    }

    private async Task SendNotificationAsync(string method, object? @params)
    {
        var notification = new LspNotification
        {
            Method = method,
            Params = @params
        };
        await WriteMessageAsync(notification);
    }

    private async Task LogAsync(string message)
    {
        await SendNotificationAsync("window/logMessage", new { type = 4, message }); // 4 = Log
    }

    private readonly SemaphoreSlim _writeLock = new(1, 1);

    private async Task WriteMessageAsync(object message)
    {
        var json = JsonSerializer.Serialize(message, _jsonOptions);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var header = $"Content-Length: {bytes.Length}\r\n\r\n";
        var headerBytes = System.Text.Encoding.UTF8.GetBytes(header);

        await _writeLock.WaitAsync();
        try
        {
            await _output.WriteAsync(headerBytes);
            await _output.WriteAsync(bytes);
            await _output.FlushAsync();
        }
        finally
        {
            _writeLock.Release();
        }
    }
}
