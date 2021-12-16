using Silk.NET.OpenGLES;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Easy2D;

public static class GL
{
    public static Silk.NET.OpenGLES.GL Instance { get; private set; }
    public static unsafe string GLVendor => Marshal.PtrToStringAnsi(new IntPtr(Instance.GetString(StringName.Vendor)));
    public static unsafe string GLRenderer => Marshal.PtrToStringAnsi(new IntPtr(Instance.GetString(StringName.Renderer)));
    public static unsafe string GLShadingVersion => Marshal.PtrToStringAnsi(new IntPtr(Instance.GetString(StringName.ShadingLanguageVersion)));
    public static unsafe string GLVersion => Marshal.PtrToStringAnsi(new IntPtr(Instance.GetString(StringName.Version)));
    public static unsafe string GLExtensions => Marshal.PtrToStringAnsi(new IntPtr(Instance.GetString(StringName.Extensions)));

    public static void SetGL(Silk.NET.OpenGLES.GL gl)
    {
        Instance = gl;

        Utils.Log($"Vendor: {GLVendor}", LogLevel.Important);
        Utils.Log($"Renderer: {GLRenderer}", LogLevel.Important);
        Utils.Log($"Version: {GLVersion}", LogLevel.Important);
        Utils.Log($"GLSL Version: {GLShadingVersion}", LogLevel.Important);

#if DEBUG
        unsafe
        {
            Instance.Enable(GLEnum.DebugOutput);
            Instance.Enable(GLEnum.DebugOutputSynchronous);
            Instance.DebugMessageCallback(OnDebug, null);
        }
#endif
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
