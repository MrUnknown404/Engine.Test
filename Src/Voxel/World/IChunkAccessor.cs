using JetBrains.Annotations;

namespace Engine3.Test.Voxel.World {
	public interface IChunkAccessor {
		public bool IsEmpty { get; }

		[MustUseReturnValue] public BlockState GetBlockState(byte x, byte y, byte z);
		[MustUseReturnValue] public BlockState GetBlockState(LocalBlockPos position);

		[MustUseReturnValue] public BlockState GetBlockState(uint index);
	}
}