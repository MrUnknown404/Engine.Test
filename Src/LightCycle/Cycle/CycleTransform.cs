using System.Numerics;
using Engine3.Utility;
using JetBrains.Annotations;

namespace Engine3.Test.LightCycle.Cycle {
	[PublicAPI]
	public class CycleTransform : ITransform<CycleTransform>, IEquatable<CycleTransform> {
		public static CycleTransform Zero => new();

		public Vector2 Position { get; set; }
		public uint Rotation { get; set; }

		public Matrix4x4 CreateMatrix() {
			Matrix4x4 matrix = Matrix4x4.Identity;
			matrix *= Matrix4x4.CreateTranslation(Position.X, 0, Position.Y);
			matrix *= Matrix4x4.CreateRotationX(Rotation);
			return matrix;
		}

		public Matrix4x4 CreateMatrix(float delta, CycleTransform prev) {
			Vector2 pos = Vector2.Lerp(prev.Position, Position, delta);
			float rot = float.Lerp(prev.Rotation, Rotation, delta);

			Matrix4x4 matrix = Matrix4x4.Identity;
			matrix *= Matrix4x4.CreateTranslation(pos.X, 0, pos.Y);
			matrix *= Matrix4x4.CreateRotationX(rot);
			return matrix;
		}

		public bool Equals(CycleTransform? other) => other != null && Position.Equals(other.Position) && Rotation == other.Rotation;
		public override bool Equals(object? obj) => obj is CycleTransform transform && Equals(transform);

		public override int GetHashCode() => HashCode.Combine(Position, Rotation);

		public static bool operator ==(CycleTransform? left, CycleTransform? right) => Equals(left, right);
		public static bool operator !=(CycleTransform? left, CycleTransform? right) => !Equals(left, right);
	}
}