namespace Engine3.Test.Voxel.World {
	[Flags]
	public enum BlockStateFlags : byte {
		None = 0,
		WasGenerated = 1 << 0,
		All = byte.MaxValue,
	}

	public static class BlockStateFlagsExtensions {
		extension(BlockStateFlags value) {
			public bool HasFlagFast(BlockStateFlags flag) => (value & flag) != 0;
		}
	}
}