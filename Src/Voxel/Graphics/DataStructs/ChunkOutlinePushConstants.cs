using Engine3.Test.Voxel.World;

namespace Engine3.Test.Voxel.Graphics.DataStructs;

public readonly record struct ChunkOutlinePushConstants {
	public ChunkPos Position { get; init; }

	public ChunkOutlinePushConstants(ChunkPos position) => Position = position;
}