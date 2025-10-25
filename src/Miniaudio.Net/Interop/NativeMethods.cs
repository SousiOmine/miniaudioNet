using System;
using System.Runtime.InteropServices;
using Miniaudio.Net;

namespace Miniaudio.Net.Interop;

internal static partial class NativeMethods
{
    private const string LibraryName = "miniaudionet";

    internal static EngineHandle EngineCreate()
    {
        var handle = EngineCreateCore();
        return EngineHandle.FromIntPtr(handle);
    }

    [LibraryImport(LibraryName, EntryPoint = "manet_engine_create_default")]
    private static partial IntPtr EngineCreateCore();

    [LibraryImport(LibraryName, EntryPoint = "manet_engine_destroy")]
    internal static partial void EngineDestroy(IntPtr handle);

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

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_seek_to_pcm_frame")]
    internal static partial int SoundSeek(SoundHandle handle, ulong frameIndex);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_get_length_in_pcm_frames")]
    internal static partial int SoundGetLength(SoundHandle handle, out ulong lengthInFrames);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_get_cursor_in_pcm_frames")]
    internal static partial int SoundGetCursor(SoundHandle handle, out ulong cursorInFrames);

    [LibraryImport(LibraryName, EntryPoint = "manet_sound_get_sample_rate")]
    internal static partial uint SoundGetSampleRate(SoundHandle handle);

    [LibraryImport(LibraryName, EntryPoint = "manet_result_description", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial string DescribeResult(int result);
}
