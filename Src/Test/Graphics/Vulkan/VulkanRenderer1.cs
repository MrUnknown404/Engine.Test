using System.Diagnostics;
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
using Engine3.Client.Graphics.Vulkan.Renderers;
using Engine3.Test.Core.Graphics;
using Engine3.Utility;
using ImGuiNET;
using NLog;
using OpenTK.Graphics.Vulkan;
using StbiSharp;

namespace Engine3.Test.Test.Graphics.Vulkan {
	public unsafe class VulkanRenderer1 : VulkanRendererBase {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private const uint CubeCount = 100;
		private const string TestShaderName = "Test";

		private GraphicsPipeline? graphicsPipeline;

		private DepthImage? depthImage;

		private DescriptorBuffers? cameraUniformBuffer;

		private VulkanBuffer? cubeVertexBuffer;
		private VulkanBuffer? cubeIndexBuffer;
		private DescriptorBuffers? cubeInstanceBuffers;
		private DescriptorSets? cubeDescriptorSet;

		private VulkanBuffer? quadVertexBuffer;
		private VulkanBuffer? quadIndexBuffer;
		private DescriptorBuffers? quadInstanceBuffers;
		private DescriptorSets? quadDescriptorSet;

		private VulkanImage? image;
		private TextureSampler? textureSampler;

		private readonly Camera camera;

		private readonly VertexXyzUvRgb[] quadVertices = [ new(-0.5f, -0.5f, 0, 0, 1, 1, 0, 0), new(0.5f, -0.5f, 0, 1, 1, 0, 1, 0), new(0.5f, 0.5f, 0, 1, 0, 0, 0, 1), new(-0.5f, 0.5f, 0, 0, 0, 1, 1, 1), ];
		private readonly uint[] quadIndices = [ 0, 1, 2, 2, 3, 0, ];

		private readonly VertexXyzUvRgb[] cubeVertices; // TODO fix these
		private readonly uint[] cubeIndices = [
				6, 2, 3, 3, 7, 6, // X-
				4, 0, 1, 1, 5, 4, // X+
				0, 1, 2, 2, 3, 0, // Y-
				4, 5, 6, 6, 7, 4, // Y+
				7, 3, 0, 0, 4, 7, // Z-
				5, 1, 2, 2, 6, 5, // Z+ (textured atm)
		];

		private readonly Vector3[] cubePositions = new Vector3[CubeCount];
		private readonly Vector3 quadPosition = new(-2, 0, -2);

		private readonly ModelsBuffer cubeUniformBufferValue = new(CubeCount);
		private readonly ModelsBuffer quadUniformBufferValue = new(1);

		private readonly Assembly gameAssembly;

		protected override DepthImage? DepthImage => depthImage;

		public VulkanRenderer1(GameClient game, VulkanGraphicsBackend graphicsBackend, VulkanWindow window, Camera camera, Assembly gameAssembly) : base(graphicsBackend, window) {
			this.camera = camera;
			this.gameAssembly = gameAssembly;

			ImGuiBackend = new(window, graphicsBackend.Settings.MaxFramesInFlight, new DemoWindowImGui()) { ShowDebugUI = true, DebugUIImGui = new DebugUIImGui(game, window) { AddExtraDebugUI = AddExtraDebugUI, }, };

			const float Size = 1;
			const float H = Size / 2;
			const float R = 1, G = 1, B = 1;
			const float U = 0, V = 0;

			const float X0 = -H, X1 = +H;
			const float Y0 = -H, Y1 = +H;
			const float Z0 = -H, Z1 = +H;

			cubeVertices = [
					new(X1, Y0, Z0, U, V, R, G, B), // 0
					new(X1, Y0, Z1, 1, 1, R, G, B), // 1
					new(X0, Y0, Z1, 0, 1, R, G, B), // 2
					new(X0, Y0, Z0, U, V, R, G, B), // 3
					new(X1, Y1, Z0, U, V, R, G, B), // 4
					new(X1, Y1, Z1, 1, 0, R, G, B), // 5
					new(X0, Y1, Z1, 0, 0, R, G, B), // 6
					new(X0, Y1, Z0, U, V, R, G, B), // 7
			];

			float aspectRatio = (float)SwapChain.Extent.width / SwapChain.Extent.height;
			Random random = new();

			for (int i = 0; i < CubeCount; i++) { cubePositions[i] = new((random.NextSingle() * 10 - 5) * aspectRatio, random.NextSingle() * 10 - 5, -10.5f + random.NextSingle()); }
		}

		private void AddExtraDebugUI(float indentAmount) {
			ImGuiH.IndentedCollapsingHeader("Camera", indentAmount, DrawFunc);

			ImGui.Text("test");

			return;

			void DrawFunc() => CameraImGuiMaker.ShowImGui(camera); // this should be faster than a lambda?
		}

		protected override void Setup() {
			base.Setup();

			CreateGraphicsPipeline(out DescriptorSetLayout descriptorSetLayout);

			CreateBuffers();

			CreateSamplerAndTextures();

			CreateDescriptorSets(descriptorSetLayout);
			UpdateDescriptorSets();

			depthImage = LogicalGpu.CreateDepthImage(TransferCommandPool, SwapChain.Extent);
		}

		private void CreateGraphicsPipeline(out DescriptorSetLayout descriptorSetLayout) {
			VulkanShader vertexShader = LogicalGpu.CreateShader($"{TestShaderName} Vertex Shader", TestShaderName, ShaderLanguage.Glsl, ShaderType.Vertex, gameAssembly);
			VulkanShader fragmentShader = LogicalGpu.CreateShader($"{TestShaderName} Fragment Shader", TestShaderName, ShaderLanguage.Glsl, ShaderType.Fragment, gameAssembly);

			descriptorSetLayout = LogicalGpu.CreateDescriptorSetLayout([
					new(VkDescriptorType.DescriptorTypeUniformBuffer, VkShaderStageFlagBits.ShaderStageVertexBit, 0), //
					new(VkDescriptorType.DescriptorTypeStorageBuffer, VkShaderStageFlagBits.ShaderStageVertexBit, 1), //
					new(VkDescriptorType.DescriptorTypeCombinedImageSampler, VkShaderStageFlagBits.ShaderStageFragmentBit, 2), //
			]);

			// ew
			graphicsPipeline = LogicalGpu.CreateGraphicsPipeline(
				new("Test Graphics Pipeline", SwapChain.ImageFormat, [ vertexShader, fragmentShader, ], VertexXyzUvRgb.GetAttributeDescriptions(), VertexXyzUvRgb.GetBindingDescriptions()) {
						DescriptorSetLayouts = [ descriptorSetLayout.VkDescriptorSetLayout, ],
						FrontFace = VkFrontFace.FrontFaceClockwise, // TODO oops. indices are backwards
						CullMode = VkCullModeFlagBits.CullModeNone,
						EnableDepthTest = true,
						EnableDepthWrite = true,
				});

			Logger.Debug("Created graphics pipeline");

			LogicalGpu.EnqueueDestroy(vertexShader);
			LogicalGpu.EnqueueDestroy(fragmentShader);
		}

		private void CreateBuffers() {
			cubeVertexBuffer = LogicalGpu.CreateBuffer("Cube Vertex Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageVertexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, (ulong)(sizeof(VertexXyzUvRgb) * cubeVertices.Length));

			quadVertexBuffer = LogicalGpu.CreateBuffer("Quad Vertex Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageVertexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, (ulong)(sizeof(VertexXyzUvRgb) * quadVertices.Length));

			cubeIndexBuffer = LogicalGpu.CreateBuffer("Cube Index Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit,
				(ulong)(sizeof(uint) * cubeIndices.Length));

			quadIndexBuffer = LogicalGpu.CreateBuffer("Quad Index Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit,
				(ulong)(sizeof(uint) * quadIndices.Length));

			TransferCommandPool.CopyToBuffers([
					TransferCommandPool.CopyDataToBufferInfo.Copy(cubeVertexBuffer, cubeVertices), TransferCommandPool.CopyDataToBufferInfo.Copy(quadVertexBuffer, quadVertices),
					TransferCommandPool.CopyDataToBufferInfo.Copy(cubeIndexBuffer, cubeIndices), TransferCommandPool.CopyDataToBufferInfo.Copy(quadIndexBuffer, quadIndices),
			]);

			Logger.Debug("Created & copied vertex/index buffers");

			cameraUniformBuffer = LogicalGpu.CreateDescriptorBuffers("Camera Uniform Buffer", (ulong)sizeof(ProjectionView), MaxFramesInFlight, VkDescriptorType.DescriptorTypeUniformBuffer,
				VkBufferUsageFlagBits.BufferUsageUniformBufferBit);

			cubeInstanceBuffers = LogicalGpu.CreateDescriptorBuffers("Cube Instance Storage Buffers", cubeUniformBufferValue.Size, MaxFramesInFlight, VkDescriptorType.DescriptorTypeStorageBuffer,
				VkBufferUsageFlagBits.BufferUsageStorageBufferBit);

			quadInstanceBuffers = LogicalGpu.CreateDescriptorBuffers("Quad Instance Storage Buffers", quadUniformBufferValue.Size, MaxFramesInFlight, VkDescriptorType.DescriptorTypeStorageBuffer,
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

		private void CreateDescriptorSets(DescriptorSetLayout descriptorSetLayout) {
			DescriptorPool descriptorPool = LogicalGpu.CreateDescriptorPool([ VkDescriptorType.DescriptorTypeUniformBuffer, VkDescriptorType.DescriptorTypeStorageBuffer, VkDescriptorType.DescriptorTypeCombinedImageSampler, ], 3,
				MaxFramesInFlight);

			cubeDescriptorSet = descriptorPool.AllocateDescriptorSet(descriptorSetLayout);
			quadDescriptorSet = descriptorPool.AllocateDescriptorSet(descriptorSetLayout);
			Logger.Debug("Created descriptor sets");
		}

		private void UpdateDescriptorSets() {
			if (cubeInstanceBuffers == null || quadInstanceBuffers == null || cameraUniformBuffer == null || image == null || textureSampler == null || cubeDescriptorSet == null || quadDescriptorSet == null) {
				throw new UnreachableException();
			}

			cubeDescriptorSet.UpdateDescriptorSet(0, cameraUniformBuffer);
			cubeDescriptorSet.UpdateDescriptorSet(1, cubeInstanceBuffers);
			cubeDescriptorSet.UpdateDescriptorSet(2, image.ImageView, textureSampler.Sampler);

			quadDescriptorSet.UpdateDescriptorSet(0, cameraUniformBuffer);
			quadDescriptorSet.UpdateDescriptorSet(1, quadInstanceBuffers);
			quadDescriptorSet.UpdateDescriptorSet(2, image.ImageView, textureSampler.Sampler);

			Logger.Debug("Updated descriptor sets");
		}

		protected override void RecordCommandBuffer(GraphicsCommandBuffer commandBuffer) {
			if (graphicsPipeline == null || cubeVertexBuffer == null || cubeIndexBuffer == null || quadVertexBuffer == null || quadIndexBuffer == null || cubeDescriptorSet == null || quadDescriptorSet == null) {
				throw new NullReferenceException();
			}

			commandBuffer.CmdBindGraphicsPipeline(graphicsPipeline.Pipeline); // TODO integrate graphics pipeline binding into VulkanRenderer?

			commandBuffer.CmdSetViewport(0, 0, SwapChain.Extent.width, SwapChain.Extent.height, 0, 1);
			commandBuffer.CmdSetScissor(0, 0, SwapChain.Extent);

			// Cube
			commandBuffer.CmdBindDescriptorSet(graphicsPipeline.Layout, cubeDescriptorSet.GetCurrent(FrameIndex), VkShaderStageFlagBits.ShaderStageVertexBit | VkShaderStageFlagBits.ShaderStageFragmentBit);
			commandBuffer.CmdBindVertexBuffer(cubeVertexBuffer, 0);
			commandBuffer.CmdBindIndexBuffer(cubeIndexBuffer, cubeIndexBuffer.BufferSize);
			commandBuffer.CmdDrawIndexed((uint)cubeIndices.Length, CubeCount, 0, 0, 0);

			// Quad
			commandBuffer.CmdBindDescriptorSet(graphicsPipeline.Layout, quadDescriptorSet.GetCurrent(FrameIndex), VkShaderStageFlagBits.ShaderStageVertexBit | VkShaderStageFlagBits.ShaderStageFragmentBit);
			commandBuffer.CmdBindVertexBuffer(quadVertexBuffer, 0);
			commandBuffer.CmdBindIndexBuffer(quadIndexBuffer, quadIndexBuffer.BufferSize);
			commandBuffer.CmdDrawIndexed((uint)quadIndices.Length);
		}

		protected override void CopyBuffers(float delta) {
			if (cubeInstanceBuffers == null || quadInstanceBuffers == null || cameraUniformBuffer == null) { throw new NullReferenceException(); }

			float cubeRotation = float.Lerp(VulkanTest.PrevCubeRotation, VulkanTest.CubeRotation, delta);
			Matrix4x4 rotationMatrix = Matrix4x4.CreateRotationY(cubeRotation * float.DegreesToRadians(90f));

			for (int i = 0; i < CubeCount; i++) { cubeUniformBufferValue.Models[i] = rotationMatrix * Matrix4x4.CreateTranslation(cubePositions[i]); }

			quadUniformBufferValue.Models[0] = Matrix4x4.CreateTranslation(quadPosition.X, quadPosition.Y + MathF.Sin(cubeRotation), quadPosition.Z);

			cameraUniformBuffer.Copy(new ProjectionView(camera.Projection, camera.View), FrameIndex);
			cubeInstanceBuffers.Copy(MemoryMarshal.AsBytes(cubeUniformBufferValue.Models), FrameIndex);
			quadInstanceBuffers.Copy(MemoryMarshal.AsBytes(quadUniformBufferValue.Models), FrameIndex);
		}

		protected override void Cleanup() {
			//

			base.Cleanup();
		}
	}
}