using System;

namespace Miniaudio.Net;

public sealed class MiniaudioException : InvalidOperationException
{
    public MiniaudioException(int errorCode, string api, string? description)
        : base($"miniaudio API '{api}' failed with code {errorCode}: {description ?? "(no description)"}.")
    {
        ErrorCode = errorCode;
        Api = api;
        Description = description ?? string.Empty;
    }

    public int ErrorCode { get; }

    public string Api { get; }

    public string Description { get; }
}
