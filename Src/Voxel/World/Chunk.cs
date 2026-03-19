using JetBrains.Annotations;

namespace Engine3.Test.Voxel.World {
	public class Chunk : IChunkReader, IChunkWriter {
		public const byte Size = 16;
		public const ushort ArraySize = Size * Size * Size;

		private readonly World world;

		public ChunkPos Position { get; }
		public bool IsEmpty { get; private set; }

		private readonly BlockState[] blocks;

		internal Chunk(World world, ChunkPos position, BlockState[] blocks, bool isEmpty) {
			this.world = world;
			Position = position;

			this.blocks = blocks;
			IsEmpty = isEmpty;
		}

		internal Chunk(World world, ChunkPos position) {
			this.world = world;
			Position = position;
			blocks = new BlockState[ArraySize];

			Array.Fill(blocks, new(Blocks.Blocks.Air, BlockStateFlags.WasGenerated));
			IsEmpty = true;
		}

		public BlockState GetBlockState(byte x, byte y, byte z) => blocks[ToIndex(x, y, z)];
		public BlockState GetBlockState(LocalBlockPos position) => blocks[ToIndex(position)];
		public BlockState GetBlockState(uint index) => blocks[index];

		public void SetBlockState(BlockState blockState, byte x, byte y, byte z) => blocks[ToIndex(x, y, z)] = blockState;
		public void SetBlockState(BlockState blockState, LocalBlockPos position) => blocks[ToIndex(position)] = blockState;

		internal void UpdateIsEmpty() => IsEmpty = !blocks.Any(static blockState => blockState.Block.Properties.SolidFaceMask != BlockFaceMask.None);

		[MustUseReturnValue] internal static ushort ToIndex(byte x, byte y, byte z) => (ushort)(x + y * Size * Size + z * Size);
		[MustUseReturnValue] internal static ushort ToIndex(LocalBlockPos blockPos) => (ushort)(blockPos.X + blockPos.Y * Size * Size + blockPos.Z * Size);

		internal static void FromIndex(ushort index, out byte x, out byte y, out byte z) {
			x = (byte)(index % Size);
			y = (byte)(index / (Size * Size));
			z = (byte)(index / Size % Size);
		}

		[MustUseReturnValue] internal static LocalBlockPos FromIndex(ushort index) => new((byte)(index % Size), (byte)(index / (Size * Size)), (byte)(index / Size % Size));
	}
}