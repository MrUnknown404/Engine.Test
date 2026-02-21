namespace Engine3.Test.Voxel.World {
	[Flags]
	public enum BlockFaceMask : byte {
		None = 0,
		North = 1 << 0,
		East = 1 << 1,
		South = 1 << 2,
		West = 1 << 3,
		Up = 1 << 4,
		Down = 1 << 5,
		All = North | East | South | West | Up | Down,
	}

	public static class BlockFaceMaskExtensions {
		extension(BlockFaceMask self) {
			public bool HasFlagFast(BlockFaceMask flag) => (self & flag) != 0;
		}
	}
}