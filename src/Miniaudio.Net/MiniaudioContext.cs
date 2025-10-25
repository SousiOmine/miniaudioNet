using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Miniaudio.Net.Interop;

namespace Miniaudio.Net;

public sealed class MiniaudioContext : IDisposable
{
    private ContextHandle? _handle;

    private MiniaudioContext(ContextHandle handle, IReadOnlyList<MiniaudioBackend> preferredBackends)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
        PreferredBackends = preferredBackends;
    }

    public IReadOnlyList<MiniaudioBackend> PreferredBackends { get; }

    public static MiniaudioContext Create(IEnumerable<MiniaudioBackend>? preferredBackends = null)
    {
        ContextHandle handle;
        IReadOnlyList<MiniaudioBackend> snapshot;

        var backendArray = preferredBackends?.Distinct().ToArray();
        if (backendArray is null || backendArray.Length == 0)
        {
            handle = NativeMethods.ContextCreateDefault();
            snapshot = Array.Empty<MiniaudioBackend>();
        }
        else
        {
            var backendIds = Array.ConvertAll(backendArray, backend => (int)backend);
            handle = NativeMethods.ContextCreateWithBackends(backendIds);
            snapshot = backendArray;
        }

        if (handle is null || handle.IsInvalid)
        {
            throw new InvalidOperationException("Failed to initialize a miniaudio context. Check whether native binaries are available.");
        }

        return new MiniaudioContext(handle, snapshot);
    }

    public IReadOnlyList<MiniaudioDeviceInfo> EnumerateDevices(MiniaudioDeviceKind kind)
    {
        ThrowIfDisposed();

        unsafe
        {
            NativeMethods.ContextGetDevices(_handle!, (int)kind, null, 0, out var totalCount)
                .EnsureSuccess(nameof(NativeMethods.ContextGetDevices));

            if (totalCount == 0)
            {
                return Array.Empty<MiniaudioDeviceInfo>();
            }

            var buffer = new NativeMethods.NativeDeviceInfo[totalCount];
            fixed (NativeMethods.NativeDeviceInfo* pBuffer = buffer)
            {
                NativeMethods.ContextGetDevices(_handle!, (int)kind, pBuffer, (uint)buffer.Length, out _)
                    .EnsureSuccess(nameof(NativeMethods.ContextGetDevices));
            }

            var devices = new List<MiniaudioDeviceInfo>(buffer.Length);
            for (var i = 0; i < buffer.Length; i++)
            {
                devices.Add(Convert(in buffer[i]));
            }

            return devices;
        }
    }

    internal ContextHandle DangerousHandle
    {
        get
        {
            ThrowIfDisposed();
            return _handle!;
        }
    }

    public void Dispose()
    {
        if (_handle is null)
        {
            return;
        }

        _handle.Dispose();
        _handle = null;
        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
    {
        if (_handle is null || _handle.IsClosed)
        {
            throw new ObjectDisposedException(nameof(MiniaudioContext));
        }
    }

    private static unsafe MiniaudioDeviceInfo Convert(in NativeMethods.NativeDeviceInfo native)
    {
        string name;
        string deviceId;

        fixed (NativeMethods.NativeDeviceInfo* pNative = &native)
        {
            name = ReadNullTerminatedString(pNative->Name, NativeMethods.DeviceNameBufferSize, Encoding.UTF8);
            deviceId = ReadNullTerminatedString(pNative->Id, NativeMethods.DeviceIdBufferSize, Encoding.ASCII);
        }

        return new MiniaudioDeviceInfo(
            name,
            (MiniaudioDeviceKind)native.DeviceType,
            native.IsDefault != 0,
            deviceId);
    }

    private static unsafe string ReadNullTerminatedString(byte* buffer, int maxLength, Encoding encoding)
    {
        var span = new ReadOnlySpan<byte>(buffer, maxLength);
        var terminator = span.IndexOf((byte)0);
        if (terminator >= 0)
        {
            span = span[..terminator];
        }

        return span.Length == 0 ? string.Empty : encoding.GetString(span);
    }
}
