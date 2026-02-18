using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Engine3.Client;
using Engine3.Client.Graphics;
using Engine3.Client.Graphics.ImGui.Makers;
using Engine3.Client.Graphics.ImGui.Providers;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Test.Core.Graphics;
using Engine3.Utility;
using ImGuiNET;
using NLog;
using OpenTK.Graphics.Vulkan;
using StbiSharp;

namespace Engine3.Test.Voxel.Graphics {
	public unsafe class VoxelRenderer : VulkanRenderer {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private const string TestShaderName = "Test";

		public World.World? World { private get; set; }

		private GraphicsPipeline? cubeGraphicsPipeline; // TODO remove nullability
		private GraphicsPipeline? chunkGraphicsPipeline;

		private DepthImage? depthImage;

		private DescriptorBuffers? cameraUniformBuffer;

		private VulkanBuffer? cubeVertexBuffer; // TODO remove
		private VulkanBuffer? cubeIndexBuffer;
		private DescriptorBuffers? cubeInstanceBuffers;
		private DescriptorSets? cubeDescriptorSet;

		private VulkanBuffer? chunkVertexBuffer;
		private VulkanBuffer? chunkIndexBuffer;
		private DescriptorSets? chunkDescriptorSet;

		private VulkanImage? image;
		private TextureSampler? textureSampler;

		private readonly Camera camera;

		private readonly VertexXyzUvRgb[] cubeVertices;
		private readonly uint[] cubeIndices = [
				6, 2, 3, 3, 7, 6, // X-
				4, 0, 1, 1, 5, 4, // X+
				0, 1, 2, 2, 3, 0, // Y-
				4, 5, 6, 6, 7, 4, // Y+
				7, 3, 0, 0, 4, 7, // Z-
				5, 1, 2, 2, 6, 5, // Z+ (textured atm)
		];

		private readonly Vector3 cubePosition = new(0, 0, -5);
		private readonly ObjectUniformBuffer cubeUniformBufferValue = new(1);
		private readonly ChunkUniformBuffer chunkUniformBufferValue = new(1); // TODO offset chunk by this

		private readonly Assembly gameAssembly;

		protected override DepthImage? DepthImage => depthImage;

		private bool shouldRegenerateChunk = true;

		private ulong chunkIndicesCount;

		public VoxelRenderer(GameClient game, VulkanGraphicsBackend graphicsBackend, VulkanWindow window, Camera camera, Assembly gameAssembly) : base(graphicsBackend, window) {
			this.camera = camera;
			this.gameAssembly = gameAssembly;

			ImGuiBackend = new(window, graphicsBackend.MaxFramesInFlight, new DemoWindowImGui()) { ShowDebugUI = true, DebugUIImGui = new DebugUIImGui(game, window) { AddExtraDebugUI = AddExtraDebugUI, }, };

			const float Size = 1;
			const float H = Size / 2;
			const float R = 1, G = 1, B = 1;
			const float U = 0, V = 0;

			const float X0 = -H, X1 = +H;
			const float Y0 = -H, Y1 = +H;
			const float Z0 = -H, Z1 = +H;

			cubeVertices = [
					new(X1, Y0, Z0, U, V, R, G, B), // 0
					new(X1, Y0, Z1, 1, 0, R, G, B), // 1
					new(X0, Y0, Z1, 0, 0, R, G, B), // 2
					new(X0, Y0, Z0, U, V, R, G, B), // 3
					new(X1, Y1, Z0, U, V, R, G, B), // 4
					new(X1, Y1, Z1, 1, 1, R, G, B), // 5
					new(X0, Y1, Z1, 0, 1, R, G, B), // 6
					new(X0, Y1, Z0, U, V, R, G, B), // 7
			];
		}

		private void AddExtraDebugUI(float indentAmount) {
			ImGuiH.IndentedCollapsingHeader("Camera", indentAmount, DrawFunc);

			ImGui.Text("test");

			return;

			void DrawFunc() => CameraImGuiMaker.ShowImGui(camera); // this should be faster than a lambda?
		}

		public override void Setup() {
			base.Setup();

			CreateCubeGraphicsPipeline(out DescriptorSetLayout cubeDescriptorSetLayout);
			CreateChunkGraphicsPipeline(out DescriptorSetLayout chunkDescriptorSetLayout);

			CreateBuffers();

			CreateSamplerAndTextures();

			CreateCubeDescriptorSets(cubeDescriptorSetLayout.VkDescriptorSetLayout);
			CreateChunkDescriptorSets(chunkDescriptorSetLayout.VkDescriptorSetLayout);
			UpdateDescriptorSets();

			depthImage = LogicalGpu.CreateDepthImage(TransferCommandPool, SwapChain.Extent);
		}

		private void CreateCubeGraphicsPipeline(out DescriptorSetLayout descriptorSetLayout) {
			VulkanShader vertexShader = LogicalGpu.CreateShader($"{TestShaderName} Vertex Shader", TestShaderName, ShaderLanguage.Glsl, ShaderType.Vertex, gameAssembly);
			VulkanShader fragmentShader = LogicalGpu.CreateShader($"{TestShaderName} Fragment Shader", TestShaderName, ShaderLanguage.Glsl, ShaderType.Fragment, gameAssembly);

			descriptorSetLayout = LogicalGpu.CreateDescriptorSetLayout([
					new(VkDescriptorType.DescriptorTypeUniformBuffer, VkShaderStageFlagBits.ShaderStageVertexBit, 0), //
					new(VkDescriptorType.DescriptorTypeStorageBuffer, VkShaderStageFlagBits.ShaderStageVertexBit, 1), //
					new(VkDescriptorType.DescriptorTypeCombinedImageSampler, VkShaderStageFlagBits.ShaderStageFragmentBit, 2), //
			]);

			// ew
			cubeGraphicsPipeline = LogicalGpu.CreateGraphicsPipeline(
				new("Test Graphics Pipeline", SwapChain.ImageFormat, [ vertexShader, fragmentShader, ], VertexXyzUvRgb.GetAttributeDescriptions(), VertexXyzUvRgb.GetBindingDescriptions()) {
						DescriptorSetLayouts = [ descriptorSetLayout.VkDescriptorSetLayout, ],
						// FrontFace = VkFrontFace.FrontFaceCounterClockwise, // TODO oops. most indices are backwards
						CullMode = VkCullModeFlagBits.CullModeNone,
				});

			Logger.Debug("Created cube graphics pipeline");

			LogicalGpu.EnqueueDestroy(vertexShader);
			LogicalGpu.EnqueueDestroy(fragmentShader);
		}

		private void CreateChunkGraphicsPipeline(out DescriptorSetLayout descriptorSetLayout) {
			VulkanShader vertexShader = LogicalGpu.CreateShader("Chunk Vertex Shader", "Chunk", ShaderLanguage.Glsl, ShaderType.Vertex, gameAssembly);
			VulkanShader fragmentShader = LogicalGpu.CreateShader("Chunk Fragment Shader", "Chunk", ShaderLanguage.Glsl, ShaderType.Fragment, gameAssembly);

			descriptorSetLayout = LogicalGpu.CreateDescriptorSetLayout([
					new(VkDescriptorType.DescriptorTypeUniformBuffer, VkShaderStageFlagBits.ShaderStageVertexBit, 0), //
			]);

			chunkGraphicsPipeline = LogicalGpu.CreateGraphicsPipeline(
				new("Chunk Graphics Pipeline", SwapChain.ImageFormat, [ vertexShader, fragmentShader, ], ChunkVertex.GetAttributeDescriptions(), ChunkVertex.GetBindingDescriptions()) {
						DescriptorSetLayouts = [ descriptorSetLayout.VkDescriptorSetLayout, ],
						// CullMode = VkCullModeFlagBits.CullModeNone,
				});

			Logger.Debug("Created chunk graphics pipeline");

			LogicalGpu.EnqueueDestroy(vertexShader);
			LogicalGpu.EnqueueDestroy(fragmentShader);
		}

		private void CreateBuffers() {
			// cube
			cubeVertexBuffer = LogicalGpu.CreateBuffer("Cube Vertex Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageVertexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, (ulong)(sizeof(VertexXyzUvRgb) * cubeVertices.Length));

			cubeIndexBuffer = LogicalGpu.CreateBuffer("Cube Index Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit,
				(ulong)(sizeof(uint) * cubeIndices.Length));

			ChunkVertex[] vertices = ChunkMeshBuilder.GetChunkVertices();

			// chunk
			chunkVertexBuffer = LogicalGpu.CreateBuffer("Chunk Vertex Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageVertexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, (ulong)(sizeof(ChunkVertex) * vertices.Length));

			chunkIndexBuffer = LogicalGpu.CreateBuffer("Chunk Index Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit,
				1);

			// copy
			TransferCommandPool.CopyToBuffers([
					TransferCommandPool.CopyDataToBufferInfo.Copy(cubeVertexBuffer, cubeVertices), TransferCommandPool.CopyDataToBufferInfo.Copy(cubeIndexBuffer, cubeIndices),
					TransferCommandPool.CopyDataToBufferInfo.Copy(chunkVertexBuffer, vertices),
			]);

			Logger.Debug("Created & copied vertex/index buffers");

			// descriptor buffers
			cameraUniformBuffer = LogicalGpu.CreateDescriptorBuffers("Camera Uniform Buffer", (ulong)sizeof(CameraUniformBuffer), MaxFramesInFlight, VkDescriptorType.DescriptorTypeUniformBuffer,
				VkBufferUsageFlagBits.BufferUsageUniformBufferBit);

			cubeInstanceBuffers = LogicalGpu.CreateDescriptorBuffers("Cube Instance Storage Buffers", cubeUniformBufferValue.Size, MaxFramesInFlight, VkDescriptorType.DescriptorTypeStorageBuffer,
				VkBufferUsageFlagBits.BufferUsageStorageBufferBit);

			Logger.Debug("Created uniform buffers");
		}

		private void CreateSamplerAndTextures() {
			textureSampler = LogicalGpu.CreateSampler(new(VkFilter.FilterLinear, VkFilter.FilterLinear, Window.SelectedGpu.PhysicalDeviceProperties2.properties.limits));
			Logger.Debug("Created texture sampler");

			using (StbiImage stbiImage = AssetH.LoadImage("Test.64x64", "png", 4, gameAssembly)) {
				image = LogicalGpu.CreateImage("Test 64x64 Image", (uint)stbiImage.Width, (uint)stbiImage.Height, VkFormat.FormatR8g8b8a8Srgb);
				TransferCommandPool.CopyToImage(image, PhysicalGpu.QueueFamilyIndices, LogicalGpu.TransferQueue, stbiImage);
			}

			Logger.Debug("Created image");
		}

		private void CreateCubeDescriptorSets(VkDescriptorSetLayout descriptorSetLayout) {
			DescriptorPool descriptorPool = LogicalGpu.CreateDescriptorPool([ VkDescriptorType.DescriptorTypeUniformBuffer, VkDescriptorType.DescriptorTypeStorageBuffer, VkDescriptorType.DescriptorTypeCombinedImageSampler, ], 3,
				MaxFramesInFlight);

			cubeDescriptorSet = descriptorPool.AllocateDescriptorSet(descriptorSetLayout);
			Logger.Debug("Created cube descriptor sets");
		}

		private void CreateChunkDescriptorSets(VkDescriptorSetLayout descriptorSetLayout) {
			DescriptorPool descriptorPool = LogicalGpu.CreateDescriptorPool([ VkDescriptorType.DescriptorTypeUniformBuffer, ], 1, MaxFramesInFlight);

			chunkDescriptorSet = descriptorPool.AllocateDescriptorSet(descriptorSetLayout);
			Logger.Debug("Created chunk descriptor sets");
		}

		private void UpdateDescriptorSets() {
			if (cubeInstanceBuffers == null || cameraUniformBuffer == null || image == null || textureSampler == null || cubeDescriptorSet == null || chunkDescriptorSet == null) { throw new NullReferenceException(); }

			cubeDescriptorSet.UpdateDescriptorSet(0, cameraUniformBuffer);
			cubeDescriptorSet.UpdateDescriptorSet(1, cubeInstanceBuffers);
			cubeDescriptorSet.UpdateDescriptorSet(2, image.ImageView, textureSampler.Sampler);

			chunkDescriptorSet.UpdateDescriptorSet(0, cameraUniformBuffer);

			Logger.Debug("Updated descriptor sets");
		}

		protected override void RecordCommandBuffer(GraphicsCommandBuffer graphicsCommandBuffer) {
			if (cubeGraphicsPipeline == null ||
				chunkGraphicsPipeline == null ||
				cubeVertexBuffer == null ||
				cubeIndexBuffer == null ||
				cubeDescriptorSet == null ||
				chunkVertexBuffer == null ||
				chunkIndexBuffer == null ||
				chunkDescriptorSet == null) { throw new NullReferenceException(); }

			graphicsCommandBuffer.CmdSetViewport(0, 0, SwapChain.Extent.width, SwapChain.Extent.height, 0, 1);
			graphicsCommandBuffer.CmdSetScissor(0, 0, SwapChain.Extent);

			// Cube
			graphicsCommandBuffer.CmdBindGraphicsPipeline(cubeGraphicsPipeline.Pipeline); // TODO integrate graphics pipeline binding into VulkanRenderer?

			graphicsCommandBuffer.CmdBindDescriptorSet(cubeGraphicsPipeline.Layout, cubeDescriptorSet.GetCurrent(FrameIndex), VkShaderStageFlagBits.ShaderStageVertexBit | VkShaderStageFlagBits.ShaderStageFragmentBit);
			graphicsCommandBuffer.CmdBindVertexBuffer(cubeVertexBuffer, 0);
			graphicsCommandBuffer.CmdBindIndexBuffer(cubeIndexBuffer, cubeIndexBuffer.BufferSize);
			graphicsCommandBuffer.CmdDrawIndexed((uint)cubeIndices.Length, 1, 0, 0, 0);

			if (World is null) { return; }

			// Chunk
			graphicsCommandBuffer.CmdBindGraphicsPipeline(chunkGraphicsPipeline.Pipeline);

			graphicsCommandBuffer.CmdBindDescriptorSet(chunkGraphicsPipeline.Layout, chunkDescriptorSet.GetCurrent(FrameIndex), VkShaderStageFlagBits.ShaderStageVertexBit);
			graphicsCommandBuffer.CmdBindVertexBuffer(chunkVertexBuffer, 0);
			graphicsCommandBuffer.CmdBindIndexBuffer(chunkIndexBuffer, chunkIndexBuffer.BufferSize);
			graphicsCommandBuffer.CmdDrawIndexed((uint)chunkIndicesCount, 1, 0, 0, 0);
		}

		protected override void CopyBuffers(float delta) {
			if (cubeInstanceBuffers == null || cameraUniformBuffer == null || chunkIndexBuffer == null) { throw new NullReferenceException(); }

			cubeUniformBufferValue.Models[0] = Matrix4x4.CreateRotationY(float.Lerp(VoxelTest.PrevCubeRotation, VoxelTest.CubeRotation, delta) * float.DegreesToRadians(90f)) * Matrix4x4.CreateTranslation(cubePosition);

			cameraUniformBuffer.Copy(new CameraUniformBuffer(camera.Projection with { M22 = -camera.Projection.M22, }, camera.View), FrameIndex); // TODO lerp camera position & rotation
			cubeInstanceBuffers.Copy(MemoryMarshal.AsBytes(cubeUniformBufferValue.Models), FrameIndex);

			if (shouldRegenerateChunk) {
				if (World is null) { return; }

				uint[] indices = ChunkMeshBuilder.CreateChunkIndices(World.Chunk);
				chunkIndicesCount = (ulong)indices.Length;
				ulong bufferSize = chunkIndicesCount * sizeof(uint);

				if (chunkIndexBuffer.BufferSize < bufferSize) {
					LogicalGpu.EnqueueDestroy(chunkIndexBuffer);

					chunkIndexBuffer = LogicalGpu.CreateBuffer(chunkIndexBuffer.DebugName, VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit,
						VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, bufferSize);
				}

				TransferCommandPool.CopyToBuffer(chunkIndexBuffer, indices);

				shouldRegenerateChunk = false;
			}
		}

		public void MarkChunkDirty() => shouldRegenerateChunk = true;

		protected override void Cleanup() {
			//

			base.Cleanup();
		}
	}
}