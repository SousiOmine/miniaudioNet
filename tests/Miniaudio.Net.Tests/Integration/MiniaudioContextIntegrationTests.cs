using NUnit.Framework;
using Miniaudio.Net;
using System;
using System.Collections.Generic;

namespace Miniaudio.Net.Tests.Integration;

/// <summary>
/// MiniaudioContextのインテグレーションテスト。
/// これらのテストはネイティブライブラリが必要です。
/// </summary>
[TestFixture]
[Category("Integration")]
public class MiniaudioContextIntegrationTests
{
    [Test]
    public void Create_DefaultBackends_ReturnsValidContext()
    {
        using var context = MiniaudioContext.Create();

        Assert.That(context, Is.Not.Null);
        Assert.That(context.PreferredBackends, Is.Empty);
    }

    [Test]
    public void Create_WithPreferredBackends_ReturnsValidContext()
    {
        var backends = new List<MiniaudioBackend> { MiniaudioBackend.Wasapi, MiniaudioBackend.DSound };

        using var context = MiniaudioContext.Create(backends);

        Assert.That(context, Is.Not.Null);
        Assert.That(context.PreferredBackends, Has.Count.EqualTo(2));
    }

    [Test]
    public void EnumerateDevices_Playback_ReturnsDeviceList()
    {
        using var context = MiniaudioContext.Create();

        var devices = context.EnumerateDevices(MiniaudioDeviceKind.Playback);

        Assert.That(devices, Is.Not.Null);
        // CI環境ではデバイスがない場合もあるため、空リストも許容
    }

    [Test]
    public void EnumerateDevices_Capture_ReturnsDeviceList()
    {
        using var context = MiniaudioContext.Create();

        var devices = context.EnumerateDevices(MiniaudioDeviceKind.Capture);

        Assert.That(devices, Is.Not.Null);
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var context = MiniaudioContext.Create();

        Assert.DoesNotThrow(() =>
        {
            context.Dispose();
            context.Dispose();
        });
    }

    [Test]
    public void EnumerateDevices_AfterDispose_ThrowsObjectDisposedException()
    {
        var context = MiniaudioContext.Create();
        context.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
            context.EnumerateDevices(MiniaudioDeviceKind.Playback));
    }

    [Test]
    public void Create_DuplicateBackends_RemovesDuplicates()
    {
        var backends = new List<MiniaudioBackend> { MiniaudioBackend.Wasapi, MiniaudioBackend.Wasapi };

        using var context = MiniaudioContext.Create(backends);

        Assert.That(context.PreferredBackends, Has.Count.EqualTo(1));
    }

    [Test]
    public void Create_EmptyBackends_TreatedAsDefault()
    {
        using var context = MiniaudioContext.Create(Array.Empty<MiniaudioBackend>());

        Assert.That(context.PreferredBackends, Is.Empty);
    }
}
