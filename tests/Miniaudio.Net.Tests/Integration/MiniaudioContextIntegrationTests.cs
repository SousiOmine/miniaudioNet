using NUnit.Framework;
using Miniaudio.Net;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Miniaudio.Net.Tests.Integration;

/// <summary>
/// MiniaudioContextのインテグレーションテスト。
/// これらのテストはネイティブライブラリが必要です。
/// </summary>
[TestFixture]
[Category("Integration")]
public class MiniaudioContextIntegrationTests
{
    /// <summary>
    /// 現在のプラットフォームで利用可能なバックエンドを取得
    /// </summary>
    private static MiniaudioBackend[] GetPlatformBackends()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new[] { MiniaudioBackend.Wasapi, MiniaudioBackend.DSound };
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new[] { MiniaudioBackend.CoreAudio };
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new[] { MiniaudioBackend.PulseAudio, MiniaudioBackend.Alsa };
        }
        else
        {
            return Array.Empty<MiniaudioBackend>();
        }
    }

    /// <summary>
    /// 現在のプラットフォームで利用可能な単一のバックエンドを取得
    /// </summary>
    private static MiniaudioBackend GetPrimaryPlatformBackend()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return MiniaudioBackend.Wasapi;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return MiniaudioBackend.CoreAudio;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return MiniaudioBackend.PulseAudio;
        }
        else
        {
            // フォールバック: Nullバックエンドを使用
            return MiniaudioBackend.Null;
        }
    }

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
        var backends = GetPlatformBackends();
        // プラットフォーム固有のバックエンドが取得できない場合はスキップ
        if (backends.Length == 0)
        {
            Assert.Ignore("No platform-specific backends available for this OS.");
        }

        using var context = MiniaudioContext.Create(backends);

        Assert.That(context, Is.Not.Null);
        Assert.That(context.PreferredBackends, Has.Count.EqualTo(backends.Length));
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
        var backends = new List<MiniaudioBackend> { MiniaudioBackend.Null, MiniaudioBackend.Null };

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
