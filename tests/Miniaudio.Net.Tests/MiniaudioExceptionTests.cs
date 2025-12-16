using NUnit.Framework;
using Miniaudio.Net;
using System;

namespace Miniaudio.Net.Tests;

[TestFixture]
public class MiniaudioExceptionTests
{
    [Test]
    public void Constructor_SetsAllProperties()
    {
        var exception = new MiniaudioException(42, "TestApi", "Test description");

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo(42));
            Assert.That(exception.Api, Is.EqualTo("TestApi"));
            Assert.That(exception.Description, Is.EqualTo("Test description"));
        });
    }

    [Test]
    public void Constructor_NullDescription_SetsEmptyString()
    {
        var exception = new MiniaudioException(1, "TestApi", null);

        Assert.That(exception.Description, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Message_ContainsAllInformation()
    {
        var exception = new MiniaudioException(100, "ma_engine_init", "Invalid operation");

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Does.Contain("ma_engine_init"));
            Assert.That(exception.Message, Does.Contain("100"));
            Assert.That(exception.Message, Does.Contain("Invalid operation"));
        });
    }

    [Test]
    public void Message_NullDescription_ContainsNoDescription()
    {
        var exception = new MiniaudioException(1, "TestApi", null);

        Assert.That(exception.Message, Does.Contain("(no description)"));
    }

    [Test]
    public void InheritsFromInvalidOperationException()
    {
        var exception = new MiniaudioException(1, "TestApi", "Test");

        Assert.That(exception, Is.InstanceOf<InvalidOperationException>());
    }

    [Test]
    public void CanBeCaughtAsInvalidOperationException()
    {
        bool caughtAsInvalidOperation = false;

        try
        {
            throw new MiniaudioException(1, "TestApi", "Test");
        }
        catch (InvalidOperationException)
        {
            caughtAsInvalidOperation = true;
        }

        Assert.That(caughtAsInvalidOperation, Is.True);
    }
}
