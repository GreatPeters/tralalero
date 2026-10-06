using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class LegacyOutlineShaderTests
{
    private const string ShaderName = "FlatKit/Stylized Surface With Outline";

    [Test]
    public void FreshMaterialBindsEveryPassWithoutKeywordSpaceAssertions()
    {
        var shader = Shader.Find(ShaderName);
        Assert.That(shader, Is.Not.Null);
        var material = new Material(shader);
        var target = new RenderTexture(32, 32, 24);
        var previous = RenderTexture.active;
        try
        {
            target.Create();
            RenderTexture.active = target;
            Assert.That(Enumerable.Range(0, material.passCount).Select(material.GetPassName),
                Is.EqualTo(new[] { "ForwardLit", "OutlineLegacy", "ShadowCaster", "GBuffer", "DepthOnly", "DepthNormals", "Meta" }));
            for (int pass = 0; pass < material.passCount; pass++)
                Assert.That(material.SetPass(pass), Is.True, material.GetPassName(pass));
            // SetPass can return true even when native keyword-state assertions fire.
            LogAssert.NoUnexpectedReceived();
        }
        finally
        {
            RenderTexture.active = previous;
            target.Release();
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(material);
        }
    }

    [Test]
    public void AllPassesRemainSrpBatcherCompatible()
    {
        var method = typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.That(method, Is.Not.Null, "Unity 6000.2 compatibility diagnostic must be available.");
        Assert.That((int)method.Invoke(null, new object[] { Shader.Find(ShaderName), 0 }), Is.Zero);
    }
}
