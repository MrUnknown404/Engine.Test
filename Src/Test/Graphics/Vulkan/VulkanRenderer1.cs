using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Engine3.Client;
using Engine3.Client.Graphics;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Test.Test.Graphics.Test;
using NLog;
using OpenTK.Graphics.Vulkan;
using USharpLibs.Common.Math;

namespace Engine3.Test.Test.Graphics.Vulkan {
	public unsafe class VulkanRenderer1 : VulkanRenderer {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private const uint CubeCount = 100;

		private const string TestShaderName = "Test";

		private GraphicsPipeline? graphicsPipeline;

		private DescriptorBuffers? cameraUniformBuffer;

		private VulkanBuffer? cubeVertexBuffer;
		private VulkanBuffer? cubeIndexBuffer;
		private DescriptorBuffers? cubeUniformBuffers;
		private DescriptorSets? cubeDescriptorSet;

		private VulkanBuffer? quadVertexBuffer;
		private VulkanBuffer? quadIndexBuffer;
		private DescriptorBuffers? quadUniformBuffers;
		private DescriptorSets? quadDescriptorSet;

		private VulkanImage? image;
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

		private readonly Vector3[] cubePositions = new Vector3[CubeCount];
		private readonly Vector3 quadPosition = new(-2, 0, -2);

		private readonly ObjectUniformBuffer cubeUniformBufferValue = new(CubeCount);
		private readonly ObjectUniformBuffer quadUniformBufferValue = new(1);

		private readonly Assembly gameAssembly;

		public VulkanRenderer1(VulkanGraphicsBackend graphicsBackend, VulkanWindow window, Assembly gameAssembly) : base(graphicsBackend, window) {
			this.gameAssembly = gameAssembly;

			// camera = new OrthographicCamera(10, 10, 0.5f, 10f) { Position = new(0, 1, 3), YawDegrees = 270, };
			float aspectRatio = (float)SwapChain.Extent.width / SwapChain.Extent.height;
			camera = new PerspectiveCamera(aspectRatio, 0.01f, 100f) { Position = new(0, 0, 2.5f), YawDegrees = 270, };

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

			Random random = new();
			for (int i = 0; i < CubeCount; i++) { cubePositions[i] = new((random.NextSingle() * 10 - 5) * aspectRatio, random.NextSingle() * 10 - 5, -10); }
		}

		public override void Setup() {
			CreateGraphicsPipeline(out DescriptorSetLayout descriptorSetLayout);

			CreateBuffers();

			CreateSamplerAndTextures();

			CreateDescriptorSets(descriptorSetLayout.VkDescriptorSetLayout);
			UpdateDescriptorSets();
		}

		private void CreateGraphicsPipeline(out DescriptorSetLayout descriptorSetLayout) {
			VulkanShader vertexShader = CreateShader($"{TestShaderName} Vertex Shader", TestShaderName, ShaderLanguage.Glsl, ShaderType.Vertex, gameAssembly);
			VulkanShader fragmentShader = CreateShader($"{TestShaderName} Fragment Shader", TestShaderName, ShaderLanguage.Glsl, ShaderType.Fragment, gameAssembly);

			descriptorSetLayout = CreateDescriptorSetLayout([
					new(VkDescriptorType.DescriptorTypeUniformBuffer, VkShaderStageFlagBits.ShaderStageVertexBit, 0), //
					new(VkDescriptorType.DescriptorTypeStorageBuffer, VkShaderStageFlagBits.ShaderStageVertexBit, 1), //
					new(VkDescriptorType.DescriptorTypeCombinedImageSampler, VkShaderStageFlagBits.ShaderStageFragmentBit, 2), // do i need to increase i?
			]);

			// ew
			graphicsPipeline = CreateGraphicsPipeline(new("Test Graphics Pipeline", SwapChain.ImageFormat, [ vertexShader, fragmentShader, ], TestVertex2.GetAttributeDescriptions(), TestVertex2.GetBindingDescriptions()) {
					DescriptorSetLayouts = [ descriptorSetLayout.VkDescriptorSetLayout, ],
					// FrontFace = VkFrontFace.FrontFaceCounterClockwise, // TODO oops. indices are backwards
					CullMode = VkCullModeFlagBits.CullModeNone,
			});

			Logger.Debug("Created graphics pipeline");

			DestroyResource(vertexShader);
			DestroyResource(fragmentShader);
		}

		private void CreateBuffers() {
			cubeVertexBuffer = CreateBuffer("Cube Vertex Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageVertexBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit,
				(ulong)(sizeof(TestVertex2) * cubeVertices.Length));

			quadVertexBuffer = CreateBuffer("Quad Vertex Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageVertexBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit,
				(ulong)(sizeof(TestVertex2) * quadVertices.Length));

			cubeIndexBuffer = CreateBuffer("Cube Index Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit,
				(ulong)(sizeof(uint) * cubeIndices.Length));

			quadIndexBuffer = CreateBuffer("Quad Index Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit,
				(ulong)(sizeof(uint) * quadIndices.Length));

			CopyToBuffers([ CopyToInfo.Of(cubeVertices, cubeVertexBuffer), CopyToInfo.Of(quadVertices, quadVertexBuffer), CopyToInfo.Of(cubeIndices, cubeIndexBuffer), CopyToInfo.Of(quadIndices, quadIndexBuffer), ]);
			Logger.Debug("Created & copied vertex/index buffers");

			cameraUniformBuffer = CreateDescriptorBuffers("Camera Uniform Buffer", (ulong)sizeof(CameraUniformBuffer), VkDescriptorType.DescriptorTypeUniformBuffer, VkBufferUsageFlagBits.BufferUsageUniformBufferBit);
			cubeUniformBuffers = CreateDescriptorBuffers("Cube Instance Storage Buffers", cubeUniformBufferValue.Size, VkDescriptorType.DescriptorTypeStorageBuffer, VkBufferUsageFlagBits.BufferUsageStorageBufferBit);
			quadUniformBuffers = CreateDescriptorBuffers("Quad Instance Storage Buffers", quadUniformBufferValue.Size, VkDescriptorType.DescriptorTypeStorageBuffer, VkBufferUsageFlagBits.BufferUsageStorageBufferBit);
			Logger.Debug("Created uniform buffers");
		}

		private void CreateSamplerAndTextures() {
			textureSampler = CreateSampler(new(VkFilter.FilterLinear, VkFilter.FilterLinear, Window.SelectedGpu.PhysicalDeviceProperties2.properties.limits));
			Logger.Debug("Created texture sampler");

			image = CreateImageAndCopyUsingStaging("Test 64x64 Image", "Test.64x64", "png", 4, VkFormat.FormatR8g8b8a8Srgb, gameAssembly);
			Logger.Debug("Created image");
		}

		private void CreateDescriptorSets(VkDescriptorSetLayout descriptorSetLayout) {
			DescriptorPool descriptorPool = CreateDescriptorPool([ VkDescriptorType.DescriptorTypeUniformBuffer, VkDescriptorType.DescriptorTypeStorageBuffer, VkDescriptorType.DescriptorTypeCombinedImageSampler, ],
				2u * MaxFramesInFlight);

			cubeDescriptorSet = descriptorPool.AllocateDescriptorSet(descriptorSetLayout);
			quadDescriptorSet = descriptorPool.AllocateDescriptorSet(descriptorSetLayout);
			Logger.Debug("Created descriptor sets");
		}

		private void UpdateDescriptorSets() {
			if (cubeUniformBuffers == null || quadUniformBuffers == null || cameraUniformBuffer == null || image == null || textureSampler == null || cubeDescriptorSet == null || quadDescriptorSet == null) {
				throw new UnreachableException();
			}

			cubeDescriptorSet.UpdateDescriptorSet(0, cameraUniformBuffer);
			quadDescriptorSet.UpdateDescriptorSet(0, cameraUniformBuffer);
			cubeDescriptorSet.UpdateDescriptorSet(1, cubeUniformBuffers);
			quadDescriptorSet.UpdateDescriptorSet(1, quadUniformBuffers);
			cubeDescriptorSet.UpdateDescriptorSet(2, image.ImageView, textureSampler.Sampler);
			quadDescriptorSet.UpdateDescriptorSet(2, image.ImageView, textureSampler.Sampler);

			Logger.Debug("Updated descriptor sets");
		}

		protected override void RecordCommandBuffer(GraphicsCommandBuffer graphicsCommandBuffer, float delta) {
			if (graphicsPipeline == null || cubeVertexBuffer == null || cubeIndexBuffer == null || quadVertexBuffer == null || quadIndexBuffer == null || cubeDescriptorSet == null || quadDescriptorSet == null) {
				throw new NullReferenceException();
			}

			graphicsCommandBuffer.CmdBindGraphicsPipeline(graphicsPipeline.Pipeline); // TODO integrate graphics pipeline binding into VulkanRenderer?

			graphicsCommandBuffer.CmdSetViewport(0, 0, SwapChain.Extent.width, SwapChain.Extent.height, 0, 1);
			graphicsCommandBuffer.CmdSetScissor(SwapChain.Extent, new(0, 0));

			// Cube
			graphicsCommandBuffer.CmdBindDescriptorSet(graphicsPipeline.Layout, cubeDescriptorSet.GetCurrent(FrameIndex), VkShaderStageFlagBits.ShaderStageVertexBit | VkShaderStageFlagBits.ShaderStageFragmentBit);
			graphicsCommandBuffer.CmdBindVertexBuffer(cubeVertexBuffer, 0);
			graphicsCommandBuffer.CmdBindIndexBuffer(cubeIndexBuffer, cubeIndexBuffer.BufferSize);
			graphicsCommandBuffer.CmdDrawIndexed((uint)cubeIndices.Length, CubeCount, 0, 0, 0);

			// Quad
			graphicsCommandBuffer.CmdBindDescriptorSet(graphicsPipeline.Layout, quadDescriptorSet.GetCurrent(FrameIndex), VkShaderStageFlagBits.ShaderStageVertexBit | VkShaderStageFlagBits.ShaderStageFragmentBit);
			graphicsCommandBuffer.CmdBindVertexBuffer(quadVertexBuffer, 0);
			graphicsCommandBuffer.CmdBindIndexBuffer(quadIndexBuffer, quadIndexBuffer.BufferSize);
			graphicsCommandBuffer.CmdDrawIndexed((uint)quadIndices.Length);
		}

		protected override void CopyUniformBuffers(float delta) {
			if (cubeUniformBuffers == null || quadUniformBuffers == null || cameraUniformBuffer == null) { throw new NullReferenceException(); }

			// camera.YawDegrees += 0.05f;

			// TODO i think because the projection & view matrix are the same they should have their own shared uniform buffer (push constants? edit: probably still use a uniform buffer). then a second uniform buffer for model transformations

			float cubeRotation = float.Lerp(VulkanTest.PrevCubeRotation, VulkanTest.CubeRotation, delta);
			Matrix4x4 rotationMatrix = Matrix4x4.CreateRotationY(cubeRotation * MathH.ToRadians(90f));

			for (int i = 0; i < CubeCount; i++) { cubeUniformBufferValue.Models[i] = rotationMatrix * Matrix4x4.CreateTranslation(cubePositions[i]); }

			quadUniformBufferValue.Models[0] = Matrix4x4.CreateTranslation(quadPosition.X, quadPosition.Y + MathF.Sin(cubeRotation), quadPosition.Z);

			cameraUniformBuffer.Copy(new CameraUniformBuffer(camera.CreateProjectionMatrix(), camera.CreateViewMatrix()));
			cubeUniformBuffers.Copy(MemoryMarshal.AsBytes(cubeUniformBufferValue.Models));
			quadUniformBuffers.Copy(MemoryMarshal.AsBytes(quadUniformBufferValue.Models));
		}

		protected override void Cleanup() {
			//

			base.Cleanup();
		}
	}
}