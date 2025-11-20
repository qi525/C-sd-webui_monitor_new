using System;
using System.Runtime.InteropServices;

namespace WebUIMonitor
{
    /// <summary>
    /// NVIDIA NVML 显存查询（NVIDIA/AMD GPU）
    /// </summary>
    public static class NvmlHelper
    {
        private const string NvmlDll = "nvml.dll";
        private static bool _initialized;
        private static bool _available;

        [DllImport(NvmlDll, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint nvmlInit();

        [DllImport(NvmlDll, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint nvmlDeviceGetCount(out uint count);

        [DllImport(NvmlDll, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint nvmlDeviceGetHandleByIndex(uint index, out IntPtr device);

        [DllImport(NvmlDll, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint nvmlDeviceGetName(IntPtr device, IntPtr name, uint length);

        [DllImport(NvmlDll, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint nvmlDeviceGetMemoryInfo(IntPtr device, out NvmlMemory mem);

        [StructLayout(LayoutKind.Sequential)]
        private struct NvmlMemory { public ulong total; public ulong free; public ulong used; }

        private static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            try { _available = (nvmlInit() == 0); }
            catch { }
        }

        public static (string name, double usedGB, double totalGB, bool success) GetGpuMemory()
        {
            Init();
            if (!_available) return ("N/A", 0, 0, false);

            try
            {
                if (nvmlDeviceGetCount(out uint count) != 0 || count == 0) return ("N/A", 0, 0, false);
                if (nvmlDeviceGetHandleByIndex(0, out IntPtr device) != 0) return ("N/A", 0, 0, false);

                IntPtr namePtr = Marshal.AllocHGlobal(64);
                string name = "Unknown";
                try
                {
                    if (nvmlDeviceGetName(device, namePtr, 64) == 0)
                        name = Marshal.PtrToStringAnsi(namePtr) ?? "Unknown";
                }
                finally { Marshal.FreeHGlobal(namePtr); }

                if (nvmlDeviceGetMemoryInfo(device, out NvmlMemory mem) != 0) return (name, 0, 0, false);

                const long GB = 1024L * 1024L * 1024L;
                return (name, mem.used / (double)GB, mem.total / (double)GB, true);
            }
            catch { return ("N/A", 0, 0, false); }
        }
    }
}
