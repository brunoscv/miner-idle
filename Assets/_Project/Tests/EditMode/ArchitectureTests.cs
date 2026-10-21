using System.Linq;
using MinersIdle.Core;
using NUnit.Framework;

namespace MinersIdle.Tests
{
    /// <summary>Guarda de arquitetura: o Núcleo não pode depender da UnityEngine (Arquitetura Técnica, D-T1).</summary>
    public class ArchitectureTests
    {
        [Test]
        public void Core_DoesNotReferenceUnityEngine()
        {
            var referenced = typeof(CoreAssembly).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToList();
            Assert.That(referenced.Any(n => n.StartsWith("UnityEngine") || n.StartsWith("UnityEditor")), Is.False,
                "MinersIdle.Core referencia: " + string.Join(", ", referenced));
        }
    }
}
