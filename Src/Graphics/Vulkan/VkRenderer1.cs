using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using Engine3.Client;
using Engine3.Client.Graphics;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Test.Graphics.Test;
using NLog;
using OpenTK.Graphics.Vulkan;
using USharpLibs.Common.Math;

namespace Engine3.Test.Graphics.Vulkan {
	public unsafe class VkRenderer1 : VkRenderer {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private const string TestShaderName = "Test";

		private GraphicsPipeline? graphicsPipeline;

		private VkBufferObject? cubeVertexBuffer;
		private VkBufferObject? cubeIndexBuffer;
		private UniformBuffers? cubeUniformBuffers;

		private VkBufferObject? quadVertexBuffer;
		private VkBufferObject? quadIndexBuffer;
		private UniformBuffers? quadUniformBuffers;

		private VkImageObject? image;
		private TextureSampler? textureSampler;

		private readonly Camera camera;

		private readonly TestVertex2[] quadVertices = [ new(-0.5f - 2, -0.5f, 0, 0, 1, 1, 0, 0), new(0.5f - 2, -0.5f, 0, 1, 1, 0, 1, 0), new(0.5f - 2, 0.5f, 0, 1, 0, 0, 0, 1), new(-0.5f - 2, 0.5f, 0, 0, 0, 1, 1, 1), ];
		private readonly uint[] quadIndices = [ 0, 1, 2, 2, 3, 0, ];

		private readonly TestVertex2[] cubeVertices;
		private readonly uint[] cubeIndices = [
				6, 2, 3, 3, 7, 6, // X-
				4, 0, 1, 1, 5, 4, // X+
				0, 1, 2, 2, 3, 0, // Y-
				4, 5, 6, 6, 7, 4, // Y+
				7, 3, 0, 0, 4, 7, // Z-
				5, 1, 2, 2, 6, 5, // Z+ (textured atm)
		];

		private readonly TestUniformBufferObject cubeUniformBufferObject = new();
		private readonly TestUniformBufferObject quadUniformBufferObject = new();
		private readonly Vector3 cubePosition = new(0, 0, -2);
		private readonly Vector3 quadPosition = new(-2, 0, -2);

		private readonly Assembly gameAssembly;

		public VkRenderer1(VulkanGraphicsBackend graphicsBackend, VkWindow window, Assembly gameAssembly) : base(graphicsBackend, window) {
			this.gameAssembly = gameAssembly;

			// camera = new OrthographicCamera(10, 10, 0.5f, 10f) { Position = new(0, 1, 3), YawDegrees = 270, };
			camera = new PerspectiveCamera((float)SwapChain.Extent.width / SwapChain.Extent.height, 0.01f, 10f) { Position = new(0, 0, 2.5f), YawDegrees = 270, };

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
		}

		public override void Setup() {
			CreateGraphicsPipeline();

			uint uniformBufferSize = TestUniformBufferObject.Size;
			CreateBuffers(uniformBufferSize);

			CreateSamplerAndTextures();

			UpdateDescriptorSets(uniformBufferSize);
		}

		private void CreateGraphicsPipeline() {
			VkShaderObject vertexShader = new("Test Vertex Shader", LogicalDevice, TestShaderName, ShaderLanguage.Glsl, ShaderType.Vertex, gameAssembly);
			VkShaderObject fragmentShader = new("Test Fragment Shader", LogicalDevice, TestShaderName, ShaderLanguage.Glsl, ShaderType.Fragment, gameAssembly);

			// ew
			graphicsPipeline = new(PhysicalDevice, LogicalDevice, new("Test Graphics Pipeline", SwapChain.ImageFormat, [ vertexShader, fragmentShader, ], TestVertex2.GetAttributeDescriptions(),
				TestVertex2.GetBindingDescriptions(),
				[ new(VkDescriptorType.DescriptorTypeUniformBuffer, VkShaderStageFlagBits.ShaderStageVertexBit, 0), new(VkDescriptorType.DescriptorTypeCombinedImageSampler, VkShaderStageFlagBits.ShaderStageFragmentBit, 1), ],
				MaxFramesInFlight) {
					// FrontFace = VkFrontFace.FrontFaceCounterClockwise, // TODO oops. indices are backwards
					CullMode = VkCullModeFlagBits.CullModeNone,
			});

			Logger.Debug("Created graphics pipeline");

			vertexShader.Destroy();
			fragmentShader.Destroy();
		}

		private void CreateBuffers(ulong uniformBufferSize) {
			if (graphicsPipeline == null) { throw new UnreachableException(); }

			cubeVertexBuffer = new("Cube Vertex Buffer", (ulong)(sizeof(TestVertex2) * cubeVertices.Length), PhysicalDeviceMemoryProperties, LogicalDevice,
				VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageVertexBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit);

			quadVertexBuffer = new("Quad Vertex Buffer", (ulong)(sizeof(TestVertex2) * quadVertices.Length), PhysicalDeviceMemoryProperties, LogicalDevice,
				VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageVertexBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit);

			cubeIndexBuffer = new("Cube Index Buffer", (ulong)(sizeof(uint) * cubeIndices.Length), PhysicalDeviceMemoryProperties, LogicalDevice,
				VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit);

			quadIndexBuffer = new("Quad Index Buffer", (ulong)(sizeof(uint) * quadIndices.Length), PhysicalDeviceMemoryProperties, LogicalDevice,
				VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit);

			cubeVertexBuffer.CopyUsingStaging(TransferCommandPool, LogicalGpu.TransferQueue, cubeVertices);
			quadVertexBuffer.CopyUsingStaging(TransferCommandPool, LogicalGpu.TransferQueue, quadVertices);
			cubeIndexBuffer.CopyUsingStaging(TransferCommandPool, LogicalGpu.TransferQueue, cubeIndices);
			quadIndexBuffer.CopyUsingStaging(TransferCommandPool, LogicalGpu.TransferQueue, quadIndices);
			Logger.Debug("Created vertex/index buffers");

			cubeUniformBuffers = new("Cube Uniform Buffers", this, PhysicalDeviceMemoryProperties, LogicalDevice, uniformBufferSize);
			quadUniformBuffers = new("Quad Uniform Buffers", this, PhysicalDeviceMemoryProperties, LogicalDevice, uniformBufferSize);
			Logger.Debug("Created uniform buffers");
		}

		private void CreateSamplerAndTextures() {
			textureSampler = new(LogicalDevice, new("Test Texture Sampler", VkFilter.FilterLinear, VkFilter.FilterLinear, Window.SelectedGpu.PhysicalDeviceProperties2.properties.limits));
			Logger.Debug("Created texture sampler");

			image = VkImageObject.CreateFromRgbaPng("Test Image", PhysicalDeviceMemoryProperties, LogicalDevice, TransferCommandPool, LogicalGpu.TransferQueue, PhysicalGpu.QueueFamilyIndices, "Test.64x64", gameAssembly);
			Logger.Debug("Created image");
		}

		private void UpdateDescriptorSets(ulong bufferSize) {
			if (graphicsPipeline == null || cubeUniformBuffers == null || quadUniformBuffers == null || image == null || textureSampler == null) { throw new UnreachableException(); }

			graphicsPipeline.UpdateDescriptorSet(0, cubeUniformBuffers, bufferSize);
			graphicsPipeline.UpdateDescriptorSet(1, image.ImageView, textureSampler.Sampler);
			Logger.Debug("Updated descriptor sets");
		}

		protected override void RecordCommandBuffer(GraphicsCommandBufferObject graphicsCommandBuffer, float delta) {
			if (graphicsPipeline == null || cubeVertexBuffer == null || cubeIndexBuffer == null || quadVertexBuffer == null || quadIndexBuffer == null || cubeUniformBuffers == null || quadUniformBuffers == null) {
				throw new NullReferenceException();
			}

			graphicsCommandBuffer.CmdBindGraphicsPipeline(graphicsPipeline.Pipeline);

			graphicsCommandBuffer.CmdSetViewport(0, 0, SwapChain.Extent.width, SwapChain.Extent.height, 0, 1);
			graphicsCommandBuffer.CmdSetScissor(SwapChain.Extent, new(0, 0));

			graphicsCommandBuffer.CmdBindDescriptorSet(graphicsPipeline.Layout, graphicsPipeline.GetDescriptorSet(CurrentFrame), VkShaderStageFlagBits.ShaderStageVertexBit);

			// Cube
			graphicsCommandBuffer.CmdBindVertexBuffer(cubeVertexBuffer, 0);
			graphicsCommandBuffer.CmdBindIndexBuffer(cubeIndexBuffer, cubeIndexBuffer.BufferSize);
			graphicsCommandBuffer.CmdDrawIndexed((uint)cubeIndices.Length);

			// Quad
			graphicsCommandBuffer.CmdBindVertexBuffer(quadVertexBuffer, 0);
			graphicsCommandBuffer.CmdBindIndexBuffer(quadIndexBuffer, quadIndexBuffer.BufferSize);
			graphicsCommandBuffer.CmdDrawIndexed((uint)quadIndices.Length);
		}

		protected override void CopyUniformBuffer(float delta) {
			if (cubeUniformBuffers == null || quadUniformBuffers == null) { throw new UnreachableException(); }

			// camera.YawDegrees += 0.05f;

			cubeUniformBufferObject.Projection = camera.CreateProjectionMatrix();
			cubeUniformBufferObject.View = camera.CreateViewMatrix();
			cubeUniformBufferObject.Model = Matrix4x4.CreateRotationY(FrameCount / 5000f * MathH.ToRadians(90f)) * Matrix4x4.CreateTranslation(cubePosition); // TODO currently affected by frame rate

			cubeUniformBuffers.Copy(cubeUniformBufferObject.CollectBytes());

			quadUniformBufferObject.Projection = camera.CreateProjectionMatrix();
			quadUniformBufferObject.View = camera.CreateViewMatrix();
			quadUniformBufferObject.Model = Matrix4x4.CreateTranslation(quadPosition);

			quadUniformBuffers.Copy(quadUniformBufferObject.CollectBytes());
		}

		protected override void Cleanup() {
			cubeVertexBuffer?.Destroy();
			cubeIndexBuffer?.Destroy();
			cubeUniformBuffers?.Destroy();

			quadVertexBuffer?.Destroy();
			quadIndexBuffer?.Destroy();
			quadUniformBuffers?.Destroy();

			textureSampler?.Destroy();
			image?.Destroy();

			graphicsPipeline?.Destroy();
		}
	}
}