using NUnit.Framework;
using Miniaudio.Net;
using System;

namespace Miniaudio.Net.Tests;

[TestFixture]
public class ListenerConeTests
{
    [Test]
    public void Constructor_SetsAllProperties()
    {
        var cone = new MiniaudioEngine.ListenerCone(1.0f, 2.0f, 0.5f);

        Assert.Multiple(() =>
        {
            Assert.That(cone.InnerAngleRadians, Is.EqualTo(1.0f));
            Assert.That(cone.OuterAngleRadians, Is.EqualTo(2.0f));
            Assert.That(cone.OuterGain, Is.EqualTo(0.5f));
        });
    }

    [Test]
    public void Constructor_ZeroValues_AreValid()
    {
        var cone = new MiniaudioEngine.ListenerCone(0f, 0f, 0f);

        Assert.Multiple(() =>
        {
            Assert.That(cone.InnerAngleRadians, Is.EqualTo(0f));
            Assert.That(cone.OuterAngleRadians, Is.EqualTo(0f));
            Assert.That(cone.OuterGain, Is.EqualTo(0f));
        });
    }

    [Test]
    public void Constructor_FullCircleAngles()
    {
        var fullCircle = (float)(2 * Math.PI);
        var cone = new MiniaudioEngine.ListenerCone(fullCircle, fullCircle, 1.0f);

        Assert.Multiple(() =>
        {
            Assert.That(cone.InnerAngleRadians, Is.EqualTo(fullCircle).Within(0.0001f));
            Assert.That(cone.OuterAngleRadians, Is.EqualTo(fullCircle).Within(0.0001f));
        });
    }

    [Test]
    public void Constructor_NegativeValues_AreAccepted()
    {
        var cone = new MiniaudioEngine.ListenerCone(-1.0f, -2.0f, -0.5f);

        Assert.Multiple(() =>
        {
            Assert.That(cone.InnerAngleRadians, Is.EqualTo(-1.0f));
            Assert.That(cone.OuterAngleRadians, Is.EqualTo(-2.0f));
            Assert.That(cone.OuterGain, Is.EqualTo(-0.5f));
        });
    }

    [Test]
    public void Properties_AreReadOnly()
    {
        var coneType = typeof(MiniaudioEngine.ListenerCone);

        var innerAngle = coneType.GetProperty("InnerAngleRadians");
        var outerAngle = coneType.GetProperty("OuterAngleRadians");
        var outerGain = coneType.GetProperty("OuterGain");

        Assert.Multiple(() =>
        {
            Assert.That(innerAngle?.CanWrite, Is.False);
            Assert.That(outerAngle?.CanWrite, Is.False);
            Assert.That(outerGain?.CanWrite, Is.False);
        });
    }

    [Test]
    public void TypicalAudioConeValues()
    {
        var innerAngle = (float)(Math.PI / 4);
        var outerAngle = (float)(Math.PI / 2);
        var cone = new MiniaudioEngine.ListenerCone(innerAngle, outerAngle, 0.25f);

        Assert.Multiple(() =>
        {
            Assert.That(cone.InnerAngleRadians, Is.LessThan(cone.OuterAngleRadians));
            Assert.That(cone.OuterGain, Is.InRange(0f, 1f));
        });
    }
}
