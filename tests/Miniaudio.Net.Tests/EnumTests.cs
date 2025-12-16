using NUnit.Framework;
using Miniaudio.Net;
using System;

namespace Miniaudio.Net.Tests;

[TestFixture]
public class EnumTests
{
    [TestFixture]
    public class MiniaudioSampleFormatTests
    {
        [Test]
        public void Unknown_HasValueZero()
        {
            Assert.That((int)MiniaudioSampleFormat.Unknown, Is.EqualTo(0));
        }

        [Test]
        public void AllFormats_HaveUniqueValues()
        {
            var values = Enum.GetValues<MiniaudioSampleFormat>();
            var uniqueValues = new System.Collections.Generic.HashSet<int>();

            foreach (var value in values)
            {
                Assert.That(uniqueValues.Add((int)value), Is.True,
                    $"Duplicate value found: {value}");
            }
        }

        [Test]
        public void CommonFormats_Exist()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Enum.IsDefined(MiniaudioSampleFormat.Unknown), Is.True);
                Assert.That(Enum.IsDefined(MiniaudioSampleFormat.U8), Is.True);
                Assert.That(Enum.IsDefined(MiniaudioSampleFormat.S16), Is.True);
                Assert.That(Enum.IsDefined(MiniaudioSampleFormat.S24), Is.True);
                Assert.That(Enum.IsDefined(MiniaudioSampleFormat.S32), Is.True);
                Assert.That(Enum.IsDefined(MiniaudioSampleFormat.F32), Is.True);
            });
        }
    }

    [TestFixture]
    public class MiniaudioDeviceKindTests
    {
        [Test]
        public void PlaybackAndCapture_AreDefined()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Enum.IsDefined(MiniaudioDeviceKind.Playback), Is.True);
                Assert.That(Enum.IsDefined(MiniaudioDeviceKind.Capture), Is.True);
            });
        }

        [Test]
        public void PlaybackAndCapture_HaveDifferentValues()
        {
            Assert.That(MiniaudioDeviceKind.Playback, Is.Not.EqualTo(MiniaudioDeviceKind.Capture));
        }
    }

    [TestFixture]
    public class ResourceManagerFlagsTests
    {
        [Test]
        public void None_HasValueZero()
        {
            Assert.That((uint)ResourceManagerFlags.None, Is.EqualTo(0u));
        }

        [Test]
        public void NonBlocking_IsDefined()
        {
            Assert.That(Enum.IsDefined(ResourceManagerFlags.NonBlocking), Is.True);
        }

        [Test]
        public void Flags_CanBeCombined()
        {
            var combined = ResourceManagerFlags.NonBlocking;

            Assert.That(combined.HasFlag(ResourceManagerFlags.NonBlocking), Is.True);
        }
    }

    [TestFixture]
    public class SoundInitFlagsTests
    {
        [Test]
        public void None_HasValueZero()
        {
            Assert.That((uint)SoundInitFlags.None, Is.EqualTo(0u));
        }

        [Test]
        public void CommonFlags_AreDefined()
        {
            var values = Enum.GetValues<SoundInitFlags>();
            Assert.That(values.Length, Is.GreaterThan(0));
        }
    }

    [TestFixture]
    public class SoundPositioningTests
    {
        [Test]
        public void AllValues_AreDefined()
        {
            var values = Enum.GetValues<SoundPositioning>();
            Assert.That(values.Length, Is.GreaterThan(0));
        }
    }

    [TestFixture]
    public class SoundStateTests
    {
        [Test]
        public void CommonStates_AreDefined()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Enum.IsDefined(SoundState.Stopped), Is.True);
                Assert.That(Enum.IsDefined(SoundState.Playing), Is.True);
                Assert.That(Enum.IsDefined(SoundState.Starting), Is.True);
                Assert.That(Enum.IsDefined(SoundState.Stopping), Is.True);
            });
        }

        [Test]
        public void AllStates_HaveUniqueValues()
        {
            var values = Enum.GetValues<SoundState>();
            var uniqueValues = new System.Collections.Generic.HashSet<int>();

            foreach (var value in values)
            {
                Assert.That(uniqueValues.Add((int)value), Is.True,
                    $"Duplicate value found: {value}");
            }
        }
    }

    [TestFixture]
    public class MiniaudioBackendTests
    {
        [Test]
        public void CommonBackends_AreDefined()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Enum.IsDefined(MiniaudioBackend.Wasapi), Is.True);
            });
        }

        [Test]
        public void AllBackends_HavePositiveOrZeroValues()
        {
            var values = Enum.GetValues<MiniaudioBackend>();

            foreach (var value in values)
            {
                Assert.That((int)value, Is.GreaterThanOrEqualTo(0),
                    $"Backend {value} has negative value");
            }
        }
    }
}
