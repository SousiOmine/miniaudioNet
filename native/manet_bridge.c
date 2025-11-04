#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>
#include <string.h>
#include <stdio.h>

#if !defined(MINIAUDIO_IMPLEMENTATION)
#define MINIAUDIO_IMPLEMENTATION
#endif
#include "miniaudio.h"

#if defined(_WIN32)
#define MANET_API __declspec(dllexport)
#else
#define MANET_API __attribute__((visibility("default")))
#endif

typedef struct manet_context {
    ma_context context;
} manet_context;

typedef struct manet_engine {
    ma_engine engine;
} manet_engine;

enum {
    MANET_DEVICE_NAME_BUFFER_SIZE = MA_MAX_DEVICE_NAME_LENGTH + 1,
    MANET_DEVICE_ID_HEX_LENGTH = sizeof(ma_device_id) * 2,
    MANET_DEVICE_ID_HEX_BUFFER_SIZE = (sizeof(ma_device_id) * 2) + 1
};

typedef struct manet_device_descriptor {
    ma_device_type type;
    ma_bool32 isDefault;
    char name[MANET_DEVICE_NAME_BUFFER_SIZE];
    char id[MANET_DEVICE_ID_HEX_BUFFER_SIZE];
} manet_device_descriptor;

typedef enum manet_sound_state {
    MANET_SOUND_STATE_STOPPED = 0,
    MANET_SOUND_STATE_PLAYING = 1,
    MANET_SOUND_STATE_STARTING = 2,
    MANET_SOUND_STATE_STOPPING = 3
} manet_sound_state;

typedef struct manet_pcm_stream manet_pcm_stream;
typedef struct manet_sound manet_sound;
typedef void (*manet_sound_end_proc)(manet_sound* handle, void* userData);

struct manet_sound {
    ma_sound sound;
    manet_sound_state state;
    ma_bool32 ownsAudioBuffer;
    ma_audio_buffer audioBuffer;
    manet_pcm_stream* stream;
    ma_bool32 isStreaming;
    /* Managed callback forwarding. */
    void* managedEndUserData;
    manet_sound_end_proc managedEndCallback;
};

typedef struct manet_resource_manager {
    ma_resource_manager manager;
} manet_resource_manager;

typedef struct manet_resource_manager_config_simple {
    ma_uint32 flags;
    ma_uint32 decodedFormat;
    ma_uint32 decodedChannels;
    ma_uint32 decodedSampleRate;
    ma_uint32 jobThreadCount;
} manet_resource_manager_config_simple;

typedef void (*manet_capture_device_proc)(const float* samples, ma_uint32 frameCount, ma_uint32 channelCount, void* userData);

typedef struct manet_capture_device {
    ma_device device;
    manet_capture_device_proc callback;
    void* userData;
    ma_uint32 channelCount;
} manet_capture_device;

static void manet_copy_string(char* dst, size_t dstSize, const char* src);
static void manet_device_id_to_hex(const ma_device_id* id, char* buffer, size_t bufferSize);
static int manet_hex_value(char digit);
static ma_bool32 manet_device_id_from_hex(const char* hex, ma_device_id* id);
static void manet_write_device_descriptor(manet_device_descriptor* dst, const ma_device_info* src, ma_device_type type);
static ma_uint32 manet_min_u32(ma_uint32 a, ma_uint32 b);
static manet_engine* manet_engine_create_with_config(const ma_engine_config* inputConfig);
static void manet_apply_resource_manager_settings(ma_resource_manager_config* config, const manet_resource_manager_config_simple* settings);
static void manet_sound_end_callback_trampoline(void* pUserData, ma_sound* pSound);
static void manet_capture_device_data_callback(ma_device* pDevice, void* pOutput, const void* pInput, ma_uint32 frameCount);
static manet_pcm_stream* manet_pcm_stream_create(ma_uint32 channels, ma_uint32 sampleRate, ma_uint32 capacityInFrames);
static void manet_pcm_stream_destroy(manet_pcm_stream* stream);
static ma_result manet_pcm_stream_append_pcm_frames(manet_pcm_stream* stream, const float* frames, ma_uint64 frameCount, ma_uint64* framesWritten);
static ma_uint64 manet_pcm_stream_capacity(const manet_pcm_stream* stream);
static ma_uint64 manet_pcm_stream_available_read(const manet_pcm_stream* stream);
static ma_uint64 manet_pcm_stream_available_write(const manet_pcm_stream* stream);
static ma_result manet_pcm_stream_reset(manet_pcm_stream* stream);
static void manet_pcm_stream_mark_end(manet_pcm_stream* stream);
static void manet_pcm_stream_clear_end(manet_pcm_stream* stream);
static ma_bool32 manet_pcm_stream_is_end_requested(const manet_pcm_stream* stream);
static ma_uint32 manet_pcm_stream_get_channels(const manet_pcm_stream* stream);
static ma_uint32 manet_pcm_stream_get_sample_rate(const manet_pcm_stream* stream);
static ma_result manet_pcm_stream_on_read(ma_data_source* pDataSource, void* pFramesOut, ma_uint64 frameCount, ma_uint64* pFramesRead);
static ma_result manet_pcm_stream_on_get_data_format(ma_data_source* pDataSource, ma_format* pFormat, ma_uint32* pChannels, ma_uint32* pSampleRate, ma_channel* pChannelMap, size_t channelMapCap);

struct manet_pcm_stream {
    ma_data_source_base ds;
    ma_pcm_rb ringBuffer;
    ma_atomic_bool32 endRequested;
    ma_uint64 capacityInFrames;
};

static ma_data_source_vtable g_manet_pcm_stream_vtable = {
    manet_pcm_stream_on_read,
    NULL,
    manet_pcm_stream_on_get_data_format,
    NULL,
    NULL,
    NULL,
    0
};

static void* manet_alloc(size_t size)
{
    if (size == 0) {
        return NULL;
    }

    return ma_malloc(size, NULL);
}

static void manet_free(void* ptr)
{
    if (ptr == NULL) {
        return;
    }

    ma_free(ptr, NULL);
}

static ma_result manet_validate_engine(manet_engine* handle)
{
    return handle == NULL ? MA_INVALID_OPERATION : MA_SUCCESS;
}

static ma_result manet_validate_context(manet_context* handle)
{
    return handle == NULL ? MA_INVALID_OPERATION : MA_SUCCESS;
}

static ma_result manet_validate_sound(manet_sound* handle)
{
    return handle == NULL ? MA_INVALID_OPERATION : MA_SUCCESS;
}

static ma_result manet_validate_streaming_sound(manet_sound* handle)
{
    if (handle == NULL) {
        return MA_INVALID_OPERATION;
    }

    if (handle->isStreaming == MA_FALSE || handle->stream == NULL) {
        return MA_INVALID_OPERATION;
    }

    return MA_SUCCESS;
}

static manet_sound_state manet_sound_update_state(manet_sound* handle)
{
    if (handle == NULL) {
        return MANET_SOUND_STATE_STOPPED;
    }

    if (ma_sound_is_playing(&handle->sound)) {
        handle->state = MANET_SOUND_STATE_PLAYING;
        return handle->state;
    }

    if (handle->state == MANET_SOUND_STATE_STARTING) {
        if (ma_sound_at_end(&handle->sound)) {
            handle->state = MANET_SOUND_STATE_STOPPED;
        }
        return handle->state;
    }

    if (handle->state == MANET_SOUND_STATE_STOPPING) {
        handle->state = MANET_SOUND_STATE_STOPPED;
        return handle->state;
    }

    handle->state = MANET_SOUND_STATE_STOPPED;
    return handle->state;
}

static void manet_sound_end_callback_trampoline(void* pUserData, ma_sound* pSound)
{
    (void)pSound;

    manet_sound* handle = (manet_sound*)pUserData;
    if (handle == NULL) {
        return;
    }

    if (handle->managedEndCallback != NULL) {
        handle->managedEndCallback(handle, handle->managedEndUserData);
    }
}

#if !defined(MA_NO_DEVICE_IO)
static void manet_capture_device_data_callback(ma_device* pDevice, void* pOutput, const void* pInput, ma_uint32 frameCount)
{
    (void)pOutput;

    if (pDevice == NULL) {
        return;
    }

    manet_capture_device* handle = (manet_capture_device*)pDevice->pUserData;
    if (handle == NULL || handle->callback == NULL || pInput == NULL) {
        return;
    }

    handle->callback((const float*)pInput, frameCount, handle->channelCount, handle->userData);
}
#endif

static manet_pcm_stream* manet_pcm_stream_create(ma_uint32 channels, ma_uint32 sampleRate, ma_uint32 capacityInFrames)
{
    if (channels == 0 || sampleRate == 0 || capacityInFrames == 0) {
        return NULL;
    }

    manet_pcm_stream* stream = (manet_pcm_stream*)manet_alloc(sizeof(*stream));
    if (stream == NULL) {
        return NULL;
    }

    memset(stream, 0, sizeof(*stream));

    ma_result result = ma_pcm_rb_init(ma_format_f32, channels, capacityInFrames, NULL, NULL, &stream->ringBuffer);
    if (result != MA_SUCCESS) {
        manet_free(stream);
        return NULL;
    }

    stream->capacityInFrames = capacityInFrames;
    stream->ringBuffer.sampleRate = sampleRate;
    ma_atomic_bool32_set(&stream->endRequested, MA_FALSE);

    ma_data_source_config config = ma_data_source_config_init();
    config.vtable = &g_manet_pcm_stream_vtable;

    result = ma_data_source_init(&config, &stream->ds);
    if (result != MA_SUCCESS) {
        ma_pcm_rb_uninit(&stream->ringBuffer);
        manet_free(stream);
        return NULL;
    }

    return stream;
}

static void manet_pcm_stream_destroy(manet_pcm_stream* stream)
{
    if (stream == NULL) {
        return;
    }

    ma_data_source_uninit((ma_data_source*)&stream->ds);
    ma_pcm_rb_uninit(&stream->ringBuffer);
    manet_free(stream);
}

static ma_uint64 manet_pcm_stream_capacity(const manet_pcm_stream* stream)
{
    if (stream == NULL) {
        return 0;
    }

    return stream->capacityInFrames;
}

static ma_uint64 manet_pcm_stream_available_read(const manet_pcm_stream* stream)
{
    if (stream == NULL) {
        return 0;
    }

    return ma_pcm_rb_available_read((ma_pcm_rb*)&stream->ringBuffer);
}

static ma_uint64 manet_pcm_stream_available_write(const manet_pcm_stream* stream)
{
    if (stream == NULL) {
        return 0;
    }

    return ma_pcm_rb_available_write((ma_pcm_rb*)&stream->ringBuffer);
}

static ma_result manet_pcm_stream_reset(manet_pcm_stream* stream)
{
    if (stream == NULL) {
        return MA_INVALID_OPERATION;
    }

    ma_pcm_rb_reset(&stream->ringBuffer);
    ma_atomic_bool32_set(&stream->endRequested, MA_FALSE);
    return MA_SUCCESS;
}

static void manet_pcm_stream_mark_end(manet_pcm_stream* stream)
{
    if (stream == NULL) {
        return;
    }

    ma_atomic_bool32_set(&stream->endRequested, MA_TRUE);
}

static void manet_pcm_stream_clear_end(manet_pcm_stream* stream)
{
    if (stream == NULL) {
        return;
    }

    ma_atomic_bool32_set(&stream->endRequested, MA_FALSE);
}

static ma_bool32 manet_pcm_stream_is_end_requested(const manet_pcm_stream* stream)
{
    if (stream == NULL) {
        return MA_FALSE;
    }

    return ma_atomic_bool32_get(&((manet_pcm_stream*)stream)->endRequested);
}

static ma_uint32 manet_pcm_stream_get_channels(const manet_pcm_stream* stream)
{
    if (stream == NULL) {
        return 0;
    }

    return stream->ringBuffer.channels;
}

static ma_uint32 manet_pcm_stream_get_sample_rate(const manet_pcm_stream* stream)
{
    if (stream == NULL) {
        return 0;
    }

    return stream->ringBuffer.sampleRate;
}

static ma_result manet_pcm_stream_append_pcm_frames(manet_pcm_stream* stream, const float* frames, ma_uint64 frameCount, ma_uint64* framesWritten)
{
    if (framesWritten != NULL) {
        *framesWritten = 0;
    }

    if (stream == NULL) {
        return MA_INVALID_OPERATION;
    }

    if (frameCount == 0) {
        return MA_SUCCESS;
    }

    if (frames == NULL) {
        return MA_INVALID_ARGS;
    }

    if (manet_pcm_stream_is_end_requested(stream)) {
        return MA_INVALID_OPERATION;
    }

    ma_pcm_rb* rb = &stream->ringBuffer;
    ma_uint64 totalWritten = 0;

    while (totalWritten < frameCount) {
        ma_uint64 framesRemaining = frameCount - totalWritten;
        if (framesRemaining == 0) {
            break;
        }

        ma_uint32 chunk = (framesRemaining > 0xFFFFFFFF) ? 0xFFFFFFFF : (ma_uint32)framesRemaining;

        ma_uint32 available = ma_pcm_rb_available_write(rb);
        if (available == 0) {
            break;
        }

        if (chunk > available) {
            chunk = available;
        }

        if (chunk == 0) {
            break;
        }

        ma_uint32 mappedFrameCount = chunk;
        void* mappedBuffer = NULL;
        ma_result result = ma_pcm_rb_acquire_write(rb, &mappedFrameCount, &mappedBuffer);
        if (result != MA_SUCCESS || mappedFrameCount == 0) {
            break;
        }

        const void* source = ma_offset_pcm_frames_const_ptr(frames, totalWritten, rb->format, rb->channels);
        ma_copy_pcm_frames(mappedBuffer, source, mappedFrameCount, rb->format, rb->channels);

        result = ma_pcm_rb_commit_write(rb, mappedFrameCount);
        if (result != MA_SUCCESS) {
            totalWritten += mappedFrameCount;
            break;
        }

        totalWritten += mappedFrameCount;
    }

    if (framesWritten != NULL) {
        *framesWritten = totalWritten;
    }

    return MA_SUCCESS;
}

static ma_result manet_pcm_stream_on_read(ma_data_source* pDataSource, void* pFramesOut, ma_uint64 frameCount, ma_uint64* pFramesRead)
{
    manet_pcm_stream* stream = (manet_pcm_stream*)pDataSource;
    if (pFramesRead != NULL) {
        *pFramesRead = 0;
    }

    if (stream == NULL || frameCount == 0) {
        return MA_SUCCESS;
    }

    ma_pcm_rb* rb = &stream->ringBuffer;
    ma_uint64 totalFramesRead = 0;

    while (totalFramesRead < frameCount) {
        ma_uint64 framesToRead = frameCount - totalFramesRead;
        if (framesToRead > 0xFFFFFFFF) {
            framesToRead = 0xFFFFFFFF;
        }

        ma_uint32 mappedFrameCount = (ma_uint32)framesToRead;
        void* mappedBuffer = NULL;
        ma_result result = ma_pcm_rb_acquire_read(rb, &mappedFrameCount, &mappedBuffer);
        if (result != MA_SUCCESS) {
            break;
        }

        if (mappedFrameCount == 0) {
            break;
        }

        if (pFramesOut != NULL) {
            void* dst = ma_offset_pcm_frames_ptr(pFramesOut, totalFramesRead, rb->format, rb->channels);
            ma_copy_pcm_frames(dst, mappedBuffer, mappedFrameCount, rb->format, rb->channels);
        }

        result = ma_pcm_rb_commit_read(rb, mappedFrameCount);
        if (result != MA_SUCCESS) {
            break;
        }

        totalFramesRead += mappedFrameCount;
    }

    if (totalFramesRead == 0) {
        if (manet_pcm_stream_is_end_requested(stream) && ma_pcm_rb_available_read(rb) == 0) {
            return MA_AT_END;
        }

        if (pFramesOut != NULL) {
            ma_silence_pcm_frames(pFramesOut, frameCount, rb->format, rb->channels);
        }

        if (pFramesRead != NULL) {
            *pFramesRead = frameCount;
        }

        return MA_SUCCESS;
    }

    if (totalFramesRead < frameCount) {
        if (!manet_pcm_stream_is_end_requested(stream) || ma_pcm_rb_available_read(rb) != 0) {
            if (pFramesOut != NULL) {
                ma_silence_pcm_frames(
                    ma_offset_pcm_frames_ptr(pFramesOut, totalFramesRead, rb->format, rb->channels),
                    frameCount - totalFramesRead,
                    rb->format,
                    rb->channels);
            }

            totalFramesRead = frameCount;
        }
    }

    if (pFramesRead != NULL) {
        *pFramesRead = totalFramesRead;
    }

    return MA_SUCCESS;
}

static ma_result manet_pcm_stream_on_get_data_format(ma_data_source* pDataSource, ma_format* pFormat, ma_uint32* pChannels, ma_uint32* pSampleRate, ma_channel* pChannelMap, size_t channelMapCap)
{
    manet_pcm_stream* stream = (manet_pcm_stream*)pDataSource;
    if (stream == NULL) {
        return MA_INVALID_OPERATION;
    }

    if (pFormat != NULL) {
        *pFormat = stream->ringBuffer.format;
    }

    if (pChannels != NULL) {
        *pChannels = stream->ringBuffer.channels;
    }

    if (pSampleRate != NULL) {
        *pSampleRate = stream->ringBuffer.sampleRate;
    }

    if (pChannelMap != NULL) {
        ma_channel_map_init_standard(ma_standard_channel_map_default, pChannelMap, channelMapCap, stream->ringBuffer.channels);
    }

    return MA_SUCCESS;
}

MANET_API manet_engine* manet_engine_create_default(void)
{
    return manet_engine_create_with_config(NULL);
}

MANET_API void manet_engine_destroy(manet_engine* handle)
{
    if (handle == NULL) {
        return;
    }

    ma_engine_uninit(&handle->engine);
    manet_free(handle);
}

MANET_API ma_result manet_engine_start(manet_engine* handle)
{
    if (manet_validate_engine(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    return ma_engine_start(&handle->engine);
}

MANET_API ma_result manet_engine_stop(manet_engine* handle)
{
    if (manet_validate_engine(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    return ma_engine_stop(&handle->engine);
}

MANET_API ma_uint64 manet_engine_get_time_in_pcm_frames(manet_engine* handle)
{
    if (manet_validate_engine(handle) != MA_SUCCESS) {
        return 0;
    }

    return ma_engine_get_time_in_pcm_frames(&handle->engine);
}

MANET_API ma_uint64 manet_engine_get_time_in_milliseconds(manet_engine* handle)
{
    if (manet_validate_engine(handle) != MA_SUCCESS) {
        return 0;
    }

    return ma_engine_get_time_in_milliseconds(&handle->engine);
}

MANET_API ma_result manet_engine_set_time_in_pcm_frames(manet_engine* handle, ma_uint64 globalTime)
{
    if (manet_validate_engine(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    return ma_engine_set_time_in_pcm_frames(&handle->engine, globalTime);
}

MANET_API ma_result manet_engine_set_time_in_milliseconds(manet_engine* handle, ma_uint64 globalTime)
{
    if (manet_validate_engine(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    return ma_engine_set_time_in_milliseconds(&handle->engine, globalTime);
}

MANET_API ma_result manet_engine_set_volume(manet_engine* handle, float volume)
{
    if (manet_validate_engine(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    return ma_engine_set_volume(&handle->engine, volume);
}

MANET_API float manet_engine_get_volume(manet_engine* handle)
{
    if (manet_validate_engine(handle) != MA_SUCCESS) {
        return 0.0f;
    }

    return ma_engine_get_volume(&handle->engine);
}

MANET_API ma_result manet_engine_set_gain_db(manet_engine* handle, float gainDB)
{
    if (manet_validate_engine(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    return ma_engine_set_gain_db(&handle->engine, gainDB);
}

MANET_API float manet_engine_get_gain_db(manet_engine* handle)
{
    if (manet_validate_engine(handle) != MA_SUCCESS) {
        return 0.0f;
    }

    return ma_engine_get_gain_db(&handle->engine);
}

MANET_API ma_uint32 manet_engine_get_sample_rate(manet_engine* handle)
{
    if (manet_validate_engine(handle) != MA_SUCCESS) {
        return 0;
    }

    return ma_engine_get_sample_rate(&handle->engine);
}

MANET_API ma_uint32 manet_engine_get_channels(manet_engine* handle)
{
    if (manet_validate_engine(handle) != MA_SUCCESS) {
        return 0;
    }

    return ma_engine_get_channels(&handle->engine);
}

MANET_API ma_uint32 manet_engine_get_listener_count(manet_engine* handle)
{
    if (manet_validate_engine(handle) != MA_SUCCESS) {
        return 0;
    }

    return ma_engine_get_listener_count(&handle->engine);
}

MANET_API ma_uint32 manet_engine_find_closest_listener(manet_engine* handle, float x, float y, float z)
{
    if (manet_validate_engine(handle) != MA_SUCCESS) {
        return MA_LISTENER_INDEX_CLOSEST;
    }

    return ma_engine_find_closest_listener(&handle->engine, x, y, z);
}

MANET_API ma_result manet_engine_play_sound(manet_engine* handle, const char* path)
{
    if (manet_validate_engine(handle) != MA_SUCCESS || path == NULL) {
        return MA_INVALID_OPERATION;
    }

    return ma_engine_play_sound(&handle->engine, path, NULL);
}

MANET_API ma_result manet_engine_set_listener_position(manet_engine* handle, ma_uint32 index, float x, float y, float z)
{
    if (manet_validate_engine(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    ma_engine_listener_set_position(&handle->engine, index, x, y, z);
    return MA_SUCCESS;
}

MANET_API ma_result manet_engine_get_listener_position(manet_engine* handle, ma_uint32 index, float* x, float* y, float* z)
{
    if (manet_validate_engine(handle) != MA_SUCCESS || x == NULL || y == NULL || z == NULL) {
        return MA_INVALID_OPERATION;
    }

    ma_vec3f position = ma_engine_listener_get_position(&handle->engine, index);
    *x = position.x;
    *y = position.y;
    *z = position.z;

    return MA_SUCCESS;
}

MANET_API ma_result manet_engine_set_listener_direction(manet_engine* handle, ma_uint32 index, float x, float y, float z)
{
    if (manet_validate_engine(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    ma_engine_listener_set_direction(&handle->engine, index, x, y, z);
    return MA_SUCCESS;
}

MANET_API ma_result manet_engine_get_listener_direction(manet_engine* handle, ma_uint32 index, float* x, float* y, float* z)
{
    if (manet_validate_engine(handle) != MA_SUCCESS || x == NULL || y == NULL || z == NULL) {
        return MA_INVALID_OPERATION;
    }

    ma_vec3f direction = ma_engine_listener_get_direction(&handle->engine, index);
    *x = direction.x;
    *y = direction.y;
    *z = direction.z;

    return MA_SUCCESS;
}

MANET_API ma_result manet_engine_set_listener_world_up(manet_engine* handle, ma_uint32 index, float x, float y, float z)
{
    if (manet_validate_engine(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    ma_engine_listener_set_world_up(&handle->engine, index, x, y, z);
    return MA_SUCCESS;
}

MANET_API ma_result manet_engine_get_listener_world_up(manet_engine* handle, ma_uint32 index, float* x, float* y, float* z)
{
    if (manet_validate_engine(handle) != MA_SUCCESS || x == NULL || y == NULL || z == NULL) {
        return MA_INVALID_OPERATION;
    }

    ma_vec3f up = ma_engine_listener_get_world_up(&handle->engine, index);
    *x = up.x;
    *y = up.y;
    *z = up.z;

    return MA_SUCCESS;
}

MANET_API ma_result manet_engine_set_listener_velocity(manet_engine* handle, ma_uint32 index, float x, float y, float z)
{
    if (manet_validate_engine(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    ma_engine_listener_set_velocity(&handle->engine, index, x, y, z);
    return MA_SUCCESS;
}

MANET_API ma_result manet_engine_get_listener_velocity(manet_engine* handle, ma_uint32 index, float* x, float* y, float* z)
{
    if (manet_validate_engine(handle) != MA_SUCCESS || x == NULL || y == NULL || z == NULL) {
        return MA_INVALID_OPERATION;
    }

    ma_vec3f velocity = ma_engine_listener_get_velocity(&handle->engine, index);
    *x = velocity.x;
    *y = velocity.y;
    *z = velocity.z;

    return MA_SUCCESS;
}

MANET_API ma_result manet_engine_set_listener_cone(manet_engine* handle, ma_uint32 index, float innerAngleInRadians, float outerAngleInRadians, float outerGain)
{
    if (manet_validate_engine(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    ma_engine_listener_set_cone(&handle->engine, index, innerAngleInRadians, outerAngleInRadians, outerGain);
    return MA_SUCCESS;
}

MANET_API ma_result manet_engine_get_listener_cone(manet_engine* handle, ma_uint32 index, float* innerAngleInRadians, float* outerAngleInRadians, float* outerGain)
{
    if (manet_validate_engine(handle) != MA_SUCCESS || innerAngleInRadians == NULL || outerAngleInRadians == NULL || outerGain == NULL) {
        return MA_INVALID_OPERATION;
    }

    ma_engine_listener_get_cone(&handle->engine, index, innerAngleInRadians, outerAngleInRadians, outerGain);
    return MA_SUCCESS;
}

MANET_API ma_result manet_engine_set_listener_enabled(manet_engine* handle, ma_uint32 index, ma_bool32 isEnabled)
{
    if (manet_validate_engine(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    ma_engine_listener_set_enabled(&handle->engine, index, isEnabled);
    return MA_SUCCESS;
}

MANET_API ma_bool32 manet_engine_is_listener_enabled(manet_engine* handle, ma_uint32 index)
{
    if (manet_validate_engine(handle) != MA_SUCCESS) {
        return MA_FALSE;
    }

    return ma_engine_listener_is_enabled(&handle->engine, index);
}

MANET_API manet_engine* manet_engine_create_with_options(
    manet_context* contextHandle,
    manet_resource_manager* resourceManagerHandle,
    const char* playbackDeviceId,
    ma_uint32 sampleRate,
    ma_uint32 channelCount,
    ma_uint32 periodSizeInFrames,
    ma_uint32 periodSizeInMilliseconds,
    ma_bool32 noAutoStart,
    ma_bool32 noDevice)
{
    ma_engine_config config = ma_engine_config_init();

#if !defined(MA_NO_DEVICE_IO)
    if (contextHandle != NULL) {
        config.pContext = &contextHandle->context;
    }

    ma_device_id playbackId;
    ma_device_id* playbackIdPtr = NULL;
    MA_ZERO_OBJECT(&playbackId);

    if (playbackDeviceId != NULL && playbackDeviceId[0] != '\0') {
        if (manet_device_id_from_hex(playbackDeviceId, &playbackId) == MA_FALSE) {
            return NULL;
        }

        playbackIdPtr = &playbackId;
    }

    config.pPlaybackDeviceID = playbackIdPtr;
#else
    (void)playbackDeviceId;
    (void)contextHandle;
#endif
    if (resourceManagerHandle != NULL) {
        config.pResourceManager = &resourceManagerHandle->manager;
    }

    if (sampleRate != 0) {
        config.sampleRate = sampleRate;
    }

    if (channelCount != 0) {
        config.channels = channelCount;
    }

    if (periodSizeInFrames != 0) {
        config.periodSizeInFrames = periodSizeInFrames;
    }

    if (periodSizeInMilliseconds != 0) {
        config.periodSizeInMilliseconds = periodSizeInMilliseconds;
    }

    config.noAutoStart = noAutoStart;
    config.noDevice = noDevice;

    return manet_engine_create_with_config(&config);
}

MANET_API manet_resource_manager* manet_resource_manager_create_with_config(const manet_resource_manager_config_simple* settings)
{
    manet_resource_manager* handle = (manet_resource_manager*)manet_alloc(sizeof(*handle));
    if (handle == NULL) {
        return NULL;
    }

    ma_resource_manager_config config = ma_resource_manager_config_init();
    manet_apply_resource_manager_settings(&config, settings);

    ma_result result = ma_resource_manager_init(&config, &handle->manager);
    if (result != MA_SUCCESS) {
        manet_free(handle);
        return NULL;
    }

    return handle;
}

MANET_API manet_resource_manager* manet_resource_manager_create_default(void)
{
    return manet_resource_manager_create_with_config(NULL);
}

MANET_API void manet_resource_manager_destroy(manet_resource_manager* handle)
{
    if (handle == NULL) {
        return;
    }

    ma_resource_manager_uninit(&handle->manager);
    manet_free(handle);
}

MANET_API manet_context* manet_context_create_default(void)
{
    manet_context* handle = (manet_context*)manet_alloc(sizeof(*handle));
    if (handle == NULL) {
        return NULL;
    }

    ma_context_config config = ma_context_config_init();
    ma_result result = ma_context_init(NULL, 0, &config, &handle->context);
    if (result != MA_SUCCESS) {
        manet_free(handle);
        return NULL;
    }

    return handle;
}

MANET_API manet_context* manet_context_create_with_backends(const ma_backend* backends, ma_uint32 backendCount)
{
    manet_context* handle = (manet_context*)manet_alloc(sizeof(*handle));
    if (handle == NULL) {
        return NULL;
    }

    ma_context_config config = ma_context_config_init();

    const ma_backend* backendList = NULL;
    ma_uint32 listCount = 0;
    if (backends != NULL && backendCount > 0) {
        backendList = backends;
        listCount = backendCount;
    }

    ma_result result = ma_context_init(backendList, listCount, &config, &handle->context);
    if (result != MA_SUCCESS) {
        manet_free(handle);
        return NULL;
    }

    return handle;
}

MANET_API void manet_context_destroy(manet_context* handle)
{
    if (handle == NULL) {
        return;
    }

    ma_context_uninit(&handle->context);
    manet_free(handle);
}

MANET_API ma_result manet_context_get_devices(
    manet_context* handle,
    ma_device_type deviceType,
    manet_device_descriptor* descriptors,
    ma_uint32 descriptorCapacity,
    ma_uint32* outDeviceCount)
{
    if (manet_validate_context(handle) != MA_SUCCESS) {
        if (outDeviceCount != NULL) {
            *outDeviceCount = 0;
        }

        return MA_INVALID_OPERATION;
    }

    ma_device_info* playbackInfos = NULL;
    ma_device_info* captureInfos = NULL;
    ma_uint32 playbackCount = 0;
    ma_uint32 captureCount = 0;
    ma_result result = ma_context_get_devices(&handle->context, &playbackInfos, &playbackCount, &captureInfos, &captureCount);
    if (result != MA_SUCCESS) {
        if (outDeviceCount != NULL) {
            *outDeviceCount = 0;
        }

        return result;
    }

    ma_device_info* sourceInfos = playbackInfos;
    ma_uint32 sourceCount = playbackCount;
    if (deviceType == ma_device_type_capture) {
        sourceInfos = captureInfos;
        sourceCount = captureCount;
    }

    if (outDeviceCount != NULL) {
        *outDeviceCount = sourceCount;
    }

    if (descriptors == NULL || descriptorCapacity == 0 || sourceInfos == NULL) {
        return MA_SUCCESS;
    }

    ma_uint32 copyCount = manet_min_u32(descriptorCapacity, sourceCount);
    for (ma_uint32 i = 0; i < copyCount; ++i) {
        manet_write_device_descriptor(&descriptors[i], &sourceInfos[i], deviceType);
    }

    return MA_SUCCESS;
}

MANET_API manet_sound* manet_sound_create_from_file(manet_engine* engineHandle, const char* path, ma_uint32 flags)
{
    if (manet_validate_engine(engineHandle) != MA_SUCCESS || path == NULL) {
        return NULL;
    }

    manet_sound* soundHandle = (manet_sound*)manet_alloc(sizeof(*soundHandle));
    if (soundHandle == NULL) {
        return NULL;
    }

    memset(soundHandle, 0, sizeof(*soundHandle));

    ma_result result = ma_sound_init_from_file(&engineHandle->engine, path, flags, NULL, NULL, &soundHandle->sound);
    if (result != MA_SUCCESS) {
        manet_free(soundHandle);
        return NULL;
    }

    soundHandle->state = MANET_SOUND_STATE_STOPPED;
    return soundHandle;
}

#if defined(_WIN32)
MANET_API manet_sound* manet_sound_create_from_file_w(manet_engine* engineHandle, const wchar_t* path, ma_uint32 flags)
{
    if (manet_validate_engine(engineHandle) != MA_SUCCESS || path == NULL) {
        return NULL;
    }

    manet_sound* soundHandle = (manet_sound*)manet_alloc(sizeof(*soundHandle));
    if (soundHandle == NULL) {
        return NULL;
    }

    memset(soundHandle, 0, sizeof(*soundHandle));

    ma_result result = ma_sound_init_from_file_w(&engineHandle->engine, path, flags, NULL, NULL, &soundHandle->sound);
    if (result != MA_SUCCESS) {
        manet_free(soundHandle);
        return NULL;
    }

    soundHandle->state = MANET_SOUND_STATE_STOPPED;
    return soundHandle;
}
#endif

static void manet_copy_string(char* dst, size_t dstSize, const char* src)
{
    if (dst == NULL || dstSize == 0) {
        return;
    }

    if (src == NULL) {
        dst[0] = '\0';
        return;
    }

    strncpy(dst, src, dstSize - 1);
    dst[dstSize - 1] = '\0';
}

static void manet_device_id_to_hex(const ma_device_id* id, char* buffer, size_t bufferSize)
{
    static const char HEX_DIGITS[] = "0123456789abcdef";

    if (buffer == NULL || bufferSize == 0) {
        return;
    }

    if (id == NULL || bufferSize < MANET_DEVICE_ID_HEX_BUFFER_SIZE) {
        buffer[0] = '\0';
        return;
    }

    const ma_uint8* bytes = (const ma_uint8*)id;
    for (size_t i = 0; i < sizeof(*id); ++i) {
        buffer[i * 2] = HEX_DIGITS[(bytes[i] >> 4) & 0x0F];
        buffer[i * 2 + 1] = HEX_DIGITS[bytes[i] & 0x0F];
    }

    buffer[MANET_DEVICE_ID_HEX_LENGTH] = '\0';
}

static int manet_hex_value(char digit)
{
    if (digit >= '0' && digit <= '9') {
        return digit - '0';
    }

    if (digit >= 'a' && digit <= 'f') {
        return 10 + (digit - 'a');
    }

    if (digit >= 'A' && digit <= 'F') {
        return 10 + (digit - 'A');
    }

    return -1;
}

static ma_bool32 manet_device_id_from_hex(const char* hex, ma_device_id* id)
{
    if (hex == NULL || id == NULL) {
        return MA_FALSE;
    }

    size_t length = strlen(hex);
    if (length != MANET_DEVICE_ID_HEX_LENGTH) {
        return MA_FALSE;
    }

    ma_uint8* bytes = (ma_uint8*)id;
    for (size_t i = 0; i < sizeof(*id); ++i) {
        int high = manet_hex_value(hex[i * 2]);
        int low = manet_hex_value(hex[i * 2 + 1]);
        if (high < 0 || low < 0) {
            return MA_FALSE;
        }

        bytes[i] = (ma_uint8)((high << 4) | low);
    }

    return MA_TRUE;
}

static void manet_write_device_descriptor(manet_device_descriptor* dst, const ma_device_info* src, ma_device_type type)
{
    if (dst == NULL || src == NULL) {
        return;
    }

    dst->type = type;
    dst->isDefault = src->isDefault;
    manet_copy_string(dst->name, sizeof(dst->name), src->name);
    manet_device_id_to_hex(&src->id, dst->id, sizeof(dst->id));
}

static ma_uint32 manet_min_u32(ma_uint32 a, ma_uint32 b)
{
    return (a < b) ? a : b;
}

static void manet_apply_resource_manager_settings(
    ma_resource_manager_config* config,
    const manet_resource_manager_config_simple* settings)
{
    if (config == NULL || settings == NULL) {
        return;
    }

    if (settings->flags != 0) {
        config->flags = settings->flags;
    }

    if (settings->decodedFormat != 0) {
        config->decodedFormat = (ma_format)settings->decodedFormat;
    }

    if (settings->decodedChannels != 0) {
        config->decodedChannels = settings->decodedChannels;
    }

    if (settings->decodedSampleRate != 0) {
        config->decodedSampleRate = settings->decodedSampleRate;
    }

    if (settings->jobThreadCount != 0) {
        config->jobThreadCount = settings->jobThreadCount;
    }
}

static manet_engine* manet_engine_create_with_config(const ma_engine_config* inputConfig)
{
    ma_engine_config config;
    if (inputConfig != NULL) {
        config = *inputConfig;
    } else {
        config = ma_engine_config_init();
    }

    manet_engine* handle = (manet_engine*)manet_alloc(sizeof(*handle));
    if (handle == NULL) {
        return NULL;
    }

    ma_result result = ma_engine_init(&config, &handle->engine);
    if (result != MA_SUCCESS) {
#if defined(_DEBUG)
        fprintf(stderr, "[manet] ma_engine_init failed: %d (%s)\n", result, ma_result_description(result));
#endif
        manet_free(handle);
        return NULL;
    }

    return handle;
}

MANET_API manet_sound* manet_sound_create_from_pcm_frames(manet_engine* engineHandle, const float* frames, ma_uint64 frameCount, ma_uint32 channels, ma_uint32 sampleRate, ma_uint32 flags)
{
    if (manet_validate_engine(engineHandle) != MA_SUCCESS || frames == NULL || channels == 0 || sampleRate == 0) {
        return NULL;
    }

    manet_sound* soundHandle = (manet_sound*)manet_alloc(sizeof(*soundHandle));
    if (soundHandle == NULL) {
        return NULL;
    }

    memset(soundHandle, 0, sizeof(*soundHandle));

    ma_audio_buffer_config bufferConfig = ma_audio_buffer_config_init(ma_format_f32, channels, frameCount, frames, NULL);
    bufferConfig.sampleRate = sampleRate;
    ma_result result = ma_audio_buffer_init_copy(&bufferConfig, &soundHandle->audioBuffer);
    if (result != MA_SUCCESS) {
        manet_free(soundHandle);
        return NULL;
    }

    soundHandle->ownsAudioBuffer = MA_TRUE;

    result = ma_sound_init_from_data_source(&engineHandle->engine, (ma_data_source*)&soundHandle->audioBuffer, flags, NULL, &soundHandle->sound);
    if (result != MA_SUCCESS) {
        ma_audio_buffer_uninit(&soundHandle->audioBuffer);
        manet_free(soundHandle);
        return NULL;
    }

    if ((flags & MA_SOUND_FLAG_LOOPING) != 0) {
        ma_data_source_set_looping((ma_data_source*)&soundHandle->audioBuffer, MA_TRUE);
    }

    soundHandle->state = MANET_SOUND_STATE_STOPPED;
    return soundHandle;
}

MANET_API manet_sound* manet_sound_create_streaming(manet_engine* engineHandle, ma_uint32 channels, ma_uint32 sampleRate, ma_uint32 capacityInFrames, ma_uint32 flags)
{
    if (manet_validate_engine(engineHandle) != MA_SUCCESS || channels == 0 || sampleRate == 0 || capacityInFrames == 0) {
        return NULL;
    }

    manet_pcm_stream* stream = manet_pcm_stream_create(channels, sampleRate, capacityInFrames);
    if (stream == NULL) {
        return NULL;
    }

    manet_sound* soundHandle = (manet_sound*)manet_alloc(sizeof(*soundHandle));
    if (soundHandle == NULL) {
        manet_pcm_stream_destroy(stream);
        return NULL;
    }

    memset(soundHandle, 0, sizeof(*soundHandle));

    ma_result result = ma_sound_init_from_data_source(&engineHandle->engine, (ma_data_source*)&stream->ds, flags, NULL, &soundHandle->sound);
    if (result != MA_SUCCESS) {
        manet_pcm_stream_destroy(stream);
        manet_free(soundHandle);
        return NULL;
    }

    if ((flags & MA_SOUND_FLAG_LOOPING) != 0) {
        ma_data_source_set_looping((ma_data_source*)&stream->ds, MA_TRUE);
    }

    soundHandle->state = MANET_SOUND_STATE_STOPPED;
    soundHandle->stream = stream;
    soundHandle->isStreaming = MA_TRUE;
    soundHandle->ownsAudioBuffer = MA_FALSE;
    return soundHandle;
}

MANET_API ma_result manet_sound_stream_append_pcm_frames(manet_sound* handle, const float* frames, ma_uint64 frameCount, ma_uint64* framesWritten)
{
    if (framesWritten != NULL) {
        *framesWritten = 0;
    }

    if (manet_validate_streaming_sound(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    return manet_pcm_stream_append_pcm_frames(handle->stream, frames, frameCount, framesWritten);
}

MANET_API ma_result manet_sound_stream_get_available_write(manet_sound* handle, ma_uint64* availableFrames)
{
    if (availableFrames != NULL) {
        *availableFrames = 0;
    }

    if (manet_validate_streaming_sound(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    if (availableFrames != NULL) {
        *availableFrames = manet_pcm_stream_available_write(handle->stream);
    }

    return MA_SUCCESS;
}

MANET_API ma_result manet_sound_stream_get_queued_frames(manet_sound* handle, ma_uint64* queuedFrames)
{
    if (queuedFrames != NULL) {
        *queuedFrames = 0;
    }

    if (manet_validate_streaming_sound(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    if (queuedFrames != NULL) {
        *queuedFrames = manet_pcm_stream_available_read(handle->stream);
    }

    return MA_SUCCESS;
}

MANET_API ma_uint64 manet_sound_stream_get_capacity_in_frames(manet_sound* handle)
{
    if (manet_validate_streaming_sound(handle) != MA_SUCCESS) {
        return 0;
    }

    return manet_pcm_stream_capacity(handle->stream);
}

MANET_API ma_result manet_sound_stream_mark_end(manet_sound* handle)
{
    if (manet_validate_streaming_sound(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    manet_pcm_stream_mark_end(handle->stream);
    return MA_SUCCESS;
}

MANET_API ma_result manet_sound_stream_clear_end(manet_sound* handle)
{
    if (manet_validate_streaming_sound(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    manet_pcm_stream_clear_end(handle->stream);
    return MA_SUCCESS;
}

MANET_API ma_bool32 manet_sound_stream_is_end(manet_sound* handle)
{
    if (manet_validate_streaming_sound(handle) != MA_SUCCESS) {
        return MA_FALSE;
    }

    return manet_pcm_stream_is_end_requested(handle->stream);
}

MANET_API ma_result manet_sound_stream_reset(manet_sound* handle)
{
    if (manet_validate_streaming_sound(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    ma_result result = manet_pcm_stream_reset(handle->stream);
    if (result != MA_SUCCESS) {
        return result;
    }

    ma_sound_seek_to_pcm_frame(&handle->sound, 0);
    return MA_SUCCESS;
}

MANET_API ma_uint32 manet_sound_stream_get_channels(manet_sound* handle)
{
    if (manet_validate_streaming_sound(handle) != MA_SUCCESS) {
        return 0;
    }

    return manet_pcm_stream_get_channels(handle->stream);
}

MANET_API ma_uint32 manet_sound_stream_get_sample_rate(manet_sound* handle)
{
    if (manet_validate_streaming_sound(handle) != MA_SUCCESS) {
        return 0;
    }

    return manet_pcm_stream_get_sample_rate(handle->stream);
}

MANET_API void manet_sound_destroy(manet_sound* handle)
{
    if (handle == NULL) {
        return;
    }

    if (handle->ownsAudioBuffer) {
        ma_audio_buffer_uninit(&handle->audioBuffer);
    }

    ma_sound_uninit(&handle->sound);

    if (handle->isStreaming == MA_TRUE && handle->stream != NULL) {
        manet_pcm_stream_destroy(handle->stream);
        handle->stream = NULL;
    }

    manet_free(handle);
}

MANET_API ma_result manet_sound_start(manet_sound* handle)
{
    if (manet_validate_sound(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    ma_result result = ma_sound_start(&handle->sound);
    if (result == MA_SUCCESS) {
        handle->state = MANET_SOUND_STATE_STARTING;
    }

    return result;
}

MANET_API ma_result manet_sound_stop(manet_sound* handle)
{
    if (manet_validate_sound(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    ma_result result = ma_sound_stop(&handle->sound);
    if (result == MA_SUCCESS) {
        handle->state = MANET_SOUND_STATE_STOPPING;
    }

    return result;
}

MANET_API ma_result manet_sound_set_volume(manet_sound* handle, float volume)
{
    if (manet_validate_sound(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    ma_sound_set_volume(&handle->sound, volume);
    return MA_SUCCESS;
}

MANET_API float manet_sound_get_volume(manet_sound* handle)
{
    if (manet_validate_sound(handle) != MA_SUCCESS) {
        return 0.0f;
    }

    return ma_sound_get_volume(&handle->sound);
}

MANET_API ma_result manet_sound_set_pitch(manet_sound* handle, float pitch)
{
    if (manet_validate_sound(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    ma_sound_set_pitch(&handle->sound, pitch);
    return MA_SUCCESS;
}

MANET_API float manet_sound_get_pitch(manet_sound* handle)
{
    if (manet_validate_sound(handle) != MA_SUCCESS) {
        return 0.0f;
    }

    return ma_sound_get_pitch(&handle->sound);
}

MANET_API ma_result manet_sound_set_pan(manet_sound* handle, float pan)
{
    if (manet_validate_sound(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    ma_sound_set_pan(&handle->sound, pan);
    return MA_SUCCESS;
}

MANET_API float manet_sound_get_pan(manet_sound* handle)
{
    if (manet_validate_sound(handle) != MA_SUCCESS) {
        return 0.0f;
    }

    return ma_sound_get_pan(&handle->sound);
}

MANET_API ma_result manet_sound_set_looping(manet_sound* handle, ma_bool32 isLooping)
{
    if (manet_validate_sound(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    ma_sound_set_looping(&handle->sound, isLooping);
    return MA_SUCCESS;
}

MANET_API ma_bool32 manet_sound_is_looping(manet_sound* handle)
{
    if (manet_validate_sound(handle) != MA_SUCCESS) {
        return MA_FALSE;
    }

    return ma_sound_is_looping(&handle->sound);
}

MANET_API ma_result manet_sound_set_position(manet_sound* handle, float x, float y, float z)
{
    if (manet_validate_sound(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    ma_sound_set_position(&handle->sound, x, y, z);
    return MA_SUCCESS;
}

MANET_API ma_result manet_sound_get_position(manet_sound* handle, float* x, float* y, float* z)
{
    if (manet_validate_sound(handle) != MA_SUCCESS || x == NULL || y == NULL || z == NULL) {
        return MA_INVALID_OPERATION;
    }

    ma_vec3f position = ma_sound_get_position(&handle->sound);
    *x = position.x;
    *y = position.y;
    *z = position.z;

    return MA_SUCCESS;
}

MANET_API ma_result manet_sound_set_direction(manet_sound* handle, float x, float y, float z)
{
    if (manet_validate_sound(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    ma_sound_set_direction(&handle->sound, x, y, z);
    return MA_SUCCESS;
}

MANET_API ma_result manet_sound_get_direction(manet_sound* handle, float* x, float* y, float* z)
{
    if (manet_validate_sound(handle) != MA_SUCCESS || x == NULL || y == NULL || z == NULL) {
        return MA_INVALID_OPERATION;
    }

    ma_vec3f direction = ma_sound_get_direction(&handle->sound);
    *x = direction.x;
    *y = direction.y;
    *z = direction.z;

    return MA_SUCCESS;
}

MANET_API ma_result manet_sound_set_positioning(manet_sound* handle, ma_positioning positioning)
{
    if (manet_validate_sound(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    ma_sound_set_positioning(&handle->sound, positioning);
    return MA_SUCCESS;
}

MANET_API ma_positioning manet_sound_get_positioning(manet_sound* handle)
{
    if (manet_validate_sound(handle) != MA_SUCCESS) {
        return ma_positioning_absolute;
    }

    return ma_sound_get_positioning(&handle->sound);
}

MANET_API ma_result manet_sound_set_fade_in_pcm_frames(manet_sound* handle, float volumeBeg, float volumeEnd, ma_uint64 fadeLengthInFrames)
{
    if (manet_validate_sound(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    ma_sound_set_fade_in_pcm_frames(&handle->sound, volumeBeg, volumeEnd, fadeLengthInFrames);
    return MA_SUCCESS;
}

MANET_API ma_result manet_sound_set_fade_start_in_pcm_frames(
    manet_sound* handle,
    float volumeBeg,
    float volumeEnd,
    ma_uint64 fadeLengthInFrames,
    ma_uint64 absoluteGlobalTimeInFrames)
{
    if (manet_validate_sound(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    ma_sound_set_fade_start_in_pcm_frames(&handle->sound, volumeBeg, volumeEnd, fadeLengthInFrames, absoluteGlobalTimeInFrames);
    return MA_SUCCESS;
}

MANET_API manet_sound_state manet_sound_get_state(manet_sound* handle)
{
    if (manet_validate_sound(handle) != MA_SUCCESS) {
        return MANET_SOUND_STATE_STOPPED;
    }

    return manet_sound_update_state(handle);
}

MANET_API ma_result manet_sound_seek_to_pcm_frame(manet_sound* handle, ma_uint64 frameIndex)
{
    if (manet_validate_sound(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    return ma_sound_seek_to_pcm_frame(&handle->sound, frameIndex);
}

MANET_API ma_result manet_sound_get_length_in_pcm_frames(manet_sound* handle, ma_uint64* length)
{
    if (manet_validate_sound(handle) != MA_SUCCESS || length == NULL) {
        return MA_INVALID_OPERATION;
    }

    return ma_sound_get_length_in_pcm_frames(&handle->sound, length);
}

MANET_API ma_result manet_sound_get_cursor_in_pcm_frames(manet_sound* handle, ma_uint64* cursor)
{
    if (manet_validate_sound(handle) != MA_SUCCESS || cursor == NULL) {
        return MA_INVALID_OPERATION;
    }

    return ma_sound_get_cursor_in_pcm_frames(&handle->sound, cursor);
}

MANET_API ma_result manet_sound_set_start_time_in_pcm_frames(manet_sound* handle, ma_uint64 absoluteGlobalTimeInFrames)
{
    if (manet_validate_sound(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    ma_sound_set_start_time_in_pcm_frames(&handle->sound, absoluteGlobalTimeInFrames);
    return MA_SUCCESS;
}

MANET_API ma_result manet_sound_set_stop_time_in_pcm_frames(manet_sound* handle, ma_uint64 absoluteGlobalTimeInFrames)
{
    if (manet_validate_sound(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    ma_sound_set_stop_time_in_pcm_frames(&handle->sound, absoluteGlobalTimeInFrames);
    return MA_SUCCESS;
}

MANET_API ma_result manet_sound_set_stop_time_with_fade_in_pcm_frames(
    manet_sound* handle,
    ma_uint64 stopAbsoluteGlobalTimeInFrames,
    ma_uint64 fadeLengthInFrames)
{
    if (manet_validate_sound(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    ma_sound_set_stop_time_with_fade_in_pcm_frames(&handle->sound, stopAbsoluteGlobalTimeInFrames, fadeLengthInFrames);
    return MA_SUCCESS;
}

MANET_API ma_result manet_sound_set_end_callback(manet_sound* handle, manet_sound_end_proc callback, void* userData)
{
    if (manet_validate_sound(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    handle->managedEndCallback = callback;
    handle->managedEndUserData = userData;

    if (callback == NULL) {
        return ma_sound_set_end_callback(&handle->sound, NULL, NULL);
    }

    return ma_sound_set_end_callback(&handle->sound, manet_sound_end_callback_trampoline, handle);
}

MANET_API ma_uint32 manet_sound_get_sample_rate(manet_sound* handle)
{
    if (manet_validate_sound(handle) != MA_SUCCESS) {
        return 0;
    }

    ma_uint32 sampleRate = 0;
    if (ma_sound_get_data_format(&handle->sound, NULL, NULL, &sampleRate, NULL, 0) != MA_SUCCESS) {
        return 0;
    }

    return sampleRate;
}

MANET_API manet_capture_device* manet_capture_device_create(
    manet_context* contextHandle,
    const char* captureDeviceId,
    ma_uint32 sampleRate,
    ma_uint32 channelCount,
    manet_capture_device_proc callback,
    void* userData)
{
#if defined(MA_NO_DEVICE_IO)
    (void)contextHandle;
    (void)captureDeviceId;
    (void)sampleRate;
    (void)channelCount;
    (void)callback;
    (void)userData;
    return NULL;
#else
    if (callback == NULL || channelCount == 0) {
        return NULL;
    }

    manet_capture_device* handle = (manet_capture_device*)manet_alloc(sizeof(*handle));
    if (handle == NULL) {
        return NULL;
    }

    memset(handle, 0, sizeof(*handle));

    ma_device_config config = ma_device_config_init(ma_device_type_capture);
    config.capture.format = ma_format_f32;
    config.capture.channels = channelCount;
    if (sampleRate != 0) {
        config.sampleRate = sampleRate;
    }

    config.dataCallback = manet_capture_device_data_callback;
    config.pUserData = handle;

    ma_device_id captureId;
    ma_device_id* captureIdPtr = NULL;
    MA_ZERO_OBJECT(&captureId);

    if (captureDeviceId != NULL && captureDeviceId[0] != '\0') {
        if (manet_device_id_from_hex(captureDeviceId, &captureId) == MA_FALSE) {
            manet_free(handle);
            return NULL;
        }

        captureIdPtr = &captureId;
    }

    config.capture.pDeviceID = captureIdPtr;

    ma_context* pContext = NULL;
    if (contextHandle != NULL) {
        pContext = &contextHandle->context;
    }

    ma_result result = ma_device_init(pContext, &config, &handle->device);
    if (result != MA_SUCCESS) {
        manet_free(handle);
        return NULL;
    }

    handle->callback = callback;
    handle->userData = userData;
    handle->channelCount = config.capture.channels;

    return handle;
#endif
}

MANET_API ma_result manet_capture_device_start(manet_capture_device* handle)
{
#if defined(MA_NO_DEVICE_IO)
    (void)handle;
    return MA_INVALID_OPERATION;
#else
    if (handle == NULL) {
        return MA_INVALID_OPERATION;
    }

    return ma_device_start(&handle->device);
#endif
}

MANET_API ma_result manet_capture_device_stop(manet_capture_device* handle)
{
#if defined(MA_NO_DEVICE_IO)
    (void)handle;
    return MA_INVALID_OPERATION;
#else
    if (handle == NULL) {
        return MA_INVALID_OPERATION;
    }

    return ma_device_stop(&handle->device);
#endif
}

MANET_API void manet_capture_device_destroy(manet_capture_device* handle)
{
#if defined(MA_NO_DEVICE_IO)
    (void)handle;
#else
    if (handle == NULL) {
        return;
    }

    ma_device_uninit(&handle->device);
    manet_free(handle);
#endif
}

MANET_API const char* manet_result_description(ma_result result)
{
    return ma_result_description(result);
}
