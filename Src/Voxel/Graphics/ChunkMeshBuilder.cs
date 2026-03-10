using Engine3.Exceptions;
using Engine3.Test.Voxel.Blocks;
using Engine3.Test.Voxel.Graphics.Vertex;
using Engine3.Test.Voxel.World;
using JetBrains.Annotations;

namespace Engine3.Test.Voxel.Graphics {
	public static unsafe class ChunkMeshBuilder {
		private const byte FaceCount = 6;
		private const byte VerticesPerFace = 4;

		[MustUseReturnValue]
		public static uint[] CreateChunkIndices(IWorldAccessor world, ChunkPos position, bool showBorder) { // TODO optimize. eventually use a compute shader?
			const byte ChunkSize = Chunk.Size - 1;

			List<uint> indices = new();
			if (!world.TryGetChunk(position, out IChunkAccessor? chunkAccessor)) { throw new Engine3Exception($"Failed to get chunk at {position}"); }

			if (chunkAccessor.IsEmpty) { return Array.Empty<uint>(); }

			world.TryGetChunk(position.Offset(0, 0, -1), out IChunkAccessor? northChunkAccessor);
			world.TryGetChunk(position.Offset(1, 0, 0), out IChunkAccessor? eastChunkAccessor);
			world.TryGetChunk(position.Offset(0, 0, 1), out IChunkAccessor? southChunkAccessor);
			world.TryGetChunk(position.Offset(-1, 0, 0), out IChunkAccessor? westChunkAccessor);
			world.TryGetChunk(position.Offset(0, 1, 0), out IChunkAccessor? upChunkAccessor);
			world.TryGetChunk(position.Offset(0, -1, 0), out IChunkAccessor? downChunkAccessor);

			uint offset = 0;
			for (ushort i = 0; i < Chunk.ArraySize; i++) {
				Block block = chunkAccessor.GetBlock(i);

				BlockFaceMask blockFaceMask = block.Properties.SolidFaceMask;
				if (blockFaceMask == BlockFaceMask.None) {
					offset += VerticesPerFace * FaceCount;
					continue;
				}

				Chunk.FromIndex(i, out byte x, out byte y, out byte z);

				bool isNorthVisible = z == 0 ?
						northChunkAccessor != null ? !northChunkAccessor.GetBlock(x, y, ChunkSize).Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.South) : showBorder :
						!chunkAccessor.GetBlock(x, y, (byte)(z - 1)).Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.South);

				bool isEastVisible = x == ChunkSize ?
						eastChunkAccessor != null ? !eastChunkAccessor.GetBlock(0, y, z).Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.West) : showBorder :
						!chunkAccessor.GetBlock((byte)(x + 1), y, z).Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.West);

				bool isSouthVisible = z == ChunkSize ?
						southChunkAccessor != null ? !southChunkAccessor.GetBlock(x, y, 0).Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.North) : showBorder :
						!chunkAccessor.GetBlock(x, y, (byte)(z + 1)).Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.North);

				bool isWestVisible = x == 0 ?
						westChunkAccessor != null ? !westChunkAccessor.GetBlock(ChunkSize, y, z).Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.East) : showBorder :
						!chunkAccessor.GetBlock((byte)(x - 1), y, z).Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.East);

				bool isUpVisible = y == ChunkSize ?
						upChunkAccessor != null ? !upChunkAccessor.GetBlock(x, 0, z).Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.Down) : showBorder :
						!chunkAccessor.GetBlock(x, (byte)(y + 1), z).Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.Down);

				bool isDownVisible = y == 0 ?
						downChunkAccessor != null ? !downChunkAccessor.GetBlock(x, ChunkSize, z).Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.Up) : showBorder :
						!chunkAccessor.GetBlock(x, (byte)(y - 1), z).Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.Up); //

				if (blockFaceMask.HasFlagFast(BlockFaceMask.North) && isNorthVisible) { indices.AddRange([ offset + 0u, offset + 1u, offset + 2u, offset + 2u, offset + 3u, offset + 0u, ]); }
				offset += VerticesPerFace;

				if (blockFaceMask.HasFlagFast(BlockFaceMask.East) && isEastVisible) { indices.AddRange([ offset + 0u, offset + 1u, offset + 2u, offset + 2u, offset + 3u, offset + 0u, ]); }
				offset += VerticesPerFace;

				if (blockFaceMask.HasFlagFast(BlockFaceMask.South) && isSouthVisible) { indices.AddRange([ offset + 0u, offset + 1u, offset + 2u, offset + 2u, offset + 3u, offset + 0u, ]); }
				offset += VerticesPerFace;

				if (blockFaceMask.HasFlagFast(BlockFaceMask.West) && isWestVisible) { indices.AddRange([ offset + 0u, offset + 1u, offset + 2u, offset + 2u, offset + 3u, offset + 0u, ]); }
				offset += VerticesPerFace;

				if (blockFaceMask.HasFlagFast(BlockFaceMask.Up) && isUpVisible) { indices.AddRange([ offset + 0u, offset + 1u, offset + 2u, offset + 2u, offset + 3u, offset + 0u, ]); }
				offset += VerticesPerFace;

				if (blockFaceMask.HasFlagFast(BlockFaceMask.Down) && isDownVisible) { indices.AddRange([ offset + 0u, offset + 1u, offset + 2u, offset + 2u, offset + 3u, offset + 0u, ]); }
				offset += VerticesPerFace;
			}

			return indices.ToArray();
		}

		[MustUseReturnValue]
		public static ChunkVertex[] GetChunkVertices() {
			ChunkVertex[] chunkVertices = new ChunkVertex[Chunk.ArraySize * FaceCount * VerticesPerFace];
			ChunkVertex[] cubeVertices = new ChunkVertex[FaceCount * VerticesPerFace];

			uint size = (uint)(FaceCount * VerticesPerFace * sizeof(ChunkVertex));

			fixed (ChunkVertex* chunkVerticesPtr = chunkVertices) {
				fixed (ChunkVertex* cubeVerticesPtr = cubeVertices) {
					for (ushort i = 0; i < Chunk.ArraySize; i++) {
						Chunk.FromIndex(i, out byte x, out byte y, out byte z);
						BuildCube(ref cubeVertices, x, y, z);
						Buffer.MemoryCopy(cubeVerticesPtr, chunkVerticesPtr + i * FaceCount * VerticesPerFace, size, size);
					}
				}
			}

			return chunkVertices;

			static void BuildCube(ref ChunkVertex[] vertices, byte x, byte y, byte z) {
				const byte VerticesPerFace = 4;
				const float Size = 1;

				uint vertexOffset = 0;

				BuildFace(ref vertices, vertexOffset, BlockFace.North, Size, x, y, z);
				vertexOffset += VerticesPerFace;

				BuildFace(ref vertices, vertexOffset, BlockFace.East, Size, x, y, z);
				vertexOffset += VerticesPerFace;

				BuildFace(ref vertices, vertexOffset, BlockFace.South, Size, x, y, z);
				vertexOffset += VerticesPerFace;

				BuildFace(ref vertices, vertexOffset, BlockFace.West, Size, x, y, z);
				vertexOffset += VerticesPerFace;

				BuildFace(ref vertices, vertexOffset, BlockFace.Up, Size, x, y, z);
				vertexOffset += VerticesPerFace;

				BuildFace(ref vertices, vertexOffset, BlockFace.Down, Size, x, y, z);
			}

			[MustUseReturnValue]
			static void BuildFace(ref ChunkVertex[] array, uint index, BlockFace face, float size, float x = 0, float y = 0, float z = 0) {
				float h = size / 2f;
				float x0 = x - h, x1 = x + h;
				float y0 = y - h, y1 = y + h;
				float z0 = z - h, z1 = z + h;

				const float U0 = 0, U1 = 1;
				const float V0 = 0, V1 = 1;

				switch (face) {
					case BlockFace.North: SetFace(ref array, index, new(x1, y1, z0, U0, V1, 0, 0, -1), new(x1, y0, z0, U0, V0, 0, 0, -1), new(x0, y0, z0, U1, V0, 0, 0, -1), new(x0, y1, z0, U1, V1, 0, 0, -1)); break; // Z-
					case BlockFace.East: SetFace(ref array, index, new(x1, y1, z1, U0, V1, 1, 0, 0), new(x1, y0, z1, U0, V0, 1, 0, 0), new(x1, y0, z0, U1, V0, 1, 0, 0), new(x1, y1, z0, U1, V1, 1, 0, 0)); break; // X+
					case BlockFace.South: SetFace(ref array, index, new(x0, y1, z1, U0, V1, 0, 0, 1), new(x0, y0, z1, U0, V0, 0, 0, 1), new(x1, y0, z1, U1, V0, 0, 0, 1), new(x1, y1, z1, U1, V1, 0, 0, 1)); break; // Z+
					case BlockFace.West: SetFace(ref array, index, new(x0, y1, z0, U0, V1, -1, 0, 0), new(x0, y0, z0, U0, V0, -1, 0, 0), new(x0, y0, z1, U1, V0, -1, 0, 0), new(x0, y1, z1, U1, V1, -1, 0, 0)); break; // X-
					case BlockFace.Up: SetFace(ref array, index, new(x0, y1, z0, U0, V1, 0, 1, 0), new(x0, y1, z1, U0, V0, 0, 1, 0), new(x1, y1, z1, U1, V0, 0, 1, 0), new(x1, y1, z0, U1, V1, 0, 1, 0)); break; // Y+
					case BlockFace.Down: SetFace(ref array, index, new(x1, y0, z0, U0, V1, 0, -1, 0), new(x1, y0, z1, U0, V0, 0, -1, 0), new(x0, y0, z1, U1, V0, 0, -1, 0), new(x0, y0, z0, U1, V1, 0, -1, 0)); break; // Y-
					default: throw new ArgumentOutOfRangeException(nameof(face), face, null);
				}

				return;

				void SetFace(ref ChunkVertex[] array, uint index, ChunkVertex v0, ChunkVertex v1, ChunkVertex v2, ChunkVertex v3) {
					array[index + 0] = v0; // U
					array[index + 1] = v1; //
					array[index + 2] = v2; //
					array[index + 3] = v3; //
				}
			}
		}
	}
}