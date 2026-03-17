namespace Engine3.Test.Voxel.World {
	public interface IChunkWriter {
		public void SetBlockState(BlockState blockState, byte x, byte y, byte z);
		public void SetBlockState(BlockState blockState, LocalBlockPos position);
	}
}