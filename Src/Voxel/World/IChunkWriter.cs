using Engine3.Test.Voxel.Blocks;

namespace Engine3.Test.Voxel.World {
	public interface IChunkWriter {
		public void SetBlock(Block block, byte x, byte y, byte z);
		public void SetBlock(Block block, LocalBlockPos position);
	}
}