using Engine3.Test.Voxel.World;

namespace Engine3.Test.Voxel.Graphics.Renderers {
	public readonly record struct ChunkPositionPushConstants {
		public ChunkPos Position { get; init; }

		public ChunkPositionPushConstants(ChunkPos position) => Position = position;
	}
}