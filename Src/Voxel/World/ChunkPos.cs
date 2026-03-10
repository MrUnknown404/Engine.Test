using JetBrains.Annotations;

namespace Engine3.Test.Voxel.World {
	public readonly record struct ChunkPos {
		public int X { get; init; }
		public int Y { get; init; }
		public int Z { get; init; }

		public ChunkPos(int x, int y, int z) {
			X = x;
			Y = y;
			Z = z;
		}

		[MustUseReturnValue] public ChunkPos Offset(int x, int y, int z) => new(X + x, Y + y, Z + z);

		[MustUseReturnValue] public static ChunkPos operator +(ChunkPos left, ChunkPos right) => new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
		[MustUseReturnValue] public static ChunkPos operator -(ChunkPos left, ChunkPos right) => new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
		[MustUseReturnValue] public static ChunkPos operator *(ChunkPos left, ChunkPos right) => new(left.X * right.X, left.Y * right.Y, left.Z * right.Z);
		[MustUseReturnValue] public static ChunkPos operator /(ChunkPos left, ChunkPos right) => new(left.X / right.X, left.Y / right.Y, left.Z / right.Z);

		public override string ToString() => $"({X}, {Y}, {Z})";
	}
}