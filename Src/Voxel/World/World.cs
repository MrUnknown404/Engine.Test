using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Engine3.Client;
using Engine3.Test.Voxel.Graphics.Renderers;
using NLog;

namespace Engine3.Test.Voxel.World {
	public class World {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private const byte CameraViewDistance = 3; // in chunks

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

			EnqueueInitialChunkGeneration();
		}

		private void EnqueueInitialChunkGeneration() {
			const byte RadiusXZ = 8;
			const byte Height = 16;

			HashSet<ChunkPos> chunkPositions = new();

			for (int x = -RadiusXZ; x <= RadiusXZ; x++) {
				for (int z = -RadiusXZ; z <= RadiusXZ; z++) {
					for (int y = 0; y <= Height; y++) {
						chunkPositions.Add(new(x, y - Height, z)); //
					}
				}
			}

			foreach (ChunkPos chunkPos in chunkPositions) {
				chunkGenerationQueue.Enqueue(chunkPos); //
			}
		}

		internal void Update() {
			Vector3 cameraPos = camera.Position;
			ChunkPos cameraChunkPos = new((int)(cameraPos.X / Chunk.Size), (int)(cameraPos.Y / Chunk.Size), (int)(cameraPos.Z / Chunk.Size));
			cameraChunkPos = cameraChunkPos.Offset(cameraPos.X < 0 ? -1 : 0, cameraPos.Y < 0 ? -1 : 0, cameraPos.Z < 0 ? -1 : 0); // TODO not sure how to handle this atm

			if (prevCameraPos != cameraChunkPos) {
				Logger.Trace("Camera is in a new chunk. Requesting new chunks...");

				TryAddChunksAroundCamera(cameraChunkPos, CameraViewDistance);
				prevCameraPos = cameraChunkPos;
			}

			TryGenerateChunks();

			// TODO clean chunks

			return;

			void TryAddChunksAroundCamera(ChunkPos centerChunkPos, byte radius) {
				HashSet<ChunkPos> chunkPositions = new();

				for (int x = -radius; x <= radius; x++) {
					for (int y = -radius; y <= radius; y++) {
						for (int z = -radius; z <= radius; z++) {
							chunkPositions.Add(centerChunkPos.Offset(x, y, z)); //
						}
					}
				}

				Logger.Trace($"Found {chunkPositions.Count} chunks");

				uint count = 0;

				foreach (ChunkPos chunkPos in chunkPositions) {
					if (dirtyChunks.Contains(chunkPos)) {
						worldRenderPass.EnqueueChunk(chunkPos); //
						count++;
					} else if (chunks.ContainsKey(chunkPos)) {
						if (worldRenderPass.EnqueueChunkIfNotCached(chunkPos)) { count++; }
					}
				}

				Logger.Trace($"Added {count} chunks");
			}

			void TryGenerateChunks() {
				if (chunkGenerationQueue.ShouldGenerateChunks) {
					Logger.Trace($"Found {chunkGenerationQueue.ChunkCount} chunks to render");

					Chunk[] chunks = chunkGenerationQueue.GenerateChunks();
					foreach (Chunk chunk in chunks) { this.chunks.Add(chunk.Position, chunk); }
				}
			}
		}

		public bool TryGetChunk(ChunkPos position, [NotNullWhen(true)] out Chunk? chunk) => chunks.TryGetValue(position, out chunk);

		internal void MarkChunkDirty(ChunkPos position) {
			// TODO do other stuff, like save
			dirtyChunks.Add(position);
			worldRenderPass.EnqueueChunkIfCached(position);
		}

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