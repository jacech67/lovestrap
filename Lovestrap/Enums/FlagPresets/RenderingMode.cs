namespace Lovestrap.Enums.FlagPresets
{
    public enum RenderingMode
    {
        [EnumName(StaticName = "Automatic")]
        Default,
        [EnumName(StaticName = "Vulkan")]
        Vulkan,
        [EnumName(StaticName = "Direct3D 11")]
        D3D11,
        [EnumName(StaticName = "OpenGL")]
        OpenGL
    }
}
