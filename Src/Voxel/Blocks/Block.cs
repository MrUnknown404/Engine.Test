using Engine3.Test.Voxel.World;

namespace Engine3.Test.Voxel.Blocks {
	public class Block {
		public static Block Air { get; } = new("air", new() { SolidFaceMask = BlockFaceMask.None, }); // TODO registry?
		public static Block Stone { get; } = new("stone");

		public string Name { get; }

		public BlockProperties Properties { get; }

		public Block(string name, BlockProperties? properties = null) {
			Name = name;
			Properties = properties ?? new();
		}
	}

	public class BlockProperties {
		public BlockFaceMask SolidFaceMask { get; init; } = BlockFaceMask.All;
	}
}