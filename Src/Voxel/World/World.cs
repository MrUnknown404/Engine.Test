using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Engine3.Client;
using Engine3.Test.Voxel.Blocks;
using Engine3.Test.Voxel.Graphics.Renderers;
using NLog;

namespace Engine3.Test.Voxel.World {
	public class World : IWorldAccessor, IWorldWriter {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public WorldProperties WorldProperties { get; }
		public HeightMap HeightMap { get; }

		public uint ChunkCount => (uint)chunks.Count;
		public uint DirtyChunkCount => (uint)dirtyChunks.Count;
		public uint RendererChunkCount => worldRenderPass.ChunkCount;

		// TODO how do i want to store chunks? do i want to separate chunks by how they're used? rendering (client only), updates, etc?

		private readonly Dictionary<ChunkPos, Chunk> chunks = new();
		private readonly HashSet<ChunkPos> dirtyChunks = new();

		private readonly ChunkGenerationQueue chunkGenerationQueue;

		private readonly WorldRenderPass worldRenderPass;
		private readonly Camera camera;

		private ChunkPos prevCameraPos = new(int.MinValue, int.MinValue, int.MinValue);

		public World(WorldProperties worldProperties, Camera camera, WorldRenderPass worldRenderPass) {
			WorldProperties = worldProperties;
			HeightMap = new(worldProperties);
			this.camera = camera;
			this.worldRenderPass = worldRenderPass;
			chunkGenerationQueue = new(this);

			InitialChunkGeneration();
		}

		private void InitialChunkGeneration() {
			const byte RadiusXZ = 8;
			const byte Depth = 4;
			const byte Height = 4;

			HashSet<ChunkPos> chunkPositions = new();

			for (int x = -RadiusXZ; x <= RadiusXZ; x++) {
				for (int z = -RadiusXZ; z <= RadiusXZ; z++) {
					for (int y = -Depth; y <= Height; y++) {
						chunkPositions.Add(new(x, y, z)); //
					}
				}
			}

			foreach (ChunkPos chunkPos in chunkPositions) { chunkGenerationQueue.Enqueue(chunkPos); }

			TryGenerateChunks();

			if (TryEditChunk(new() { Y = 2, }, out IChunkWriter? chunkWriter)) { chunkWriter.SetBlock(Block.Stone, new()); }
		}

		internal void Update() {
			HashSet<ChunkPos> chunksToGiveToRenderer = new();

			// get camera chunk position
			Vector3 cameraPos = camera.Position;
			ChunkPos cameraChunkPos = new((int)(cameraPos.X / Chunk.Size), (int)(cameraPos.Y / Chunk.Size), (int)(cameraPos.Z / Chunk.Size));
			cameraChunkPos = cameraChunkPos.Offset(cameraPos.X < 0 ? -1 : 0, cameraPos.Y < 0 ? -1 : 0, cameraPos.Z < 0 ? -1 : 0); // TODO not sure how to handle this atm

			TryAddChunksAroundCamera(cameraChunkPos, 3);

			TryGenerateChunks();

			TryCleanChunks();
			TryGiveChunksToRenderer();

			return;

			void TryAddChunksAroundCamera(ChunkPos centerChunkPos, byte radius) {
				if (prevCameraPos == cameraChunkPos) { return; }

				Logger.Trace("Camera is in a new chunk. Requesting new chunks...");

				prevCameraPos = cameraChunkPos;

				for (int x = -radius; x <= radius; x++) {
					for (int y = -radius; y <= radius; y++) {
						for (int z = -radius; z <= radius; z++) {
							chunksToGiveToRenderer.Add(centerChunkPos.Offset(x, y, z)); //
						}
					}
				}
			}

			void TryCleanChunks() {
				if (dirtyChunks.Count == 0) { return; }

				Logger.Trace($"Found {dirtyChunks.Count} dirty chunks");

				foreach (ChunkPos position in dirtyChunks) {
					chunks[position].UpdateIsEmpty();
					worldRenderPass.EnqueueChunk(position);
				}

				dirtyChunks.Clear();
			}

			void TryGiveChunksToRenderer() {
				if (chunksToGiveToRenderer.Count == 0) { return; }

				Logger.Trace($"Found {chunksToGiveToRenderer.Count} potential chunks to to render");
				foreach (ChunkPos chunkPos in chunksToGiveToRenderer.AsValueEnumerable().Where(chunkPos => chunks.ContainsKey(chunkPos))) { worldRenderPass.EnqueueChunkIfNotCached(chunkPos); }
			}
		}

		public bool TryGetChunk(ChunkPos position, [NotNullWhen(true)] out IChunkAccessor? chunkAccessor) {
			bool flag = chunks.TryGetValue(position, out Chunk? chunk);
			chunkAccessor = chunk;
			return flag;
		}

		public bool TryEditChunk(ChunkPos position, [NotNullWhen(true)] out IChunkWriter? chunkWriter) {
			bool flag = chunks.TryGetValue(position, out Chunk? chunk);
			chunkWriter = chunk;
			MarkChunkDirty(position);
			return flag;
		}

		private void TryGenerateChunks() {
			if (chunkGenerationQueue.ShouldGenerateChunks) {
				Logger.Trace($"Found {chunkGenerationQueue.ChunkCount} chunks to generate");
				foreach (Chunk chunk in chunkGenerationQueue.GenerateChunks()) { chunks.Add(chunk.Position, chunk); }
			}
		}

		private void MarkChunkDirty(ChunkPos position) => dirtyChunks.Add(position);

		internal void MarkAllChunksDirty() {
			Logger.Debug($"Marked all {chunks.Count} chunks dirty");
			foreach (ChunkPos position in chunks.Keys) { MarkChunkDirty(position); }
		}

		internal void MarkAllRenderingChunksDirty() {
			Logger.Debug($"Marked all {worldRenderPass.ChunkCount} rendering chunks dirty");
			worldRenderPass.MarkAllChunksDirty();
		}

		internal void ClearRenderCache() {
			Logger.Debug("Cleared render cache");
			worldRenderPass.ClearCache();
		}
	}
}