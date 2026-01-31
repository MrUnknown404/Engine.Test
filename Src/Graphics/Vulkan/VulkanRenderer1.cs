using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using Engine3.Client;
using Engine3.Client.Graphics;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Test.Graphics.Test;
using NLog;
using OpenTK.Graphics.Vulkan;
using USharpLibs.Common.Math;
using VkBuffer = Engine3.Client.Graphics.Vulkan.VkBuffer;
using VkImage = Engine3.Client.Graphics.Vulkan.VkImage;

namespace Engine3.Test.Graphics.Vulkan {
	public unsafe class VulkanRenderer1 : VulkanRenderer {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private const string TestShaderName = "Test";

		private GraphicsPipeline? graphicsPipeline;

		private VkBuffer? cubeVertexBuffer;
		private VkBuffer? cubeIndexBuffer;
		private UniformBuffers? cubeUniformBuffers;
		private DescriptorSets? cubeDescriptorSet;

		private VkBuffer? quadVertexBuffer;
		private VkBuffer? quadIndexBuffer;
		private UniformBuffers? quadUniformBuffers;
		private DescriptorSets? quadDescriptorSet;

		private VkImage? image;
		private TextureSampler? textureSampler;

		private readonly Camera camera;

		private readonly TestVertex2[] quadVertices = [ new(-0.5f, -0.5f, 0, 0, 1, 1, 0, 0), new(0.5f, -0.5f, 0, 1, 1, 0, 1, 0), new(0.5f, 0.5f, 0, 1, 0, 0, 0, 1), new(-0.5f, 0.5f, 0, 0, 0, 1, 1, 1), ];
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

		public VulkanRenderer1(VulkanGraphicsBackend graphicsBackend, VulkanWindow window, Assembly gameAssembly) : base(graphicsBackend, window) {
			this.gameAssembly = gameAssembly;

			// camera = new OrthographicCamera(10, 10, 0.5f, 10f) { Position = new(0, 1, 3), YawDegrees = 270, };
			camera = new PerspectiveCamera((float)SwapChain.Extent.width / SwapChain.Extent.height, 0.01f, 10f) { Position = new(0, 0, 2.5f), YawDegrees = 270, };

			cubeUniformBufferObject.Projection = camera.CreateProjectionMatrix();
			quadUniformBufferObject.Projection = camera.CreateProjectionMatrix();

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
			CreateGraphicsPipeline(out VkDescriptorSetLayout descriptorSetLayout);

			CreateBuffers();

			CreateSamplerAndTextures();

			CreateDescriptorSets(descriptorSetLayout);
			UpdateDescriptorSets();
		}

		private void CreateGraphicsPipeline(out VkDescriptorSetLayout descriptorSetLayout) {
			VkShader vertexShader = LogicalGpu.CreateShader($"{TestShaderName} Vertex Shader", TestShaderName, ShaderLanguage.Glsl, ShaderType.Vertex, gameAssembly);
			VkShader fragmentShader = LogicalGpu.CreateShader($"{TestShaderName} Fragment Shader", TestShaderName, ShaderLanguage.Glsl, ShaderType.Fragment, gameAssembly);

			descriptorSetLayout = LogicalGpu.CreateDescriptorSetLayout([
					new(VkDescriptorType.DescriptorTypeUniformBuffer, VkShaderStageFlagBits.ShaderStageVertexBit, 0), new(VkDescriptorType.DescriptorTypeCombinedImageSampler, VkShaderStageFlagBits.ShaderStageFragmentBit, 1),
			]);

			// ew
			graphicsPipeline = CreateGraphicsPipeline(new("Test Graphics Pipeline", SwapChain.ImageFormat, [ vertexShader, fragmentShader, ], TestVertex2.GetAttributeDescriptions(), TestVertex2.GetBindingDescriptions()) {
					DescriptorSetLayouts = [ descriptorSetLayout, ],
					// FrontFace = VkFrontFace.FrontFaceCounterClockwise, // TODO oops. indices are backwards
					CullMode = VkCullModeFlagBits.CullModeNone,
			});

			Logger.Debug("Created graphics pipeline");

			vertexShader.Destroy();
			fragmentShader.Destroy();
		}

		private void CreateBuffers() {
			cubeVertexBuffer = LogicalGpu.CreateBuffer("Cube Vertex Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageVertexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, (ulong)(sizeof(TestVertex2) * cubeVertices.Length));

			quadVertexBuffer = LogicalGpu.CreateBuffer("Quad Vertex Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageVertexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, (ulong)(sizeof(TestVertex2) * quadVertices.Length));

			cubeIndexBuffer = LogicalGpu.CreateBuffer("Cube Index Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit,
				(ulong)(sizeof(uint) * cubeIndices.Length));

			quadIndexBuffer = LogicalGpu.CreateBuffer("Quad Index Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit,
				(ulong)(sizeof(uint) * quadIndices.Length));

			cubeVertexBuffer.CopyUsingStaging(TransferCommandPool, LogicalGpu.TransferQueue, cubeVertices);
			quadVertexBuffer.CopyUsingStaging(TransferCommandPool, LogicalGpu.TransferQueue, quadVertices);
			cubeIndexBuffer.CopyUsingStaging(TransferCommandPool, LogicalGpu.TransferQueue, cubeIndices);
			quadIndexBuffer.CopyUsingStaging(TransferCommandPool, LogicalGpu.TransferQueue, quadIndices);
			Logger.Debug("Created vertex/index buffers");

			ulong bufferSize = TestUniformBufferObject.Size;
			cubeUniformBuffers = LogicalGpu.CreateUniformBuffers("Cube Uniform Buffers", this, bufferSize);
			quadUniformBuffers = LogicalGpu.CreateUniformBuffers("Quad Uniform Buffers", this, bufferSize);
			Logger.Debug("Created uniform buffers");
		}

		private void CreateSamplerAndTextures() {
			textureSampler = LogicalGpu.CreateSampler(new(VkFilter.FilterLinear, VkFilter.FilterLinear, Window.SelectedGpu.PhysicalDeviceProperties2.properties.limits));
			Logger.Debug("Created texture sampler");

			image = LogicalGpu.CreateImageAndCopyUsingStaging("Test 64x64 Image", "Test.64x64", "png", 64, 64, 4, VkFormat.FormatR8g8b8a8Srgb, TransferCommandPool, gameAssembly);
			Logger.Debug("Created image");
		}

		private void CreateDescriptorSets(VkDescriptorSetLayout descriptorSetLayout) {
			DescriptorPool descriptorPool = CreateDescriptorPool([ VkDescriptorType.DescriptorTypeUniformBuffer, VkDescriptorType.DescriptorTypeCombinedImageSampler, ], 2u * MaxFramesInFlight);
			cubeDescriptorSet = descriptorPool.AllocateDescriptorSet(descriptorSetLayout);
			quadDescriptorSet = descriptorPool.AllocateDescriptorSet(descriptorSetLayout);
			Logger.Debug("Created descriptor sets");
		}

		private void UpdateDescriptorSets() {
			if (cubeUniformBuffers == null || quadUniformBuffers == null || image == null || textureSampler == null || cubeDescriptorSet == null || quadDescriptorSet == null) { throw new UnreachableException(); }

			cubeDescriptorSet.UpdateDescriptorSet(0, cubeUniformBuffers, cubeUniformBuffers.BufferSize);
			quadDescriptorSet.UpdateDescriptorSet(0, quadUniformBuffers, quadUniformBuffers.BufferSize);
			cubeDescriptorSet.UpdateDescriptorSet(1, image.ImageView, textureSampler.Sampler);
			quadDescriptorSet.UpdateDescriptorSet(1, image.ImageView, textureSampler.Sampler);

			Logger.Debug("Updated descriptor sets");
		}

		protected override void RecordCommandBuffer(GraphicsCommandBuffer graphicsCommandBuffer, float delta) {
			if (graphicsPipeline == null || cubeVertexBuffer == null || cubeIndexBuffer == null || quadVertexBuffer == null || quadIndexBuffer == null || cubeDescriptorSet == null || quadDescriptorSet == null) {
				throw new NullReferenceException();
			}

			graphicsCommandBuffer.CmdBindGraphicsPipeline(graphicsPipeline.Pipeline); // TODO automate graphics pipeline binding

			graphicsCommandBuffer.CmdSetViewport(0, 0, SwapChain.Extent.width, SwapChain.Extent.height, 0, 1);
			graphicsCommandBuffer.CmdSetScissor(SwapChain.Extent, new(0, 0));

			// Cube
			graphicsCommandBuffer.CmdBindDescriptorSet(graphicsPipeline.Layout, cubeDescriptorSet.GetCurrent(FrameIndex), VkShaderStageFlagBits.ShaderStageVertexBit | VkShaderStageFlagBits.ShaderStageFragmentBit);
			graphicsCommandBuffer.CmdBindVertexBuffer(cubeVertexBuffer, 0);
			graphicsCommandBuffer.CmdBindIndexBuffer(cubeIndexBuffer, cubeIndexBuffer.BufferSize);
			graphicsCommandBuffer.CmdDrawIndexed((uint)cubeIndices.Length);

			// Quad
			graphicsCommandBuffer.CmdBindDescriptorSet(graphicsPipeline.Layout, quadDescriptorSet.GetCurrent(FrameIndex), VkShaderStageFlagBits.ShaderStageVertexBit | VkShaderStageFlagBits.ShaderStageFragmentBit);
			graphicsCommandBuffer.CmdBindVertexBuffer(quadVertexBuffer, 0);
			graphicsCommandBuffer.CmdBindIndexBuffer(quadIndexBuffer, quadIndexBuffer.BufferSize);
			graphicsCommandBuffer.CmdDrawIndexed((uint)quadIndices.Length);
		}

		protected override void CopyUniformBuffers(float delta) {
			if (cubeUniformBuffers == null || quadUniformBuffers == null) { throw new UnreachableException(); }

			// camera.YawDegrees += 0.05f;

			// TODO i think because the projection & view matrix are the same they should have their own shared uniform buffer (push constants?). then a second uniform buffer for model transformations

			float f = FrameCount / 5000f;

			cubeUniformBufferObject.View = camera.CreateViewMatrix();
			cubeUniformBufferObject.Model = Matrix4x4.CreateRotationY(f * MathH.ToRadians(90f)) * Matrix4x4.CreateTranslation(cubePosition); // TODO currently affected by frame rate

			quadUniformBufferObject.View = camera.CreateViewMatrix();
			quadUniformBufferObject.Model = Matrix4x4.CreateTranslation(quadPosition.X, quadPosition.Y + MathF.Sin(f), quadPosition.Z);

			cubeUniformBuffers.Copy(cubeUniformBufferObject.CollectBytes());
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
		}
	}
}