using System.Numerics;

namespace Engine3.Test.Voxel.World;

public enum BlockFace : byte {
	North = 0,
	East,
	South,
	West,
	Up,
	Down,
}

public static class BlockFaceExtensions {
	extension(BlockFace self) {
		public Vector3 ToVector =>
				self switch {
						BlockFace.North => -Vector3.UnitZ,
						BlockFace.East => Vector3.UnitX,
						BlockFace.South => Vector3.UnitZ,
						BlockFace.West => -Vector3.UnitX,
						BlockFace.Up => Vector3.UnitY,
						BlockFace.Down => -Vector3.UnitY,
						_ => throw new ArgumentOutOfRangeException(nameof(self), self, null),
				};
	}
}