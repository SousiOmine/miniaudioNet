using System;
using System.Runtime.InteropServices;
using Miniaudio.Net;

namespace Miniaudio.Net.Interop;

internal static partial class NativeMethods
{
    private const string LibraryName = "miniaudionet";

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void SoundEndCallback(SoundHandle sound, IntPtr userData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void CaptureDeviceDataCallback(IntPtr samples, uint frameCount, uint channelCount, IntPtr userData);

    internal static EngineHandle EngineCreate()
    {
        var handle = EngineCreateCore();
        return EngineHandle.FromIntPtr(handle);
    }

    internal static EngineHandle EngineCreateWithOptions(
        ContextHandle? context,
        ResourceManagerHandle? resourceManager,
        string? playbackDeviceId,
        uint sampleRate,
        uint channels,
        uint periodSizeInFrames,
        uint periodSizeInMilliseconds,
        bool noAutoStart,
        bool noDevice)
    {
        var contextPtr = IntPtr.Zero;
        var resourceManagerPtr = IntPtr.Zero;
        var contextAddRef = false;
        var resourceManagerAddRef = false;

        try
        {
            if (context is not null)
            {
                context.DangerousAddRef(ref contextAddRef);
                contextPtr = context.DangerousGetHandle();
            }

            if (resourceManager is not null)
            {
                resourceManager.DangerousAddRef(ref resourceManagerAddRef);
                resourceManagerPtr = resourceManager.DangerousGetHandle();
            }

#if DEBUG
            Console.Error.WriteLine($"[Miniaudio.Net] EngineCreateWithOptions context=0x{contextPtr.ToInt64():X}, resourceManager=0x{resourceManagerPtr.ToInt64():X}");
#endif

            var handle = EngineCreateWithOptionsCore(
                contextPtr,
                resourceManagerPtr,
                playbackDeviceId,
                sampleRate,
                channels,
                periodSizeInFrames,
                periodSizeInMilliseconds,
                noAutoStart ? 1 : 0,
                noDevice ? 1 : 0);

            return EngineHandle.FromIntPtr(handle);
        }
        finally
        {
            if (resourceManagerAddRef)
            {
                resourceManager!.DangerousRelease();
            }

            if (contextAddRef)
            {
                context!.DangerousRelease();
            }
        }
    }

    internal static ContextHandle ContextCreateDefault()
    {
        var handle = ContextCreateDefaultCore();
        return ContextHandle.FromIntPtr(handle);
    }

    internal static ContextHandle ContextCreateWithBackends(int[] backends)
    {
        if (backends is null || backends.Length == 0)
        {
            throw new ArgumentException("At least one backend must be specified.", nameof(backends));
        }

        var handle = ContextCreateWithBackendsCore(backends, (uint)backends.Length);
        return ContextHandle.FromIntPtr(handle);
    }

    internal static ResourceManagerHandle ResourceManagerCreateDefault()
    {
        var handle = ResourceManagerCreateDefaultCore();
        return ResourceManagerHandle.FromIntPtr(handle);
    }

    internal static ResourceManagerHandle ResourceManagerCreate(ResourceManagerConfig config)
    {
        var handle = ResourceManagerCreateWithConfigCore(in config);
        return ResourceManagerHandle.FromIntPtr(handle);
    }

    [LibraryImport(LibraryName, EntryPoint = "manet_engine_create_default")]
    private static partial IntPtr EngineCreateCore();

    [LibraryImport(LibraryName, EntryPoint = "manet_engine_create_with_options", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr EngineCreateWithOptionsCore(
        IntPtr context,
        IntPtr resourceManager,
        string? playbackDeviceId,
        uint sampleRate,
        uint channels,
        uint periodSizeInFrames,
        uint periodSizeInMilliseconds,
        int noAutoStart,
        int noDevice);

    [LibraryImport(LibraryName, EntryPoint = "manet_context_create_default")]
    private static partial IntPtr ContextCreateDefaultCore();

    [LibraryImport(LibraryName, EntryPoint = "manet_context_create_with_backends")]
    private static partial IntPtr ContextCreateWithBackendsCore(
        [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I4)] int[] backends,
        uint backendCount);

    [LibraryImport(LibraryName, EntryPoint = "manet_resource_manager_create_default")]
    private static partial IntPtr ResourceManagerCreateDefaultCore();

    [LibraryImport(LibraryName, EntryPoint = "manet_resource_manager_create_with_config")]
    private static partial IntPtr ResourceManagerCreateWithConfigCore(in ResourceManagerConfig config);

    [LibraryImport(LibraryName, EntryPoint = "manet_engine_destroy")]
    internal static partial void EngineDestroy(IntPtr handle);

    [LibraryImport(LibraryName, EntryPoint = "manet_context_destroy")]
    internal static partial void ContextDestroy(IntPtr handle);

    [LibraryImport(LibraryName, EntryPoint = "manet_resource_manager_destroy")]
    internal static partial void ResourceManagerDestroy(IntPtr handle);

    [LibraryImport(LibraryName, EntryPoint = "manet_engine_stop")]
    internal static partial int EngineStop(EngineHandle handle);

    [LibraryImport(LibraryName, EntryPoint = "manet_engine_set_volume")]
    internal static partial int EngineSetVolume(EngineHandle handle, float volume);

    [LibraryImport(LibraryName, EntryPoint = "manet_engine_get_volume")]
    internal static partial float EngineGetVolume(EngineHandle handle);

    [LibraryImport(LibraryName, EntryPoint = "manet_engine_play_sound", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int EnginePlaySound(EngineHandle handle, string path);

    [LibraryImport(LibraryName, EntryPoint = "manet_engine_set_listener_position")]
    internal static partial int EngineSetListenerPosition(EngineHandle handle, uint index, float x, float y, float z);

    [LibraryImport(LibraryName, EntryPoint = "manet_engine_set_listener_direction")]
    internal static partial int EngineSetListenerDirection(EngineHandle handle, uint index, float x, float y, float z);

    [LibraryImport(LibraryName, EntryPoint = "manet_engine_set_listener_world_up")]
    internal static partial int EngineSetListenerWorldUp(EngineHandle handle, uint index, float x, float y, float z);

    [LibraryImport(LibraryName, EntryPoint = "manet_engine_set_listener_velocity")]
    internal static partial int EngineSetListenerVelocity(EngineHandle handle, uint index, float x, float y, float z);

    [LibraryImport(LibraryName, EntryPoint = "manet_engine_get_sample_rate")]
    internal static partial uint EngineGetSampleRate(EngineHandle handle);

    [LibraryImport(LibraryName, EntryPoint = "manet_engine_get_time_in_pcm_frames")]
    internal static partial ulong EngineGetTimeInPcmFrames(EngineHandle handle);

    internal static SoundHandle SoundCreateFromFile(EngineHandle engine, string path, uint flags)
    {
        var handle = OperatingSystem.IsWindows()
            ? SoundCreateFromFileWCore(engine, path, flags)
            : SoundCreateFromFileCore(engine, path, flags);
        return SoundHandle.FromIntPtr(handle);
    }

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_create_from_file", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr SoundCreateFromFileCore(EngineHandle engine, string path, uint flags);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_create_from_file_w", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr SoundCreateFromFileWCore(EngineHandle engine, string path, uint flags);

    internal static unsafe SoundHandle SoundCreateFromPcmFrames(EngineHandle engine, ReadOnlySpan<float> frames, ulong frameCount, uint channels, uint sampleRate, uint flags)
    {
        fixed (float* pFrames = frames)
        {
            var handle = SoundCreateFromPcmFramesCore(engine, pFrames, frameCount, channels, sampleRate, flags);
            return SoundHandle.FromIntPtr(handle);
        }
    }

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_create_from_pcm_frames")]
    private static unsafe partial IntPtr SoundCreateFromPcmFramesCore(EngineHandle engine, float* frames, ulong frameCount, uint channels, uint sampleRate, uint flags);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_destroy")]
    internal static partial void SoundDestroy(IntPtr handle);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_start")]
    internal static partial int SoundStart(SoundHandle handle);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_stop")]
    internal static partial int SoundStop(SoundHandle handle);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_set_volume")]
    internal static partial int SoundSetVolume(SoundHandle handle, float volume);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_get_volume")]
    internal static partial float SoundGetVolume(SoundHandle handle);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_get_state")]
    internal static partial SoundState SoundGetState(SoundHandle handle);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_set_pitch")]
    internal static partial int SoundSetPitch(SoundHandle handle, float pitch);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_get_pitch")]
    internal static partial float SoundGetPitch(SoundHandle handle);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_set_pan")]
    internal static partial int SoundSetPan(SoundHandle handle, float pan);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_get_pan")]
    internal static partial float SoundGetPan(SoundHandle handle);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_set_looping")]
    internal static partial int SoundSetLooping(SoundHandle handle, int isLooping);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_is_looping")]
    internal static partial int SoundIsLooping(SoundHandle handle);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_set_position")]
    internal static partial int SoundSetPosition(SoundHandle handle, float x, float y, float z);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_get_position")]
    internal static partial int SoundGetPosition(SoundHandle handle, out float x, out float y, out float z);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_set_direction")]
    internal static partial int SoundSetDirection(SoundHandle handle, float x, float y, float z);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_get_direction")]
    internal static partial int SoundGetDirection(SoundHandle handle, out float x, out float y, out float z);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_set_positioning")]
    internal static partial int SoundSetPositioning(SoundHandle handle, SoundPositioning positioning);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_get_positioning")]
    internal static partial SoundPositioning SoundGetPositioning(SoundHandle handle);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_set_fade_in_pcm_frames")]
    internal static partial int SoundSetFadeInFrames(SoundHandle handle, float volumeBeg, float volumeEnd, ulong lengthInFrames);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_set_fade_start_in_pcm_frames")]
    internal static partial int SoundSetFadeStartInFrames(
        SoundHandle handle,
        float volumeBeg,
        float volumeEnd,
        ulong lengthInFrames,
        ulong absoluteStartInFrames);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_seek_to_pcm_frame")]
    internal static partial int SoundSeek(SoundHandle handle, ulong frameIndex);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_get_length_in_pcm_frames")]
    internal static partial int SoundGetLength(SoundHandle handle, out ulong lengthInFrames);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_get_cursor_in_pcm_frames")]
    internal static partial int SoundGetCursor(SoundHandle handle, out ulong cursorInFrames);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_get_sample_rate")]
    internal static partial uint SoundGetSampleRate(SoundHandle handle);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_set_start_time_in_pcm_frames")]
    internal static partial int SoundSetStartTimeInFrames(SoundHandle handle, ulong absoluteFrameIndex);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_set_stop_time_in_pcm_frames")]
    internal static partial int SoundSetStopTimeInFrames(SoundHandle handle, ulong absoluteFrameIndex);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_set_stop_time_with_fade_in_pcm_frames")]
    internal static partial int SoundSetStopTimeWithFadeInFrames(SoundHandle handle, ulong absoluteFrameIndex, ulong fadeLengthInFrames);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_set_end_callback")]
    internal static partial int SoundSetEndCallback(SoundHandle handle, SoundEndCallback? callback, IntPtr userData);

    internal static CaptureDeviceHandle CaptureDeviceCreate(
        ContextHandle? context,
        string? captureDeviceId,
        uint sampleRate,
        uint channels,
        CaptureDeviceDataCallback callback,
        IntPtr userData)
    {
        var contextPtr = IntPtr.Zero;
        var contextAddRef = false;

        try
        {
            if (context is not null)
            {
                context.DangerousAddRef(ref contextAddRef);
                contextPtr = context.DangerousGetHandle();
            }

            var handle = CaptureDeviceCreateCore(
                contextPtr,
                captureDeviceId,
                sampleRate,
                channels,
                callback,
                userData);

            return CaptureDeviceHandle.FromIntPtr(handle);
        }
        finally
        {
            if (contextAddRef)
            {
                context!.DangerousRelease();
            }
        }
    }

    [LibraryImport(LibraryName, EntryPoint = "manet_capture_device_create", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr CaptureDeviceCreateCore(
        IntPtr context,
        string? captureDeviceId,
        uint sampleRate,
        uint channels,
        CaptureDeviceDataCallback callback,
        IntPtr userData);

    [LibraryImport(LibraryName, EntryPoint = "manet_capture_device_start")]
    internal static partial int CaptureDeviceStart(CaptureDeviceHandle handle);

    [LibraryImport(LibraryName, EntryPoint = "manet_capture_device_stop")]
    internal static partial int CaptureDeviceStop(CaptureDeviceHandle handle);

    [LibraryImport(LibraryName, EntryPoint = "manet_capture_device_destroy")]
    internal static partial void CaptureDeviceDestroy(IntPtr handle);

    [LibraryImport(LibraryName, EntryPoint = "manet_result_description", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial string DescribeResult(int result);

    [LibraryImport(LibraryName, EntryPoint = "manet_context_get_devices")]
    internal static unsafe partial int ContextGetDevices(
        ContextHandle handle,
        int deviceType,
        NativeDeviceInfo* descriptors,
        uint descriptorCapacity,
        out uint deviceCount);

    internal const int DeviceNameBufferSize = 256;
    internal const int DeviceIdBufferSize = 513;

    [StructLayout(LayoutKind.Sequential)]
    internal struct ResourceManagerConfig
    {
        public uint Flags;
        public uint DecodedFormat;
        public uint DecodedChannels;
        public uint DecodedSampleRate;
        public uint JobThreadCount;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal unsafe struct NativeDeviceInfo
    {
        public int DeviceType;
        public int IsDefault;
        public fixed byte Name[DeviceNameBufferSize];
        public fixed byte Id[DeviceIdBufferSize];
    }
}
