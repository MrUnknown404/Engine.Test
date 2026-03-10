using Engine3.Test.Voxel.Blocks;
using JetBrains.Annotations;

namespace Engine3.Test.Voxel.World {
	public interface IChunkAccessor {
		public bool IsEmpty { get; }

		[MustUseReturnValue] public Block GetBlock(byte x, byte y, byte z);
		[MustUseReturnValue] public Block GetBlock(LocalBlockPos position);

		[MustUseReturnValue] public Block GetBlock(uint index);
	}
}