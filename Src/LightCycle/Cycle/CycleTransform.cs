using System.Numerics;
using Engine3.GameObject;
using JetBrains.Annotations;

namespace Engine3.Test.LightCycle.Cycle {
	[PublicAPI]
	public class CycleTransform : ITransform<CycleTransform, Vector2, uint> {
		public static CycleTransform Zero => new();

		public Vector2 Position { get; set; }
		[Obsolete($"Use {nameof(Rotation)}")] public uint Scale { get; set; }

#pragma warning disable CS0618 // Type or member is obsolete
		public uint Rotation { get => Scale; set => Scale = value; }
#pragma warning restore CS0618 // Type or member is obsolete

		public Matrix4x4 CreateMatrix() {
			Matrix4x4 matrix = Matrix4x4.Identity;
			matrix *= Matrix4x4.CreateTranslation(Position.X, 0, Position.Y);
			matrix *= Matrix4x4.CreateRotationX(Rotation);
			return matrix;
		}

		public bool Equals(CycleTransform? other) => other != null && Position.Equals(other.Position) && Rotation == other.Rotation;
		public override bool Equals(object? obj) => obj is CycleTransform transform && Equals(transform);

		public override int GetHashCode() => HashCode.Combine(Position, Rotation);

		public static bool operator ==(CycleTransform? left, CycleTransform? right) => Equals(left, right);
		public static bool operator !=(CycleTransform? left, CycleTransform? right) => !Equals(left, right);
	}
}