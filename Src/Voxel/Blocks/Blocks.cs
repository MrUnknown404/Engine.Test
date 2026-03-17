using System.Collections.Frozen;
using Engine3.Test.Voxel.World;

namespace Engine3.Test.Voxel.Blocks {
	public static class Blocks {
		public static Block Air { get; } = new(new(VoxelTest.ModSource, "air"), new() { SolidFaceMask = BlockFaceMask.None, });
		public static Block Stone { get; } = new(new(VoxelTest.ModSource, "stone"));
		public static Block Dirt { get; } = new(new(VoxelTest.ModSource, "dirt"));
		public static Block Grass { get; } = new(new(VoxelTest.ModSource, "grass"));

		internal static FrozenSet<Block> AllBlocks { get; } = [ Air, Stone, Dirt, Grass, ];
	}
}