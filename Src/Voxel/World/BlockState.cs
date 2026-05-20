using Engine3.Test.Voxel.Blocks;

namespace Engine3.Test.Voxel.World;

public class BlockState {
	public Block Block { get; internal set; }
	public BlockStateFlags BlockStateFlags;

	public BlockState(Block block, BlockStateFlags blockStateFlags) {
		Block = block;
		BlockStateFlags = blockStateFlags;
	}
}