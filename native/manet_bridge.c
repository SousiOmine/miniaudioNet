#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>
#include <string.h>

#define MINIAUDIO_IMPLEMENTATION
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

typedef struct manet_sound {
    ma_sound sound;
    manet_sound_state state;
    ma_bool32 ownsAudioBuffer;
    ma_audio_buffer audioBuffer;
} manet_sound;

static void manet_copy_string(char* dst, size_t dstSize, const char* src);
static void manet_device_id_to_hex(const ma_device_id* id, char* buffer, size_t bufferSize);
static int manet_hex_value(char digit);
static ma_bool32 manet_device_id_from_hex(const char* hex, ma_device_id* id);
static void manet_write_device_descriptor(manet_device_descriptor* dst, const ma_device_info* src, ma_device_type type);
static ma_uint32 manet_min_u32(ma_uint32 a, ma_uint32 b);
static manet_engine* manet_engine_create_with_config(const ma_engine_config* inputConfig);

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

    ma_engine_listener_set_position(&handle->engine, index, x, y, z);
    return MA_SUCCESS;
}

MANET_API manet_engine* manet_engine_create_with_options(
    manet_context* contextHandle,
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
        manet_free(handle);
        return NULL;
    }

    return handle;
}
#endif

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

MANET_API void manet_sound_destroy(manet_sound* handle)
{
    if (handle == NULL) {
        return;
    }

    if (handle->ownsAudioBuffer) {
        ma_audio_buffer_uninit(&handle->audioBuffer);
    }

    ma_sound_uninit(&handle->sound);
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
