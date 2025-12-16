namespace Miniaudio.Net;

public sealed record MiniaudioDeviceInfo(
    string Name,
    MiniaudioDeviceKind Kind,
    bool IsDefault,
    string DeviceId)
{
    public bool IsCapture => Kind == MiniaudioDeviceKind.Capture;

    public bool IsPlayback => Kind == MiniaudioDeviceKind.Playback;

    public override string ToString()
        => $"{Name} {(IsDefault ? "[default]" : string.Empty)}".Trim();
}
