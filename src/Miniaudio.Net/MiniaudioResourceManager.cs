using System;
using Miniaudio.Net.Interop;

namespace Miniaudio.Net;

public sealed class MiniaudioResourceManager : IDisposable
{
    private ResourceManagerHandle? _handle;
    private readonly MiniaudioResourceManagerOptions _options;

    private MiniaudioResourceManager(ResourceManagerHandle handle, MiniaudioResourceManagerOptions options)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
        _options = options;
    }

    public static MiniaudioResourceManager Create(MiniaudioResourceManagerOptions? options = null)
    {
        options ??= new MiniaudioResourceManagerOptions();
        options.Validate();

        ResourceManagerHandle handle = options.HasOverrides
            ? NativeMethods.ResourceManagerCreate(options.ToNativeConfig())
            : NativeMethods.ResourceManagerCreateDefault();

        if (handle is null || handle.IsInvalid)
        {
            throw new InvalidOperationException("Failed to initialize a miniaudio resource manager. Confirm that the native miniaudionet library is built and discoverable.");
        }

        return new MiniaudioResourceManager(handle, options.Snapshot());
    }

    public MiniaudioResourceManagerOptions Options => _options;

    internal ResourceManagerHandle DangerousHandle
    {
        get
        {
            if (_handle is null || _handle.IsClosed)
            {
                throw new ObjectDisposedException(nameof(MiniaudioResourceManager));
            }

            return _handle;
        }
    }

    public void Dispose()
    {
        _handle?.Dispose();
        _handle = null;
        GC.SuppressFinalize(this);
    }
}
