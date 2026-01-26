using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using Engine3.Graphics;
using Engine3.Graphics.Vulkan;
using Engine3.Graphics.Vulkan.Objects;
using Engine3.Test.Graphics.Test;
using NLog;
using OpenTK.Graphics.Vulkan;
using USharpLibs.Common.Math;

namespace Engine3.Test.Graphics.Vulkan {
	public unsafe class VkRenderer1 : VkRenderer {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private const string TestShaderName = "Test";

		private GraphicsPipeline? graphicsPipeline;

		private VkBufferObject? vertexBuffer;
		private VkBufferObject? indexBuffer;
		private UniformBuffers? uniformBuffers;

		private VkImageObject? image;
		private TextureSampler? textureSampler;

		private readonly Camera camera;

		private readonly TestVertex2[] vertices = [ new(-0.5f, -0.5f, 0, 1, 0, 1, 0, 0), new(0.5f, -0.5f, 0, 0, 0, 0, 1, 0), new(0.5f, 0.5f, 0, 0, 1, 0, 0, 1), new(-0.5f, 0.5f, 0, 1, 1, 1, 1, 1), ];
		private readonly uint[] indices = [ 0, 1, 2, 2, 3, 0, ];
		private readonly TestUniformBufferObject testUniformBufferObject = new();
		private readonly Assembly gameAssembly;

		public VkRenderer1(GameClient gameClient, VkWindow window, Assembly gameAssembly) : base(gameClient, window) {
			this.gameAssembly = gameAssembly;

			// camera = new OrthographicCamera(10, 10, 0.5f, 10f) { Position = new(0, 1, 3), YawDegrees = 270, };
			camera = new PerspectiveCamera((float)SwapChain.Extent.width / SwapChain.Extent.height, 0.01f, 10f) { Position = new(0, 0, 2.5f), YawDegrees = 270, };
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
			graphicsPipeline = new(LogicalDevice,
				new("Test Graphics Pipeline", SwapChain.ImageFormat, [ vertexShader, fragmentShader, ], TestVertex2.GetAttributeDescriptions(), TestVertex2.GetBindingDescriptions(),
					[ new(VkDescriptorType.DescriptorTypeUniformBuffer, VkShaderStageFlagBits.ShaderStageVertexBit, 0), new(VkDescriptorType.DescriptorTypeCombinedImageSampler, VkShaderStageFlagBits.ShaderStageFragmentBit, 1), ],
					MaxFramesInFlight) { CullMode = VkCullModeFlagBits.CullModeNone, });

			Logger.Debug("Created graphics pipeline");

			vertexShader.Destroy();
			fragmentShader.Destroy();
		}

		private void CreateBuffers(ulong uniformBufferSize) {
			vertexBuffer = new("Test Vertex Buffer", (ulong)(sizeof(TestVertex2) * vertices.Length), PhysicalDeviceMemoryProperties, LogicalDevice,
				VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageVertexBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit);

			indexBuffer = new("Test Index Buffer", (ulong)(sizeof(uint) * indices.Length), PhysicalDeviceMemoryProperties, LogicalDevice,
				VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit);

			vertexBuffer.CopyUsingStaging(TransferCommandPool, LogicalGpu.TransferQueue, vertices);
			indexBuffer.CopyUsingStaging(TransferCommandPool, LogicalGpu.TransferQueue, indices);
			Logger.Debug("Created vertex/index buffers");

			uniformBuffers = new("Test Uniform Buffers", this, PhysicalDeviceMemoryProperties, LogicalDevice, uniformBufferSize);
			Logger.Debug("Created uniform buffers");
		}

		private void CreateSamplerAndTextures() {
			textureSampler = new(LogicalDevice, new("Test Texture Sampler", VkFilter.FilterLinear, VkFilter.FilterLinear, Window.SelectedGpu.PhysicalDeviceProperties2.properties.limits));
			Logger.Debug("Created texture sampler");

			image = VkImageObject.CreateFromRgbaPng("Test Image", PhysicalDeviceMemoryProperties, LogicalDevice, TransferCommandPool, LogicalGpu.TransferQueue, PhysicalGpu.QueueFamilyIndices, "Test.64x64", gameAssembly);
			Logger.Debug("Created image");
		}

		private void UpdateDescriptorSets(ulong bufferSize) {
			if (graphicsPipeline == null || uniformBuffers == null || image == null || textureSampler == null) { throw new UnreachableException(); }

			graphicsPipeline.UpdateDescriptorSet(0, uniformBuffers, bufferSize);
			graphicsPipeline.UpdateDescriptorSet(1, image.ImageView, textureSampler.Sampler);
			Logger.Debug("Updated descriptor sets");
		}

		protected override void RecordCommandBuffer(GraphicsCommandBufferObject graphicsCommandBuffer, float delta) {
			if (graphicsPipeline == null || vertexBuffer == null || indexBuffer == null) { throw new NullReferenceException(); }

			graphicsCommandBuffer.CmdBindGraphicsPipeline(graphicsPipeline.Pipeline);

			graphicsCommandBuffer.CmdSetViewport(0, 0, SwapChain.Extent.width, SwapChain.Extent.height, 0, 1);
			graphicsCommandBuffer.CmdSetScissor(SwapChain.Extent, new(0, 0));

			graphicsCommandBuffer.CmdBindVertexBuffer(vertexBuffer, 0);
			graphicsCommandBuffer.CmdBindIndexBuffer(indexBuffer, indexBuffer.BufferSize);

			graphicsCommandBuffer.CmdBindDescriptorSets(graphicsPipeline.Layout, graphicsPipeline.GetDescriptorSet(CurrentFrame), VkShaderStageFlagBits.ShaderStageVertexBit);

			graphicsCommandBuffer.CmdDrawIndexed((uint)indices.Length);
		}

		protected override void CopyUniformBuffer(float delta) {
			if (uniformBuffers == null) { throw new UnreachableException(); }

			// camera.YawDegrees += 0.05f;

			testUniformBufferObject.Projection = camera.CreateProjectionMatrix();
			testUniformBufferObject.View = camera.CreateViewMatrix();
			testUniformBufferObject.Model = Matrix4x4.CreateRotationY(FrameCount / 1000f * MathH.ToRadians(90f)) * Matrix4x4.CreateTranslation(0, 0, MathF.Sin(FrameCount / 1000f) * 2); // TODO currently affected by frame rate

			uniformBuffers.Copy(testUniformBufferObject.CollectBytes());
		}

		protected override void Cleanup() {
			vertexBuffer?.Destroy();
			indexBuffer?.Destroy();
			uniformBuffers?.Destroy();

			textureSampler?.Destroy();
			image?.Destroy();

			graphicsPipeline?.Destroy();
		}
	}
}