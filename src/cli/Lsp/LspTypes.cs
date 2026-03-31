using System.Text.Json;
using System.Text.Json.Serialization;
using Washington.Models;

namespace Washington.Lsp;

// LSP Base Types
public class LspMessage
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
}

public class LspRequest : LspMessage
{
    [JsonPropertyName("id")]
    public object? Id { get; set; }

    [JsonPropertyName("method")]
    public string? Method { get; set; }

    [JsonPropertyName("params")]
    public JsonElement? Params { get; set; }
}

public class LspResponse : LspMessage
{
    [JsonPropertyName("id")]
    public object? Id { get; set; }

    [JsonPropertyName("result")]
    public object? Result { get; set; }

    [JsonPropertyName("error")]
    public LspError? Error { get; set; }
}

public class LspNotification : LspMessage
{
    [JsonPropertyName("method")]
    public string? Method { get; set; }

    [JsonPropertyName("params")]
    public object? Params { get; set; }
}

public class LspError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

// LSP Protocol Types
public class InitializeParams
{
    [JsonPropertyName("capabilities")]
    public JsonElement? Capabilities { get; set; }

    [JsonPropertyName("rootUri")]
    public string? RootUri { get; set; }
}

public class ServerCapabilities
{
    [JsonPropertyName("textDocumentSync")]
    public int TextDocumentSync { get; set; } = 1; // Full

    [JsonPropertyName("codeLensProvider")]
    public CodeLensOptions? CodeLensProvider { get; set; }

    [JsonPropertyName("hoverProvider")]
    public bool HoverProvider { get; set; }
}

public class CodeLensOptions
{
    [JsonPropertyName("resolveProvider")]
    public bool ResolveProvider { get; set; }
}

public class InitializeResult
{
    [JsonPropertyName("capabilities")]
    public ServerCapabilities Capabilities { get; set; } = new();
}

public class TextDocumentIdentifier
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = "";
}

public class TextDocumentItem
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = "";

    [JsonPropertyName("languageId")]
    public string LanguageId { get; set; } = "";

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";
}

public class DidOpenTextDocumentParams
{
    [JsonPropertyName("textDocument")]
    public TextDocumentItem TextDocument { get; set; } = new();
}

public class DidSaveTextDocumentParams
{
    [JsonPropertyName("textDocument")]
    public TextDocumentIdentifier TextDocument { get; set; } = new();
}

public class DidChangeTextDocumentParams
{
    [JsonPropertyName("textDocument")]
    public VersionedTextDocumentIdentifier TextDocument { get; set; } = new();
}

public class VersionedTextDocumentIdentifier : TextDocumentIdentifier
{
    [JsonPropertyName("version")]
    public int Version { get; set; }
}

public class DidCloseTextDocumentParams
{
    [JsonPropertyName("textDocument")]
    public TextDocumentIdentifier TextDocument { get; set; } = new();
}

public class CodeLensParams
{
    [JsonPropertyName("textDocument")]
    public TextDocumentIdentifier TextDocument { get; set; } = new();
}

public class HoverParams
{
    [JsonPropertyName("textDocument")]
    public TextDocumentIdentifier TextDocument { get; set; } = new();

    [JsonPropertyName("position")]
    public Position Position { get; set; } = new();
}

public class Position
{
    [JsonPropertyName("line")]
    public int Line { get; set; }

    [JsonPropertyName("character")]
    public int Character { get; set; }
}

public class Range
{
    [JsonPropertyName("start")]
    public Position Start { get; set; } = new();

    [JsonPropertyName("end")]
    public Position End { get; set; } = new();
}

public class CodeLens
{
    [JsonPropertyName("range")]
    public Range Range { get; set; } = new();

    [JsonPropertyName("command")]
    public LspCommand? Command { get; set; }

    [JsonPropertyName("data")]
    public JsonElement? Data { get; set; }
}

public class LspCommand
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("command")]
    public string CommandId { get; set; } = "";

    [JsonPropertyName("arguments")]
    public List<object>? Arguments { get; set; }
}

public class Hover
{
    [JsonPropertyName("contents")]
    public MarkupContent Contents { get; set; } = new();

    [JsonPropertyName("range")]
    public Range? Range { get; set; }
}

public class MarkupContent
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "markdown";

    [JsonPropertyName("value")]
    public string Value { get; set; } = "";
}

public class Diagnostic
{
    [JsonPropertyName("range")]
    public Range Range { get; set; } = new();

    [JsonPropertyName("severity")]
    public int Severity { get; set; } // 1=Error, 2=Warning, 3=Info, 4=Hint

    [JsonPropertyName("source")]
    public string Source { get; set; } = "washington";

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";
}

public class PublishDiagnosticsParams
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = "";

    [JsonPropertyName("diagnostics")]
    public List<Diagnostic> Diagnostics { get; set; } = new();
}

// Custom request types
public class EstimateFileParams
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = "";

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "USD";
}

public class EstimateWorkspaceParams
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "USD";
}
