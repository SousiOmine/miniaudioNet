using NUnit.Framework;
using Miniaudio.Net;

namespace Miniaudio.Net.Tests;

[TestFixture]
public class MiniaudioDeviceInfoTests
{
    [Test]
    public void Constructor_SetsAllProperties()
    {
        var info = new MiniaudioDeviceInfo("Test Device", MiniaudioDeviceKind.Playback, true, "device-123");

        Assert.Multiple(() =>
        {
            Assert.That(info.Name, Is.EqualTo("Test Device"));
            Assert.That(info.Kind, Is.EqualTo(MiniaudioDeviceKind.Playback));
            Assert.That(info.IsDefault, Is.True);
            Assert.That(info.DeviceId, Is.EqualTo("device-123"));
        });
    }

    [Test]
    public void ToString_DefaultDevice_IncludesDefaultLabel()
    {
        var info = new MiniaudioDeviceInfo("Default Speaker", MiniaudioDeviceKind.Playback, true, "id-1");

        var result = info.ToString();

        Assert.That(result, Is.EqualTo("Default Speaker [default]"));
    }

    [Test]
    public void ToString_NonDefaultDevice_DoesNotIncludeDefaultLabel()
    {
        var info = new MiniaudioDeviceInfo("Secondary Speaker", MiniaudioDeviceKind.Playback, false, "id-2");

        var result = info.ToString();

        Assert.That(result, Is.EqualTo("Secondary Speaker"));
    }

    [Test]
    public void Equality_SameValues_AreEqual()
    {
        var info1 = new MiniaudioDeviceInfo("Device", MiniaudioDeviceKind.Capture, false, "abc");
        var info2 = new MiniaudioDeviceInfo("Device", MiniaudioDeviceKind.Capture, false, "abc");

        Assert.That(info1, Is.EqualTo(info2));
    }

    [Test]
    public void Equality_DifferentName_NotEqual()
    {
        var info1 = new MiniaudioDeviceInfo("Device A", MiniaudioDeviceKind.Playback, true, "id");
        var info2 = new MiniaudioDeviceInfo("Device B", MiniaudioDeviceKind.Playback, true, "id");

        Assert.That(info1, Is.Not.EqualTo(info2));
    }

    [Test]
    public void Equality_DifferentKind_NotEqual()
    {
        var info1 = new MiniaudioDeviceInfo("Device", MiniaudioDeviceKind.Playback, true, "id");
        var info2 = new MiniaudioDeviceInfo("Device", MiniaudioDeviceKind.Capture, true, "id");

        Assert.That(info1, Is.Not.EqualTo(info2));
    }

    [Test]
    public void Equality_DifferentIsDefault_NotEqual()
    {
        var info1 = new MiniaudioDeviceInfo("Device", MiniaudioDeviceKind.Playback, true, "id");
        var info2 = new MiniaudioDeviceInfo("Device", MiniaudioDeviceKind.Playback, false, "id");

        Assert.That(info1, Is.Not.EqualTo(info2));
    }

    [Test]
    public void Equality_DifferentDeviceId_NotEqual()
    {
        var info1 = new MiniaudioDeviceInfo("Device", MiniaudioDeviceKind.Playback, true, "id-1");
        var info2 = new MiniaudioDeviceInfo("Device", MiniaudioDeviceKind.Playback, true, "id-2");

        Assert.That(info1, Is.Not.EqualTo(info2));
    }

    [Test]
    public void GetHashCode_SameValues_SameHashCode()
    {
        var info1 = new MiniaudioDeviceInfo("Device", MiniaudioDeviceKind.Capture, false, "abc");
        var info2 = new MiniaudioDeviceInfo("Device", MiniaudioDeviceKind.Capture, false, "abc");

        Assert.That(info1.GetHashCode(), Is.EqualTo(info2.GetHashCode()));
    }

    [Test]
    public void Record_CanUseWithExpression()
    {
        var original = new MiniaudioDeviceInfo("Original", MiniaudioDeviceKind.Playback, true, "id-1");
        var modified = original with { Name = "Modified" };

        Assert.Multiple(() =>
        {
            Assert.That(modified.Name, Is.EqualTo("Modified"));
            Assert.That(modified.Kind, Is.EqualTo(MiniaudioDeviceKind.Playback));
            Assert.That(modified.IsDefault, Is.True);
            Assert.That(modified.DeviceId, Is.EqualTo("id-1"));
        });
    }

    [Test]
    public void IsCapture_ReturnsTrueForCaptureDevice()
    {
        var captureInfo = new MiniaudioDeviceInfo("Microphone", MiniaudioDeviceKind.Capture, false, "mic-1");
        var playbackInfo = new MiniaudioDeviceInfo("Speaker", MiniaudioDeviceKind.Playback, false, "speaker-1");

        Assert.Multiple(() =>
        {
            Assert.That(captureInfo.IsCapture, Is.True);
            Assert.That(playbackInfo.IsCapture, Is.False);
        });
    }

    [Test]
    public void IsPlayback_ReturnsTrueForPlaybackDevice()
    {
        var captureInfo = new MiniaudioDeviceInfo("Microphone", MiniaudioDeviceKind.Capture, false, "mic-1");
        var playbackInfo = new MiniaudioDeviceInfo("Speaker", MiniaudioDeviceKind.Playback, false, "speaker-1");

        Assert.Multiple(() =>
        {
            Assert.That(playbackInfo.IsPlayback, Is.True);
            Assert.That(captureInfo.IsPlayback, Is.False);
        });
    }
}
