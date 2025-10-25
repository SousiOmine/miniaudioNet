namespace Miniaudio.Net;

public sealed record MiniaudioDeviceInfo(
    string Name,
    MiniaudioDeviceKind Kind,
    bool IsDefault,
    string DeviceId)
{
    public override string ToString()
        => $"{Name} {(IsDefault ? "[default]" : string.Empty)}".Trim();
}
