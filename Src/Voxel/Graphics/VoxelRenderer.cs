using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Engine3.Client;
using Engine3.Client.Graphics;
using Engine3.Client.Graphics.DataStructs;
using Engine3.Client.Graphics.ImGui.Makers;
using Engine3.Client.Graphics.ImGui.Providers;
using Engine3.Client.Graphics.Vertex;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Test.Core.Graphics;
using Engine3.Test.Voxel.Graphics.Vertex;
using Engine3.Utility;
using ImGuiNET;
using NLog;
using OpenTK.Graphics.Vulkan;
using StbiSharp;
using Vector3 = System.Numerics.Vector3;

namespace Engine3.Test.Voxel.Graphics {
	public unsafe class VoxelRenderer : VulkanNodeRenderer {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private XyzGizmoRecorderNode? xyzGizmoRecorderNode;

		public World.World? World { private get; set; }

		private GraphicsPipeline cubeGraphicsPipeline = null!;
		private GraphicsPipeline chunkGraphicsPipeline = null!;

		private DepthImage depthImage = null!;

		private DescriptorBuffers cameraUniformBuffer = null!;

		private VulkanBuffer cubeVertexBuffer = null!; // TODO for testing. remove
		private VulkanBuffer cubeIndexBuffer = null!;
		private DescriptorBuffers cubeInstanceBuffers = null!;
		private DescriptorSets cubeDescriptorSet = null!;

		private VulkanBuffer chunkVertexBuffer = null!;
		private VulkanBuffer chunkIndexBuffer = null!;
		private DescriptorSets chunkDescriptorSet = null!;

		private VulkanImage image = null!;
		private TextureSampler textureSampler = null!;

		private readonly Camera camera;

		private readonly VertexXyzUvRgb[] cubeVertices;
		private readonly uint[] cubeIndices;

		private readonly Vector3 cubePosition = new(0, 0, -5);
		private readonly ObjectUniformBuffer cubeUniformBufferValue = new(1);
		private readonly ChunkUniformBuffer chunkUniformBufferValue = new(1); // TODO offset chunk by this

		private readonly Assembly gameAssembly;

		protected override DepthImage DepthImage => depthImage;

		private bool shouldRegenerateChunk = true;
		private ulong chunkIndicesCount;

		public VoxelRenderer(GameClient game, VulkanGraphicsBackend graphicsBackend, VulkanWindow window, Camera camera, Assembly gameAssembly) : base(graphicsBackend, window) {
			this.camera = camera;
			this.gameAssembly = gameAssembly;

			ImGuiBackend = new(window, graphicsBackend.Settings.MaxFramesInFlight, new DemoWindowImGui()) { ShowDebugUI = true, DebugUIImGui = new DebugUIImGui(game, window) { AddExtraDebugUI = AddExtraDebugUI, }, };

			// cubeVertices = CubeBuilder.BuildCube(BlockFaceMask.Up, 1);
			// // cubeIndices = [ 0, 3, 2, 2, 1, 0, ];
			//
			// const byte CubeFaceCount = 6;
			// const byte IndicesPerFace = 6;
			// const byte VertexPerFace = 4;
			//
			// uint[] arr = [ 0, 3, 2, 2, 1, 0, ];
			// cubeIndices = new uint[IndicesPerFace * CubeFaceCount];
			//
			// uint faceCount = (uint)(cubeVertices.Length / VertexPerFace);
			//
			// for (int i = 0; i < faceCount; i++) {
			// 	for (int j = 0; j < IndicesPerFace; j++) { cubeIndices[i * IndicesPerFace + j] = (uint)(i * VertexPerFace) + arr[j]; }
			// }
			//
			// Logger.Warn(cubeVertices.GetSizeAndElementsAsString());
			// Logger.Warn(cubeIndices.GetSizeAndElementsAsString());

			const float Size = 1;
			const float H = Size / 2;
			const float R = 1, G = 1, B = 1;

			const float X0 = -H, X1 = +H;
			const float Y0 = -H, Y1 = +H;
			const float Z0 = -H, Z1 = +H;

			cubeVertices = [
					new(X0, Y0, Z0, 0, 0, R, G, B), // 0 Y-
					new(X0, Y0, Z1, 1, 0, R, G, B), // 1
					new(X1, Y0, Z1, 1, 1, R, G, B), // 2
					new(X1, Y0, Z0, 0, 1, R, G, B), // 3

					new(X1, Y1, Z0, 0, 1, R, G, B), // 3 Y+
					new(X1, Y1, Z1, 1, 1, R, G, B), // 2
					new(X0, Y1, Z1, 1, 0, R, G, B), // 1
					new(X0, Y1, Z0, 0, 0, R, G, B), // 0

					new(X0, Y1, Z0, 1, 1, R, G, B), // 3 Z-
					new(X0, Y0, Z0, 1, 0, R, G, B), // 2
					new(X1, Y0, Z0, 0, 0, R, G, B), // 1
					new(X1, Y1, Z0, 0, 1, R, G, B), // 0

					new(X0, Y1, Z1, 0, 1, R, G, B), // 3 Z+
					new(X1, Y1, Z1, 1, 1, R, G, B), // 0
					new(X1, Y0, Z1, 1, 0, R, G, B), // 1
					new(X0, Y0, Z1, 0, 0, R, G, B), // 2

					new(X0, Y0, Z0, 0, 0, R, G, B), // 2 X-
					new(X0, Y1, Z0, 0, 1, R, G, B), // 3
					new(X0, Y1, Z1, 1, 1, R, G, B), // 0
					new(X0, Y0, Z1, 1, 0, R, G, B), // 1

					new(X1, Y1, Z0, 1, 1, R, G, B), // 3 X+
					new(X1, Y0, Z0, 1, 0, R, G, B), // 2
					new(X1, Y0, Z1, 0, 0, R, G, B), // 1
					new(X1, Y1, Z1, 0, 1, R, G, B), // 0
			];

			cubeIndices = [
					0, 1, 3, 3, 1, 2, // Y-
					4 + 0, 4 + 1, 4 + 3, 4 + 3, 4 + 1, 4 + 2, // Y+
					8 + 0, 8 + 1, 8 + 3, 8 + 3, 8 + 1, 8 + 2, // Z-
					12 + 0, 12 + 1, 12 + 3, 12 + 3, 12 + 1, 12 + 2, // Z+
					16 + 0, 16 + 1, 16 + 3, 16 + 3, 16 + 1, 16 + 2, // X-
					20 + 0, 20 + 1, 20 + 3, 20 + 3, 20 + 1, 20 + 2, // X+
			];
		}

		private void AddExtraDebugUI(float indentAmount) {
			ImGuiH.IndentedCollapsingHeader("Camera", indentAmount, DrawCamera);

			if (xyzGizmoRecorderNode != null) {
				bool showXyzGizmo = xyzGizmoRecorderNode.ShouldDraw;
				if (ImGui.Checkbox("Show XYZ Gizmo", ref showXyzGizmo)) { xyzGizmoRecorderNode.ShouldDraw = showXyzGizmo; }
			}

			ImGui.Text("test");

			return;

			void DrawCamera() => CameraImGuiMaker.ShowImGui(camera); // this should be faster than a lambda?
		}

		protected override void Setup() {
			base.Setup();

			xyzGizmoRecorderNode = new(LogicalGpu, TransferCommandPool, SwapChain, gameAssembly, MaxFramesInFlight, camera);
			AddNode(xyzGizmoRecorderNode);

			// TODO world node

			CreateCubeGraphicsPipeline(out DescriptorSetLayout cubeDescriptorSetLayout);
			CreateChunkGraphicsPipeline(out DescriptorSetLayout chunkDescriptorSetLayout);

			CreateBuffers();

			CreateSamplerAndTextures();

			CreateCubeDescriptorSets(cubeDescriptorSetLayout);
			CreateChunkDescriptorSets(chunkDescriptorSetLayout);
			UpdateDescriptorSets();

			depthImage = LogicalGpu.CreateDepthImage(TransferCommandPool, SwapChain.Extent);
		}

		private void CreateCubeGraphicsPipeline(out DescriptorSetLayout descriptorSetLayout) {
			const string Name = "Test";

			VulkanShader vertexShader = LogicalGpu.CreateShader($"{Name} Vertex Shader", Name, ShaderLanguage.Glsl, ShaderType.Vertex, gameAssembly);
			VulkanShader fragmentShader = LogicalGpu.CreateShader($"{Name} Fragment Shader", Name, ShaderLanguage.Glsl, ShaderType.Fragment, gameAssembly);

			descriptorSetLayout = LogicalGpu.CreateDescriptorSetLayout([
					new(VkDescriptorType.DescriptorTypeUniformBuffer, VkShaderStageFlagBits.ShaderStageVertexBit, 0), //
					new(VkDescriptorType.DescriptorTypeStorageBuffer, VkShaderStageFlagBits.ShaderStageVertexBit, 1), //
					new(VkDescriptorType.DescriptorTypeCombinedImageSampler, VkShaderStageFlagBits.ShaderStageFragmentBit, 2), //
			]);

			// ew
			cubeGraphicsPipeline = LogicalGpu.CreateGraphicsPipeline(
				new($"{Name} Graphics Pipeline", SwapChain.ImageFormat, [ vertexShader, fragmentShader, ], VertexXyzUvRgb.GetAttributeDescriptions(), VertexXyzUvRgb.GetBindingDescriptions()) {
						DescriptorSetLayouts = [ descriptorSetLayout.VkDescriptorSetLayout, ],
						FrontFace = VkFrontFace.FrontFaceClockwise, // TODO fix this
						EnableDepthTest = true,
						EnableDepthWrite = true,
				});

			Logger.Debug("Created cube graphics pipeline");

			LogicalGpu.EnqueueDestroy(vertexShader);
			LogicalGpu.EnqueueDestroy(fragmentShader);
		}

		private void CreateChunkGraphicsPipeline(out DescriptorSetLayout descriptorSetLayout) {
			const string Name = "Chunk";

			VulkanShader vertexShader = LogicalGpu.CreateShader($"{Name} Vertex Shader", Name, ShaderLanguage.Glsl, ShaderType.Vertex, gameAssembly);
			VulkanShader fragmentShader = LogicalGpu.CreateShader($"{Name} Fragment Shader", Name, ShaderLanguage.Glsl, ShaderType.Fragment, gameAssembly);

			descriptorSetLayout = LogicalGpu.CreateDescriptorSetLayout([
					new(VkDescriptorType.DescriptorTypeUniformBuffer, VkShaderStageFlagBits.ShaderStageVertexBit, 0), //
			]);

			chunkGraphicsPipeline = LogicalGpu.CreateGraphicsPipeline(
				new($"{Name} Graphics Pipeline", SwapChain.ImageFormat, [ vertexShader, fragmentShader, ], ChunkVertex.GetAttributeDescriptions(), ChunkVertex.GetBindingDescriptions()) {
						DescriptorSetLayouts = [ descriptorSetLayout.VkDescriptorSetLayout, ],
						FrontFace = VkFrontFace.FrontFaceClockwise, // TODO fix this

						EnableDepthTest = true,
						EnableDepthWrite = true,
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
			cameraUniformBuffer = LogicalGpu.CreateDescriptorBuffers("Camera Uniform Buffer", (ulong)sizeof(ProjectionView), MaxFramesInFlight, VkDescriptorType.DescriptorTypeUniformBuffer,
				VkBufferUsageFlagBits.BufferUsageUniformBufferBit);

			cubeInstanceBuffers = LogicalGpu.CreateDescriptorBuffers("Cube Instance Storage Buffers", cubeUniformBufferValue.Size, MaxFramesInFlight, VkDescriptorType.DescriptorTypeStorageBuffer,
				VkBufferUsageFlagBits.BufferUsageStorageBufferBit);

			Logger.Debug("Created uniform buffers");
		}

		private void CreateSamplerAndTextures() {
			textureSampler = LogicalGpu.CreateSampler(GraphicsBackend.Settings, new(VkFilter.FilterLinear, VkFilter.FilterLinear, Window.SelectedGpu.PhysicalDeviceProperties2.properties.limits));
			Logger.Debug("Created texture sampler");

			using (StbiImage stbiImage = AssetH.LoadImage("Test.64x64", "png", 4, gameAssembly)) {
				image = LogicalGpu.CreateImage("Test 64x64 Image", (uint)stbiImage.Width, (uint)stbiImage.Height, VkFormat.FormatR8g8b8a8Srgb);
				TransferCommandPool.CopyToImage(image, PhysicalGpu.QueueFamilyIndices, LogicalGpu.TransferQueue, stbiImage);
			}

			Logger.Debug("Created image");
		}

		private void CreateCubeDescriptorSets(DescriptorSetLayout descriptorSetLayout) {
			DescriptorPool descriptorPool = LogicalGpu.CreateDescriptorPool([ VkDescriptorType.DescriptorTypeUniformBuffer, VkDescriptorType.DescriptorTypeStorageBuffer, VkDescriptorType.DescriptorTypeCombinedImageSampler, ], 3,
				MaxFramesInFlight);

			cubeDescriptorSet = descriptorPool.AllocateDescriptorSet(descriptorSetLayout);
			Logger.Debug("Created cube descriptor sets");
		}

		private void CreateChunkDescriptorSets(DescriptorSetLayout descriptorSetLayout) {
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

			// nodes
			base.RecordCommandBuffer(graphicsCommandBuffer);
		}

		protected override void CopyBuffers(float delta) {
			if (cubeInstanceBuffers == null || cameraUniformBuffer == null || chunkIndexBuffer == null) { throw new NullReferenceException(); }

			cubeUniformBufferValue.Models[0] = Matrix4x4.CreateRotationY(float.Lerp(VoxelTest.PrevCubeRotation, VoxelTest.CubeRotation, delta) * float.DegreesToRadians(90f)) * Matrix4x4.CreateTranslation(cubePosition);

			Matrix4x4 proj = camera.Projection;

			cameraUniformBuffer.Copy(new ProjectionView(proj, camera.View), FrameIndex); // TODO lerp camera position & rotation
			cubeInstanceBuffers.Copy(MemoryMarshal.AsBytes(cubeUniformBufferValue.Models), FrameIndex);

			// nodes
			base.CopyBuffers(delta);

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