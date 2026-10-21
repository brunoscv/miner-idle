using System;
using System.Globalization;

namespace MinersIdle.Core
{
    /// <summary>
    /// Vetor 2D do Núcleo, em <c>double</c> (Arquitetura Técnica, D-T4). Existe porque o Núcleo não pode usar
    /// <c>UnityEngine.Vector2</c>; a Apresentação converte na hora de desenhar. Unidade = unidade de mundo (16 px).
    /// </summary>
    public readonly struct Vec2 : IEquatable<Vec2>
    {
        public readonly double X;
        public readonly double Y;

        public Vec2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static Vec2 Zero => new Vec2(0, 0);

        public double Length => Math.Sqrt(LengthSquared);
        public double LengthSquared => X * X + Y * Y;

        /// <summary>Distância ao quadrado: basta para comparar distâncias e evita a raiz.</summary>
        public static double DistanceSquared(Vec2 a, Vec2 b) => (a - b).LengthSquared;
        public static double Distance(Vec2 a, Vec2 b) => (a - b).Length;

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);
        public static Vec2 operator *(Vec2 a, double k) => new Vec2(a.X * k, a.Y * k);

        public bool Equals(Vec2 other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override bool Equals(object obj) => obj is Vec2 other && Equals(other);
        public override int GetHashCode() => (X.GetHashCode() * 397) ^ Y.GetHashCode();
        public static bool operator ==(Vec2 a, Vec2 b) => a.Equals(b);
        public static bool operator !=(Vec2 a, Vec2 b) => !a.Equals(b);

        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "({0:0.###}, {1:0.###})", X, Y);
    }
}
