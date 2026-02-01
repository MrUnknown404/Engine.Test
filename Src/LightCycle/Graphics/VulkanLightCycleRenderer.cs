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

namespace Engine3.Test.LightCycle.Graphics {
	public unsafe class VulkanLightCycleRenderer : VulkanRenderer {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private const string ShaderName = "Cycle";

		private readonly Assembly assembly = typeof(VulkanLightCycleRenderer).Assembly;

		private readonly GameManager gameManager;
		private readonly Camera camera;

		private GraphicsPipeline? graphicsPipeline;

		private VulkanBuffer? cubeVertexBuffer;
		private VulkanBuffer? cubeIndexBuffer;
		private UniformBuffers? cubeUniformBuffers;
		private DescriptorSets? cubeDescriptorSet;

		private readonly TestVertex[] cubeVertices;
		private readonly uint[] cubeIndices = [
				6, 2, 3, 3, 7, 6, // X-
				4, 0, 1, 1, 5, 4, // X+
				0, 1, 2, 2, 3, 0, // Y-
				4, 5, 6, 6, 7, 4, // Y+
				7, 3, 0, 0, 4, 7, // Z-
				5, 1, 2, 2, 6, 5, // Z+ (textured atm)
		];

		private readonly TestUniformBufferObject cubeUniformBufferObject = new();

		public VulkanLightCycleRenderer(VulkanGraphicsBackend graphicsBackend, VulkanWindow window, GameManager gameManager) : base(graphicsBackend, window) {
			this.gameManager = gameManager;

			camera = new PerspectiveCamera((float)SwapChain.Extent.width / SwapChain.Extent.height, 0.01f, 10f) { Position = new(0, 5, 0), YawDegrees = 0, PitchDegrees = -90, };

			cubeUniformBufferObject.Projection = camera.CreateProjectionMatrix();

			const float Size = 0.1f;
			const float H = Size / 2;
			const float R = 1, G = 1, B = 1;

			const float X0 = -H, X1 = +H;
			const float Y0 = -H, Y1 = +H;
			const float Z0 = -H, Z1 = +H;

			cubeVertices = [
					new(X1, Y0, Z0, R, G, B), // 0
					new(X1, Y0, Z1, R, G, B), // 1
					new(X0, Y0, Z1, R, G, B), // 2
					new(X0, Y0, Z0, R, G, B), // 3
					new(X1, Y1, Z0, R, G, B), // 4
					new(X1, Y1, Z1, R, G, B), // 5
					new(X0, Y1, Z1, R, G, B), // 6
					new(X0, Y1, Z0, R, G, B), // 7
			];
		}

		public override void Setup() {
			CreateGraphicsPipeline(out VkDescriptorSetLayout descriptorSetLayout);

			CreateBuffers();

			CreateDescriptorSets(descriptorSetLayout);
			UpdateDescriptorSets();
		}

		private void CreateGraphicsPipeline(out VkDescriptorSetLayout descriptorSetLayout) {
			VulkanShader vertexShader = LogicalGpu.CreateShader($"{ShaderName} Vertex Shader", ShaderName, ShaderLanguage.Glsl, ShaderType.Vertex, assembly);
			VulkanShader fragmentShader = LogicalGpu.CreateShader($"{ShaderName} Fragment Shader", ShaderName, ShaderLanguage.Glsl, ShaderType.Fragment, assembly);

			descriptorSetLayout = LogicalGpu.CreateDescriptorSetLayout([ new(VkDescriptorType.DescriptorTypeUniformBuffer, VkShaderStageFlagBits.ShaderStageVertexBit, 0), ]);

			// ew
			graphicsPipeline = CreateGraphicsPipeline(new("Test Graphics Pipeline", SwapChain.ImageFormat, [ vertexShader, fragmentShader, ], TestVertex.GetAttributeDescriptions(), TestVertex.GetBindingDescriptions()) {
					DescriptorSetLayouts = [ descriptorSetLayout, ],
					// FrontFace = VkFrontFace.FrontFaceCounterClockwise, // TODO oops. indices are backwards
					CullMode = VkCullModeFlagBits.CullModeNone,
			});

			Logger.Debug("Created graphics pipeline");

			vertexShader.Destroy();
			fragmentShader.Destroy();
		}

		private void CreateBuffers() {
			cubeVertexBuffer = CreateBuffer("Cube Vertex Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageVertexBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit,
				(ulong)(sizeof(TestVertex) * cubeVertices.Length));

			cubeIndexBuffer = CreateBuffer("Cube Index Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit,
				(ulong)(sizeof(uint) * cubeIndices.Length));

			// TODO can i use a single staging buffer for all of these?
			cubeVertexBuffer.CopyUsingStaging(TransferCommandPool, LogicalGpu.TransferQueue, cubeVertices);
			cubeIndexBuffer.CopyUsingStaging(TransferCommandPool, LogicalGpu.TransferQueue, cubeIndices);
			Logger.Debug("Created vertex/index buffers");

			ulong bufferSize = TestUniformBufferObject.Size;
			cubeUniformBuffers = CreateUniformBuffers("Cube Uniform Buffers", bufferSize);
			Logger.Debug("Created uniform buffers");
		}

		private void CreateDescriptorSets(VkDescriptorSetLayout descriptorSetLayout) {
			DescriptorPool descriptorPool = CreateDescriptorPool([ VkDescriptorType.DescriptorTypeUniformBuffer, ], 1u * MaxFramesInFlight);
			cubeDescriptorSet = descriptorPool.AllocateDescriptorSet(descriptorSetLayout);
			Logger.Debug("Created descriptor sets");
		}

		private void UpdateDescriptorSets() {
			if (cubeUniformBuffers == null || cubeDescriptorSet == null) { throw new UnreachableException(); }

			cubeDescriptorSet.UpdateDescriptorSet(0, cubeUniformBuffers);

			Logger.Debug("Updated descriptor sets");
		}

		protected override void RecordCommandBuffer(GraphicsCommandBuffer graphicsCommandBuffer, float delta) {
			if (graphicsPipeline == null || cubeVertexBuffer == null || cubeIndexBuffer == null || cubeDescriptorSet == null) { throw new NullReferenceException(); }

			if (gameManager.Map is not { } map) {
				Logger.Warn("No map found. Nothing to render. Do something about that"); // TODO impl rendering when we have no map
				return;
			}

			graphicsCommandBuffer.CmdBindGraphicsPipeline(graphicsPipeline.Pipeline); // TODO integrate graphics pipeline binding into VulkanRenderer?

			graphicsCommandBuffer.CmdSetViewport(0, 0, SwapChain.Extent.width, SwapChain.Extent.height, 0, 1);
			graphicsCommandBuffer.CmdSetScissor(SwapChain.Extent, new(0, 0));

			// Cube
			graphicsCommandBuffer.CmdBindDescriptorSet(graphicsPipeline.Layout, cubeDescriptorSet.GetCurrent(FrameIndex), VkShaderStageFlagBits.ShaderStageVertexBit);
			graphicsCommandBuffer.CmdBindVertexBuffer(cubeVertexBuffer, 0);
			graphicsCommandBuffer.CmdBindIndexBuffer(cubeIndexBuffer, cubeIndexBuffer.BufferSize);
			graphicsCommandBuffer.CmdDrawIndexed((uint)cubeIndices.Length);
		}

		protected override void CopyUniformBuffers(float delta) {
			if (cubeUniformBuffers == null) { throw new UnreachableException(); }

			if (gameManager.Map is not { } map) {
				Logger.Warn("No map found. Nothing to render. Do something about that"); // TODO impl rendering when we have no map
				return;
			}

			// camera.PitchDegrees += 0.005f;

			// TODO i think because the projection & view matrix are the same they should have their own shared uniform buffer (push constants?). then a second uniform buffer for model transformations

			float f = FrameCount / 5000f; // TODO currently affected by frame rate

			Cycle.Cycle cycle = map.Cycles.First();

			cubeUniformBufferObject.View = camera.CreateViewMatrix();
			cubeUniformBufferObject.Model = Matrix4x4.CreateRotationY(f * MathH.ToRadians(90f)) * cycle.Transform.CreateMatrix();

			cubeUniformBuffers.Copy(cubeUniformBufferObject.CollectBytes());
		}
	}
}