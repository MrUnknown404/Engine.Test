using Engine3.Client.Graphics.Vulkan;
using OpenTK.Graphics.Vulkan;
using OpenTK.Platform;

namespace Engine3.Test.Voxel.Graphics;

public class VoxelGraphicsBackend : VulkanBackend {
	public VoxelGraphicsBackend(VulkanGraphicsApiHints graphicsApiHints) : base(graphicsApiHints) { }

	protected override bool IsPhysicalDeviceSuitable(VkPhysicalDeviceProperties physicalDeviceProperties, VkPhysicalDeviceFeatures physicalDeviceFeatures) =>
			base.IsPhysicalDeviceSuitable(physicalDeviceProperties, physicalDeviceFeatures) && physicalDeviceFeatures.multiDrawIndirect == VkH.True;
}