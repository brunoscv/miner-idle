using MinersIdle.UI;
using NUnit.Framework;
using UnityEngine;

namespace MinersIdle.Tests
{
    public class SafeAreaControllerTests
    {
        [Test]
        public void FullScreen_GivesFullAnchors()
        {
            SafeAreaController.ComputeAnchors(new Rect(0, 0, 1080, 2400), 1080, 2400, out var min, out var max);
            Assert.That(min, Is.EqualTo(Vector2.zero));
            Assert.That(max, Is.EqualTo(Vector2.one));
        }

        [Test]
        public void NotchAndGestureBar_ShrinkTopAndBottom()
        {
            // 100 px de recorte em cima, 50 px de barra embaixo.
            SafeAreaController.ComputeAnchors(new Rect(0, 50, 1080, 2250), 1080, 2400, out var min, out var max);
            Assert.That(min.y, Is.EqualTo(50f / 2400f).Within(1e-6f));
            Assert.That(max.y, Is.EqualTo(2300f / 2400f).Within(1e-6f));
            Assert.That(min.x, Is.EqualTo(0f));
            Assert.That(max.x, Is.EqualTo(1f));
        }

        [Test]
        public void InvalidScreen_FallsBackToFullAnchors()
        {
            SafeAreaController.ComputeAnchors(new Rect(0, 0, 0, 0), 0, 0, out var min, out var max);
            Assert.That(min, Is.EqualTo(Vector2.zero));
            Assert.That(max, Is.EqualTo(Vector2.one));
        }
    }
}
