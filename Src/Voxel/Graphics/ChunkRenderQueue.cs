using Engine3.Test.Voxel.World;
using JetBrains.Annotations;

namespace Engine3.Test.Voxel.Graphics;

public class ChunkRenderQueue {
	public bool ShouldRenderChunks => chunksToRender.Count != 0;

	private readonly HashSet<ChunkPos> chunksToRender = new();

	public void Enqueue(ChunkPos position) => chunksToRender.Add(position);

	[MustUseReturnValue]
	internal ChunkPos[] DequeueAll() {
		ChunkPos[] positions = chunksToRender.ToArray();
		chunksToRender.Clear();
		return positions;
	}
}