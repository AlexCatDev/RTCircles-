using Silk.NET.OpenGLES;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Easy2D;
using System.Runtime.CompilerServices;

public static class GL
{
    public static Silk.NET.OpenGLES.GL Instance { get; private set; }

    public static string GLVendor { get; private set; }
    public static string GLRenderer { get; private set; }
    public static string GLShadingVersion { get; private set; }
    public static string GLVersion { get; private set; }
    public static string GLExtensions { get; private set; }

    public static ulong DrawCalls { get; private set; }

    public static int MaxTextureSlots { get; private set; } = -1;

    public unsafe static void SetGL(Silk.NET.OpenGLES.GL gl)
    {
        Instance = gl;

        GLVendor = Marshal.PtrToStringAnsi(new IntPtr(Instance.GetString(StringName.Vendor)));
        GLRenderer = Marshal.PtrToStringAnsi(new IntPtr(Instance.GetString(StringName.Renderer)));
        GLShadingVersion = Marshal.PtrToStringAnsi(new IntPtr(Instance.GetString(StringName.ShadingLanguageVersion)));
        GLVersion = Marshal.PtrToStringAnsi(new IntPtr(Instance.GetString(StringName.Version)));
        GLExtensions = Marshal.PtrToStringAnsi(new IntPtr(Instance.GetString(StringName.Extensions)));

        Utils.Log($"Vendor: {GLVendor}", LogLevel.Important);
        Utils.Log($"Renderer: {GLRenderer}", LogLevel.Important);
        Utils.Log($"Version: {GLVersion}", LogLevel.Important);
        Utils.Log($"GLSL Version: {GLShadingVersion}", LogLevel.Important);

        Instance.GetInteger(GetPName.MaxTextureImageUnits, out int slotCount);
        MaxTextureSlots = slotCount;

#if DEBUG
        Instance.Enable(GLEnum.DebugOutput);
        Instance.Enable(GLEnum.DebugOutputSynchronous);
        Instance.DebugMessageCallback(OnDebug, null);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void DrawElements(PrimitiveType mode, uint count, DrawElementsType elementsType, void* indices = null)
    {
        Instance.DrawElements(mode, count, elementsType, indices);
        DrawCalls++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void DrawArrays(PrimitiveType mode, int first, uint count)
    {
        Instance.DrawArrays(mode, first, count);
        DrawCalls++;
    }

    public static void ResetStatistics()
    {
        DrawCalls = 0;
    }

    private static void OnDebug(GLEnum source, GLEnum type, int id, GLEnum severity, int length, nint message, nint userparam)
    {
        LogLevel level = (DebugSeverity)severity == DebugSeverity.DebugSeverityNotification ? LogLevel.Debug : LogLevel.Warning;
        Utils.Log
        (
            $"[{severity.ToString().Substring(13)}] {type.ToString().Substring(9)}/{id}: {System.Runtime.InteropServices.Marshal.PtrToStringAnsi(message)}"
        , level);
    }
}
