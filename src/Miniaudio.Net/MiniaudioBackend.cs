namespace Miniaudio.Net;

public enum MiniaudioBackend
{
    Wasapi = 0,
    DSound = 1,
    WinMM = 2,
    CoreAudio = 3,
    Sndio = 4,
    Audio4 = 5,
    Oss = 6,
    PulseAudio = 7,
    Alsa = 8,
    Jack = 9,
    AAudio = 10,
    OpenSLES = 11,
    WebAudio = 12,
    Custom = 13,
    Null = 14,
}
