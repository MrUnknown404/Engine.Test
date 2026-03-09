using Engine3.Client.Graphics.Vertex;
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
		public static uint[] CreateChunkIndices(World.World world, ChunkPos position, bool showBorder) { // TODO optimize. eventually use a compute shader?
			const byte ChunkSize = Chunk.Size - 1;

			List<uint> indices = new();
			if (!world.TryGetChunk(position, out Chunk? chunk)) { throw new Engine3Exception($"Failed to get chunk at {position}"); }

			world.TryGetChunk(position.Offset(0, 0, -1), out Chunk? northChunk);
			world.TryGetChunk(position.Offset(1, 0, 0), out Chunk? eastChunk);
			world.TryGetChunk(position.Offset(0, 0, 1), out Chunk? southChunk);
			world.TryGetChunk(position.Offset(-1, 0, 0), out Chunk? westChunk);
			world.TryGetChunk(position.Offset(0, 1, 0), out Chunk? upChunk);
			world.TryGetChunk(position.Offset(0, -1, 0), out Chunk? downChunk);

			uint offset = 0;
			for (ushort i = 0; i < Chunk.ArraySize; i++) {
				Block block = chunk[i];

				BlockFaceMask blockFaceMask = block.Properties.SolidFaceMask;
				if (blockFaceMask == BlockFaceMask.None) {
					offset += VerticesPerFace * FaceCount;
					continue;
				}

				Chunk.FromIndex(i, out byte x, out byte y, out byte z);

				bool isNorthVisible = z == 0 ?
						northChunk != null ? !northChunk[x, y, ChunkSize].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.South) : showBorder :
						!chunk[x, y, (byte)(z - 1)].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.South);

				bool isEastVisible = x == ChunkSize ?
						eastChunk != null ? !eastChunk[0, y, z].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.West) : showBorder :
						!chunk[(byte)(x + 1), y, z].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.West);

				bool isSouthVisible = z == ChunkSize ?
						southChunk != null ? !southChunk[x, y, 0].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.North) : showBorder :
						!chunk[x, y, (byte)(z + 1)].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.North);

				bool isWestVisible = x == 0 ?
						westChunk != null ? !westChunk[ChunkSize, y, z].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.East) : showBorder :
						!chunk[(byte)(x - 1), y, z].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.East);

				bool isUpVisible = y == ChunkSize ?
						upChunk != null ? !upChunk[x, 0, z].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.Down) : showBorder :
						!chunk[x, (byte)(y + 1), z].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.Down);

				bool isDownVisible = y == 0 ?
						downChunk != null ? !downChunk[x, ChunkSize, z].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.Up) : showBorder :
						!chunk[x, (byte)(y - 1), z].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.Up); //

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
			VertexXyzUvNormal[] cubeVertices = new VertexXyzUvNormal[FaceCount * VerticesPerFace];

			uint size = (uint)(FaceCount * VerticesPerFace * sizeof(VertexXyzUvNormal));

			fixed (ChunkVertex* chunkVerticesPtr = chunkVertices) {
				fixed (VertexXyzUvNormal* cubeVerticesPtr = cubeVertices) {
					for (ushort i = 0; i < Chunk.ArraySize; i++) {
						Chunk.FromIndex(i, out byte x, out byte y, out byte z);
						BuildCube(ref cubeVertices, x, y, z);
						Buffer.MemoryCopy(cubeVerticesPtr, chunkVerticesPtr + i * FaceCount * VerticesPerFace, size, size);
					}
				}
			}

			return chunkVertices;

			static void BuildCube(ref VertexXyzUvNormal[] vertices, byte x, byte y, byte z) {
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

			static void BuildFace(ref VertexXyzUvNormal[] array, uint index, BlockFace face, float size, float x = 0, float y = 0, float z = 0) {
				VertexXyzUvNormal[] vertices = BuildFace2(face, size, x, y, z); // TODO calc normals. can i hardcode them?
				for (int i = 0; i < vertices.Length; i++) { array[index + i] = vertices[i]; }
			}

			[MustUseReturnValue]
			static VertexXyzUvNormal[] BuildFace2(BlockFace face, float size, float x = 0, float y = 0, float z = 0) {
				float h = size / 2f;
				float x0 = x - h, x1 = x + h;
				float y0 = y - h, y1 = y + h;
				float z0 = z - h, z1 = z + h;

				const float U0 = 0, U1 = 1;
				const float V0 = 0, V1 = 1;

				return face switch { // (U shape)
						BlockFace.North => [
								new(x1, y1, z0, U0, V1, 0, 0, -1), // Z-
								new(x1, y0, z0, U0, V0, 0, 0, -1), //
								new(x0, y0, z0, U1, V0, 0, 0, -1), //
								new(x0, y1, z0, U1, V1, 0, 0, -1), //
						],
						BlockFace.East => [
								new(x1, y1, z1, U0, V1, 1, 0, 0), // X+
								new(x1, y0, z1, U0, V0, 1, 0, 0), //
								new(x1, y0, z0, U1, V0, 1, 0, 0), //
								new(x1, y1, z0, U1, V1, 1, 0, 0), //
						],
						BlockFace.South => [
								new(x0, y1, z1, U0, V1, 0, 0, 1), // Z+
								new(x0, y0, z1, U0, V0, 0, 0, 1), //
								new(x1, y0, z1, U1, V0, 0, 0, 1), //
								new(x1, y1, z1, U1, V1, 0, 0, 1), //
						],
						BlockFace.West => [
								new(x0, y1, z0, U0, V1, -1, 0, 0), // X-
								new(x0, y0, z0, U0, V0, -1, 0, 0), //
								new(x0, y0, z1, U1, V0, -1, 0, 0), //
								new(x0, y1, z1, U1, V1, -1, 0, 0), //
						],
						BlockFace.Up => [
								new(x0, y1, z0, U0, V1, 0, 1, 0), // Y+
								new(x0, y1, z1, U0, V0, 0, 1, 0), //
								new(x1, y1, z1, U1, V0, 0, 1, 0), //
								new(x1, y1, z0, U1, V1, 0, 1, 0), //
						],
						BlockFace.Down => [
								new(x1, y0, z0, U0, V1, 0, -1, 0), // Y-
								new(x1, y0, z1, U0, V0, 0, -1, 0), //
								new(x0, y0, z1, U1, V0, 0, -1, 0), //
								new(x0, y0, z0, U1, V1, 0, -1, 0), //
						],
						_ => throw new ArgumentOutOfRangeException(nameof(face), face, null),
				};
			}
		}
	}
}