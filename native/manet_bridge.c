#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>

#define MINIAUDIO_IMPLEMENTATION
#include "miniaudio.h"

#if defined(_WIN32)
#define MANET_API __declspec(dllexport)
#else
#define MANET_API __attribute__((visibility("default")))
#endif

typedef struct manet_engine {
    ma_engine engine;
} manet_engine;

typedef struct manet_sound {
    ma_sound sound;
} manet_sound;

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

static ma_result manet_validate_sound(manet_sound* handle)
{
    return handle == NULL ? MA_INVALID_OPERATION : MA_SUCCESS;
}

MANET_API manet_engine* manet_engine_create_default(void)
{
    manet_engine* handle = (manet_engine*)manet_alloc(sizeof(*handle));
    if (handle == NULL) {
        return NULL;
    }

    ma_engine_config config = ma_engine_config_init();
    ma_result result = ma_engine_init(&config, &handle->engine);
    if (result != MA_SUCCESS) {
        manet_free(handle);
        return NULL;
    }

    return handle;
}

MANET_API void manet_engine_destroy(manet_engine* handle)
{
    if (handle == NULL) {
        return;
    }

    ma_engine_uninit(&handle->engine);
    manet_free(handle);
}

MANET_API ma_result manet_engine_stop(manet_engine* handle)
{
    if (manet_validate_engine(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    ma_engine_stop(&handle->engine);
    return MA_SUCCESS;
}

MANET_API ma_uint64 manet_engine_get_time_in_pcm_frames(manet_engine* handle)
{
    if (manet_validate_engine(handle) != MA_SUCCESS) {
        return 0;
    }

    return ma_engine_get_time_in_pcm_frames(&handle->engine);
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

MANET_API ma_uint32 manet_engine_get_sample_rate(manet_engine* handle)
{
    if (manet_validate_engine(handle) != MA_SUCCESS) {
        return 0;
    }

    return ma_engine_get_sample_rate(&handle->engine);
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

    return ma_engine_listener_set_position(&handle->engine, index, x, y, z);
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

    ma_result result = ma_sound_init_from_file(&engineHandle->engine, path, flags, NULL, NULL, &soundHandle->sound);
    if (result != MA_SUCCESS) {
        manet_free(soundHandle);
        return NULL;
    }

    return soundHandle;
}

MANET_API void manet_sound_destroy(manet_sound* handle)
{
    if (handle == NULL) {
        return;
    }

    ma_sound_uninit(&handle->sound);
    manet_free(handle);
}

MANET_API ma_result manet_sound_start(manet_sound* handle)
{
    if (manet_validate_sound(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    return ma_sound_start(&handle->sound);
}

MANET_API ma_result manet_sound_stop(manet_sound* handle)
{
    if (manet_validate_sound(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    return ma_sound_stop(&handle->sound);
}

MANET_API ma_result manet_sound_set_volume(manet_sound* handle, float volume)
{
    if (manet_validate_sound(handle) != MA_SUCCESS) {
        return MA_INVALID_OPERATION;
    }

    return ma_sound_set_volume(&handle->sound, volume);
}

MANET_API float manet_sound_get_volume(manet_sound* handle)
{
    if (manet_validate_sound(handle) != MA_SUCCESS) {
        return 0.0f;
    }

    return ma_sound_get_volume(&handle->sound);
}

MANET_API ma_sound_state manet_sound_get_state(manet_sound* handle)
{
    if (manet_validate_sound(handle) != MA_SUCCESS) {
        return MA_SOUND_STATE_STOPPED;
    }

    return ma_sound_get_state(&handle->sound);
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

MANET_API const char* manet_result_description(ma_result result)
{
    return ma_result_description(result);
}
