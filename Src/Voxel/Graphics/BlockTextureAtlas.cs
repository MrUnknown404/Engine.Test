using System.Collections.Frozen;
using System.Numerics;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Test.Voxel.Blocks;
using Engine3.Test.Voxel.Registries;
using Engine3.Test.Voxel.World;
using Engine3.Utility;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.Vulkan;
using StbiSharp;

namespace Engine3.Test.Voxel.Graphics;

public unsafe class BlockTextureAtlas {
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public VulkanImage Image { get; }

	public byte TextureSize { get; }
	public byte AtlasSize { get; }
	public ushort AtlasSizeInPixels { get; }
	public float TextureSizeInUVs { get; }

	private readonly FrozenDictionary<Block, Vector2> blockUVMap;

	public BlockTextureAtlas(VulkanResourceProvider graphicsResourceProvider, SurfaceCapablePhysicalGpu physicalGpu, LogicalGpu logicalGpu, TransferCommandPool transferCommandPool, BakedMasterRegistry<Block> blocks,
		byte textureSize) {
		const byte ColorChannels = 4;

		Block[] validBlocks = blocks.AllObjectsOrdered.Where(static b => b.Properties.SolidFaceMask != BlockFaceMask.None).ToArray();

		TextureSize = textureSize;
		AtlasSize = (byte)(validBlocks.Length == 1 ? 1 : (uint)MathF.Sqrt(validBlocks.Length) + 1);
		AtlasSizeInPixels = (ushort)(AtlasSize * TextureSize);
		TextureSizeInUVs = (float)TextureSize / (AtlasSize * TextureSize);

		Dictionary<Block, Vector2> blockUVMap = new();

		Logger.Debug($"Creating atlas of size {AtlasSize}");

		byte[] imageData = new byte[AtlasSizeInPixels * AtlasSizeInPixels * ColorChannels];

		ushort x = 0;
		ushort y = (ushort)(AtlasSize - 1);

		foreach (Block block in validBlocks) {
			if (block.Properties.SolidFaceMask == BlockFaceMask.None) { continue; }

			blockUVMap[block] = new((float)x / AtlasSize, (float)y / AtlasSize);

			using (StbiImage stbiImage = AssetH.LoadImage($"Voxel.Blocks.{block.RegistryKey.Key}", "png", ColorChannels, block.RegistryKey.Source.Assembly)) {
				Blit(ref imageData, stbiImage.Data, x, y); // TODO do on gpu?

				x++;

				if (x == AtlasSize) {
					x = 0;
					y--;
				}
			}
		}

		this.blockUVMap = blockUVMap.ToFrozenDictionary();

#if DEBUG
		// TODO dump file
#endif

		Image = graphicsResourceProvider.CreateImage("Block Texture Atlas", AtlasSizeInPixels, AtlasSizeInPixels, VkFormat.FormatR8g8b8a8Srgb);
		transferCommandPool.CopyToImage(Image, physicalGpu.QueueFamilyIndices, logicalGpu.TransferQueue, AtlasSizeInPixels, AtlasSizeInPixels, ColorChannels, imageData);

		return;

		void Blit(ref byte[] destination, ReadOnlySpan<byte> source, ushort x, ushort y) {
			uint textureSizeWithColorChannels = (uint)(TextureSize * ColorChannels);
			uint atlasSizeWithColorChannels = (uint)(AtlasSizeInPixels * ColorChannels);
			uint yOffset = (uint)(y * TextureSize);

			fixed (byte* sourcePtr = source) {
				fixed (byte* destinationPtr = destination) {
					for (int yi = 0; yi < TextureSize; yi++) {
						long dstIndex = (yOffset + yi) * atlasSizeWithColorChannels + x * textureSizeWithColorChannels;
						long yiOffset = yi * textureSizeWithColorChannels;

						Buffer.MemoryCopy(sourcePtr + yiOffset, destinationPtr + dstIndex, textureSizeWithColorChannels, textureSizeWithColorChannels);
					}
				}
			}
		}
	}

	[MustUseReturnValue] public Vector2 GetUVsForBlock(Block block) => blockUVMap[block];
}