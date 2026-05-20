using System.Numerics;
using Engine3.Client.Graphics;
using Engine3.Test.Voxel.Blocks;
using Engine3.Test.Voxel.Graphics.DataStructs;
using Engine3.Test.Voxel.Graphics.Vertex;
using Engine3.Test.Voxel.World;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Test.Voxel.Graphics.Renderers;

public class ChunkMeshBuffer {
	private readonly Dictionary<ChunkPos, ChunkMesh> cachedChunks = new();

	public uint ChunkCount => (uint)cachedChunks.Count;
	public IEnumerable<ChunkPos> CachedPositions => cachedChunks.Keys;

	public bool Contains(ChunkPos position) => cachedChunks.ContainsKey(position);

	public void Clear() => cachedChunks.Clear();

	internal void BuildDrawData(IWorldReader world, ChunkPos[] chunksToBuild, BlockTextureAtlas blockTextureAtlas) {
		foreach (ChunkPos pos in chunksToBuild) {
			if (!world.TryGetChunk(pos, out IChunkReader? chunk)) { continue; }
			cachedChunks[pos] = !chunk.IsEmpty ? CreateChunkMesh(world, chunk, blockTextureAtlas, true) : ChunkMesh.EmptyMesh;
		}
	}

	internal void GetDrawData(out ChunkVertex[] outVertices, out uint[] outIndices, out VkDrawIndexedIndirectCommand[] outCommands, out StructBuffer<PerChunkData> perChunkBuffer) {
		KeyValuePair<ChunkPos, ChunkMesh>[] pairs = cachedChunks.ToArray();

		List<ChunkVertex> vertices = new();
		List<uint> indices = new();
		List<VkDrawIndexedIndirectCommand> commands = new();
		List<PerChunkData> perChunkData = new();

		int vertexOffset = 0;
		uint indexOffset = 0;

		foreach ((ChunkPos pos, ChunkMesh mesh) in pairs) {
			if (mesh.IsEmpty) { continue; }
			if (mesh.Vertices.Length == 0 || mesh.Indices.Length == 0) { continue; }

			vertices.AddRange(mesh.Vertices);
			indices.AddRange(mesh.Indices);

			commands.Add(new() {
					indexCount = (uint)mesh.Indices.Length, //
					instanceCount = 1, //
					firstIndex = indexOffset, //
					vertexOffset = vertexOffset, //
					firstInstance = 0, //
			});

			vertexOffset += mesh.Vertices.Length;
			indexOffset += (uint)mesh.Indices.Length;

			perChunkData.Add(new(pos));
		}

		// probably should use spans or something
		outVertices = vertices.ToArray();
		outIndices = indices.ToArray();
		outCommands = commands.ToArray();
		perChunkBuffer = new(perChunkData.ToArray());
	}

	[MustUseReturnValue]
	private static ChunkMesh CreateChunkMesh(IWorldReader world, IChunkReader chunk, BlockTextureAtlas blockTextureAtlas, bool showBorder) { // TODO have a compute shader do this later
		const byte ChunkSize = Chunk.Size - 1;
		const float BlockSize = 1;

		List<ChunkVertex> vertices = new();
		List<uint> indices = new();

		ChunkPos chunkPos = chunk.Position;

		world.TryGetChunk(chunkPos.Offset(0, 0, -1), out IChunkReader? northChunk);
		world.TryGetChunk(chunkPos.Offset(1, 0, 0), out IChunkReader? eastChunk);
		world.TryGetChunk(chunkPos.Offset(0, 0, 1), out IChunkReader? southChunk);
		world.TryGetChunk(chunkPos.Offset(-1, 0, 0), out IChunkReader? westChunk);
		world.TryGetChunk(chunkPos.Offset(0, 1, 0), out IChunkReader? upChunk);
		world.TryGetChunk(chunkPos.Offset(0, -1, 0), out IChunkReader? downChunk);

		uint indexOffset = 0;

		for (ushort i = 0; i < Chunk.ArraySize; i++) {
			Block block = chunk.GetBlockState(i).Block;

			BlockFaceMask blockFaceMask = block.Properties.SolidFaceMask;
			if (blockFaceMask == BlockFaceMask.None) { continue; }

			Chunk.FromIndex(i, out byte x, out byte y, out byte z);

			Vector2 uvs = blockTextureAtlas.GetUVsForBlock(block);
			float u0 = uvs.X, u1 = u0 + blockTextureAtlas.TextureSizeInUVs;
			float v0 = uvs.Y, v1 = v0 + blockTextureAtlas.TextureSizeInUVs;

			if (blockFaceMask.HasFlagFast(BlockFaceMask.North) && IsFaceVisible(chunk, northChunk, BlockFaceMask.South, z == 0, x, y, ChunkSize, x, y, (byte)(z - 1), showBorder)) {
				BuildFace(vertices, indices, ref indexOffset, BlockFace.North, BlockSize, x, y, z, u0, u1, v0, v1);
			}

			if (blockFaceMask.HasFlagFast(BlockFaceMask.East) && IsFaceVisible(chunk, eastChunk, BlockFaceMask.West, x == ChunkSize, 0, y, z, (byte)(x + 1), y, z, showBorder)) {
				BuildFace(vertices, indices, ref indexOffset, BlockFace.East, BlockSize, x, y, z, u0, u1, v0, v1);
			}

			if (blockFaceMask.HasFlagFast(BlockFaceMask.South) && IsFaceVisible(chunk, southChunk, BlockFaceMask.North, z == ChunkSize, x, y, 0, x, y, (byte)(z + 1), showBorder)) {
				BuildFace(vertices, indices, ref indexOffset, BlockFace.South, BlockSize, x, y, z, u0, u1, v0, v1);
			}

			if (blockFaceMask.HasFlagFast(BlockFaceMask.West) && IsFaceVisible(chunk, westChunk, BlockFaceMask.East, x == 0, ChunkSize, y, z, (byte)(x - 1), y, z, showBorder)) {
				BuildFace(vertices, indices, ref indexOffset, BlockFace.West, BlockSize, x, y, z, u0, u1, v0, v1);
			}

			if (blockFaceMask.HasFlagFast(BlockFaceMask.Up) && IsFaceVisible(chunk, upChunk, BlockFaceMask.Down, y == ChunkSize, x, 0, z, x, (byte)(y + 1), z, showBorder)) {
				BuildFace(vertices, indices, ref indexOffset, BlockFace.Up, BlockSize, x, y, z, u0, u1, v0, v1);
			}

			if (blockFaceMask.HasFlagFast(BlockFaceMask.Down) && IsFaceVisible(chunk, downChunk, BlockFaceMask.Up, y == 0, x, ChunkSize, z, x, (byte)(y - 1), z, showBorder)) {
				BuildFace(vertices, indices, ref indexOffset, BlockFace.Down, BlockSize, x, y, z, u0, u1, v0, v1);
			}
		}

		return new(vertices.ToArray(), indices.ToArray());

		[MustUseReturnValue]
		static bool IsFaceVisible(IChunkReader chunk, IChunkReader? faceChunk, BlockFaceMask face, bool predicate, byte x0, byte y0, byte z0, byte x1, byte y1, byte z1, bool showBorder) =>
				predicate ?
						faceChunk != null ? !faceChunk.GetBlockState(x0, y0, z0).Block.Properties.SolidFaceMask.HasFlagFast(face) : showBorder :
						!chunk.GetBlockState(x1, y1, z1).Block.Properties.SolidFaceMask.HasFlagFast(face);

		static void BuildFace(List<ChunkVertex> vertices, List<uint> indices, ref uint indexOffset, BlockFace face, float size, float x, float y, float z, float u0, float u1, float v0, float v1) {
			const byte VerticesPerFace = 4;

			float x1 = x + size;
			float y1 = y + size;
			float z1 = z + size;

			switch (face) {
				case BlockFace.North: // Z-
					vertices.Add(new(x1, y1, z, u0, v1, 0, 0, -1));
					vertices.Add(new(x1, y, z, u0, v0, 0, 0, -1));
					vertices.Add(new(x, y, z, u1, v0, 0, 0, -1));
					vertices.Add(new(x, y1, z, u1, v1, 0, 0, -1));
					break;
				case BlockFace.East: // X+
					vertices.Add(new(x1, y1, z1, u0, v1, 1, 0, 0));
					vertices.Add(new(x1, y, z1, u0, v0, 1, 0, 0));
					vertices.Add(new(x1, y, z, u1, v0, 1, 0, 0));
					vertices.Add(new(x1, y1, z, u1, v1, 1, 0, 0));
					break;
				case BlockFace.South: // Z+
					vertices.Add(new(x, y1, z1, u0, v1, 0, 0, 1));
					vertices.Add(new(x, y, z1, u0, v0, 0, 0, 1));
					vertices.Add(new(x1, y, z1, u1, v0, 0, 0, 1));
					vertices.Add(new(x1, y1, z1, u1, v1, 0, 0, 1));
					break;
				case BlockFace.West: // X-
					vertices.Add(new(x, y1, z, u0, v1, -1, 0, 0));
					vertices.Add(new(x, y, z, u0, v0, -1, 0, 0));
					vertices.Add(new(x, y, z1, u1, v0, -1, 0, 0));
					vertices.Add(new(x, y1, z1, u1, v1, -1, 0, 0));
					break;
				case BlockFace.Up: // Y+
					vertices.Add(new(x, y1, z, u0, v1, 0, 1, 0));
					vertices.Add(new(x, y1, z1, u0, v0, 0, 1, 0));
					vertices.Add(new(x1, y1, z1, u1, v0, 0, 1, 0));
					vertices.Add(new(x1, y1, z, u1, v1, 0, 1, 0));
					break;
				case BlockFace.Down: // Y-
					vertices.Add(new(x1, y, z, u0, v1, 0, -1, 0));
					vertices.Add(new(x1, y, z1, u0, v0, 0, -1, 0));
					vertices.Add(new(x, y, z1, u1, v0, 0, -1, 0));
					vertices.Add(new(x, y, z, u1, v1, 0, -1, 0));
					break;
				default: throw new ArgumentOutOfRangeException(nameof(face), face, null);
			}

			indices.Add(indexOffset + 0);
			indices.Add(indexOffset + 1);
			indices.Add(indexOffset + 2);
			indices.Add(indexOffset + 2);
			indices.Add(indexOffset + 3);
			indices.Add(indexOffset + 0);

			indexOffset += VerticesPerFace;
		}
	}
}