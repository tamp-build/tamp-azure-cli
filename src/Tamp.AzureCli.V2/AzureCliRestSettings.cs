namespace Tamp.AzureCli.V2;

/// <summary>
/// Settings for <c>az rest</c> — the generic REST proxy. This is the
/// load-bearing verb for the long tail of az functionality: Flex
/// Consumption listKeys, ADO REST proxy, any ARM operation without a
/// dedicated typed verb.
/// </summary>
public sealed class AzureCliRestSettings : AzureCliSettingsBase
{
    /// <summary>Request URL. Required. If it doesn't start with a host, az treats it as an ARM resource ID and prefixes with the ARM endpoint. Maps to <c>--uri</c> / <c>--url</c> / <c>-u</c>.</summary>
    public string? Uri { get; set; }

    /// <summary>HTTP method. Maps to <c>--method</c> / <c>-m</c>. Allowed: <c>get</c>, <c>post</c>, <c>put</c>, <c>patch</c>, <c>delete</c>, <c>head</c>, <c>options</c>. Default: get.</summary>
    public string? Method { get; set; }

    /// <summary>Request body. Use <c>@{file}</c> to load from a file. Maps to <c>--body</c> / <c>-b</c>.</summary>
    public string? Body { get; set; }

    /// <summary>Headers as KEY=VALUE pairs. Maps to repeated entries under <c>--headers</c> (space-separated KEY=VALUE).</summary>
    public Dictionary<string, string> Headers { get; } = new();

    /// <summary>URI query parameters as KEY=VALUE pairs. Maps to <c>--uri-parameters</c>.</summary>
    public Dictionary<string, string> UriParameters { get; } = new();

    /// <summary>Override the AAD resource scope used to acquire the bearer token. Maps to <c>--resource</c>.</summary>
    public string? Resource { get; set; }

    /// <summary>Save response payload to a file. Maps to <c>--output-file</c>.</summary>
    public string? OutputFile { get; set; }

    /// <summary>Don't auto-append Authorization header. Maps to <c>--skip-authorization-header</c>.</summary>
    public bool SkipAuthorizationHeader { get; set; }

    public AzureCliRestSettings SetUri(string uri) { Uri = uri; return this; }
    public AzureCliRestSettings SetMethod(string method) { Method = method; return this; }
    public AzureCliRestSettings SetBody(string body) { Body = body; return this; }
    public AzureCliRestSettings SetBodyFromFile(string path) { Body = $"@{path}"; return this; }
    public AzureCliRestSettings SetHeader(string key, string value) { Headers[key] = value; return this; }
    public AzureCliRestSettings SetUriParameter(string key, string value) { UriParameters[key] = value; return this; }
    public AzureCliRestSettings SetResource(string resource) { Resource = resource; return this; }
    public AzureCliRestSettings SetOutputFile(string path) { OutputFile = path; return this; }
    public AzureCliRestSettings SetSkipAuthorizationHeader(bool v = true) { SkipAuthorizationHeader = v; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        if (string.IsNullOrEmpty(Uri))
            throw new InvalidOperationException("az rest: Uri is required.");
        var args = new List<string> { "rest" };
        EmitCommonArguments(args);
        args.Add("--uri"); args.Add(Uri!);
        if (!string.IsNullOrEmpty(Method)) { args.Add("--method"); args.Add(Method!); }
        if (!string.IsNullOrEmpty(Body)) { args.Add("--body"); args.Add(Body!); }
        if (Headers.Count > 0)
        {
            args.Add("--headers");
            args.AddRange(Headers.Select(kv => $"{kv.Key}={kv.Value}"));
        }
        if (UriParameters.Count > 0)
        {
            args.Add("--uri-parameters");
            args.AddRange(UriParameters.Select(kv => $"{kv.Key}={kv.Value}"));
        }
        if (!string.IsNullOrEmpty(Resource)) { args.Add("--resource"); args.Add(Resource!); }
        if (!string.IsNullOrEmpty(OutputFile)) { args.Add("--output-file"); args.Add(OutputFile!); }
        if (SkipAuthorizationHeader) args.Add("--skip-authorization-header");
        return args;
    }
}
