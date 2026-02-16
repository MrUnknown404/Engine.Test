namespace Engine3.Test.Voxel.World {
	[Flags]
	public enum BlockFace : byte {
		None = 0,
		North = 1 << 0,
		East = 1 << 1,
		South = 1 << 2,
		West = 1 << 3,
		Up = 1 << 4,
		Down = 1 << 5,
		All = North | East | South | West | Up | Down,
	}

	public static class BlockFaceExtensions {
		extension(BlockFace value) {
			public bool HasFlagFast(BlockFace flag) => (value & flag) != 0;
		}
	}
}