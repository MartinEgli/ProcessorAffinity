// -----------------------------------------------------------------------
// <copyright file="ProcessorAffinity.cs" company="Anori Soft">
// Copyright (c) Anori Soft. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace ProcessorAffinity
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Security.Permissions;

    /// <summary>
    ///     Gets and sets the processor affinity of the current thread.
    /// </summary>
    public static class ProcessorAffinity
    {
        /// <summary>
        ///     Masks from ids.
        /// </summary>
        /// <param name="ids">The ids.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">CPUId</exception>
        private static ulong MaskFromIds(IEnumerable<int> ids)
        {
            ulong mask = 0;
            foreach (var id in ids)
            {
                if (id < 0 || id >= Environment.ProcessorCount)
                {
                    throw new ArgumentOutOfRangeException("CPUId", id.ToString());
                }

                mask |= 1UL << id;
            }

            return mask;
        }

        /// <summary>
        ///     Ids from mask.
        /// </summary>
        /// <param name="mask">The mask.</param>
        /// <returns></returns>
        private static IEnumerable<int> IdsFromMask(ulong mask)
        {
            var ids = new List<int>();
            var i = 0;
            while (mask > 0UL)
            {
                if ((mask & 1UL) != 0)
                {
                    ids.Add(i);
                }

                mask >>= 1;
                i++;
            }

            return ids;
        }

        /// <summary>
        ///     Sets a processor affinity mask for the current thread.
        /// </summary>
        /// <param name="mask">
        ///     A thread affinity mask where each bit set to 1 specifies a logical processor on which this thread is allowed to
        ///     run.
        ///     <remarks>Note: a thread cannot specify a broader set of CPUs than those specified in the process affinity mask.</remarks>
        /// </param>
        /// <returns>
        ///     The previous affinity mask for the current thread.
        /// </returns>
        /// <exception cref="Win32Exception"></exception>
        public static UIntPtr SetThreadAffinityMask(UIntPtr mask)
        {
            return SetThreadAffinityMask(Win32Native.GetCurrentThread(), mask);
        }

        /// <summary>
        ///     Sets the thread affinity mask.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="mask">The mask.</param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        public static UIntPtr SetThreadAffinityMask(SafeThreadHandle handle, UIntPtr mask)
        {
            var threadAffinityMask = Win32Native.SetThreadAffinityMask(handle, mask);
            if (threadAffinityMask == UIntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return threadAffinityMask;
        }

        /// <summary>
        ///     Gets the processor affinity mask.
        /// </summary>
        /// <param name="process">The process.</param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        public static IntPtr GetProcessAffinityMask(Process process)
        {
            var handle = new SafeProcessHandle(process.Handle, false);
            if (!Win32Native.GetProcessAffinityMask(handle, out var processAffinity, out var systemAffinity))
            {
                throw new Win32Exception();
            }

            return processAffinity;
        }

        /// <summary>
        ///     Gets the system affinity mask.
        /// </summary>
        /// <param name="process">The process.</param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        public static IntPtr GetSystemAffinityMask(Process process)
        {
            var handle = new SafeProcessHandle(process.Handle, false);
            if (!Win32Native.GetProcessAffinityMask(handle, out var processAffinity, out var systemAffinity))
            {
                throw new Win32Exception();
            }

            return systemAffinity;
        }

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static void SetProcessorAffinityMask(Process process, IntPtr mask)
        {
            var handle = new SafeProcessHandle(process.Handle, false);
            if (!Win32Native.SetProcessAffinityMask(handle, mask))
            {
                throw new Win32Exception();
            }
        }

        /// <summary>
        ///     Gets the current processor number.
        /// </summary>
        /// <returns></returns>
        public static uint GetCurrentProcessorNumber()
        {
            return Win32Native.NtGetCurrentProcessorNumber();
        }

        /// <summary>
        ///     Gets the current thread.
        /// </summary>
        /// <returns></returns>
        public static SafeThreadHandle GetCurrentThread()
        {
            return Win32Native.GetCurrentThread();
        }

        /// <summary>
        ///     Sets the logical CPUs that the current thread is allowed to execute on.
        /// </summary>
        /// <param name="cpuIds">
        ///     One or more logical processor identifier(s) the current thread is allowed to run on.
        ///     <remarks>Note: numbering starts from 0.</remarks>
        /// </param>
        /// <returns>
        ///     The previous affinity mask for the current thread.
        /// </returns>
        public static int[] SetThreadAffinity(params int[] cpuIds)
        {
            return IdsFromMask((ulong)SetThreadAffinityMask((UIntPtr)MaskFromIds(cpuIds))).ToArray();
        }

        /// <summary>
        ///     Sets the thread affinity.
        /// </summary>
        /// <param name="cpuIds">The cpu ids.</param>
        /// <returns></returns>
        public static IEnumerable<int> SetThreadAffinity(IEnumerable<int> cpuIds)
        {
            return IdsFromMask((ulong)SetThreadAffinityMask((UIntPtr)MaskFromIds(cpuIds)));
        }

        /// <summary>
        ///     Restrict a code block to run on the specified logical CPUs in conjuction with
        ///     the <code>using</code> statement.
        /// </summary>
        /// <param name="cpuIds">
        ///     One or more logical processor identifier(s) the current thread is allowed to run on.
        ///     <remarks>Note: numbering starts from 0.</remarks>
        /// </param>
        /// <returns>
        ///     A helper structure that will reset the affinity when its Dispose() method is called at the end of the using
        ///     block.
        /// </returns>
        /// <exception cref="ArgumentNullException">cpuIds</exception>
        /// <exception cref="ArgumentException">Value cannot be an empty collection. - cpuIds</exception>
        public static ProcessorAffinityHelper BeginAffinity(params int[] cpuIds)
        {
            if (cpuIds == null)
            {
                throw new ArgumentNullException(nameof(cpuIds));
            }

            if (cpuIds.Length == 0)
            {
                throw new ArgumentException("Value cannot be an empty collection.", nameof(cpuIds));
            }

            var a = SetThreadAffinityMask(((UIntPtr)MaskFromIds(cpuIds)));
            var b = IdsFromMask((ulong)a);
            var c = GetProcessAffinityMask(Process.GetCurrentProcess());
            var d = IdsFromMask((ulong)c);
            var l = new List<int> { 0, 1 };
            SetProcessAffinity(l);
            var p = Process.GetCurrentProcess();
            var e = GetProcessAffinity(p);
            var f = SetProcessToSystemAffinity(p);
            return new ProcessorAffinityHelper(a);
        }

        /// <summary>
        ///     Gets the processor affinity.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <returns></returns>
        public static IEnumerable<int> GetProcessAffinity(Process p) => IdsFromMask((ulong)GetProcessAffinityMask(p));

        /// <summary>
        ///     Gets the process affinity.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<int> GetProcessAffinity() => GetProcessAffinity(Process.GetCurrentProcess());

        /// <summary>
        ///     Gets the system affinity.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <returns></returns>
        public static IEnumerable<int> GetSystemAffinity(Process p) => IdsFromMask((ulong)GetSystemAffinityMask(p));

        /// <summary>
        ///     Gets the system affinity.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<int> GetSystemAffinity() => GetSystemAffinity(Process.GetCurrentProcess());

        /// <summary>
        ///     Sets the process to system affinity.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <returns></returns>
        public static IEnumerable<int> SetProcessToSystemAffinity(Process p)
        {
            var cpuIds = GetSystemAffinity(Process.GetCurrentProcess());
            SetProcessAffinity(p, cpuIds);
            cpuIds = GetProcessAffinity(p);
            return cpuIds;
        }

        /// <summary>
        ///     Sets the process to system affinity.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<int> SetProcessToSystemAffinity()
        {
            return SetProcessToSystemAffinity(Process.GetCurrentProcess());
        }

        /// <summary>
        ///     Sets the processor affinity.
        /// </summary>
        /// <param name="cpuIds">The cpu ids.</param>
        public static void SetProcessAffinity(IEnumerable<int> cpuIds)
        {
            SetProcessAffinity(Process.GetCurrentProcess(), cpuIds);
        }

        /// <summary>
        ///     Sets the processor affinity.
        /// </summary>
        /// <param name="process">The process.</param>
        /// <param name="cpuIds">The cpu ids.</param>
        public static void SetProcessAffinity(Process process, IEnumerable<int> cpuIds)
        {
            SetProcessorAffinityMask(process, (IntPtr)MaskFromIds(cpuIds));
        }

        /// <summary>
        ///     Sets the processor affinity.
        /// </summary>
        /// <param name="cpuIds">The cpu ids.</param>
        public static void SetProcessAffinity(params int[] cpuIds)
        {
            SetProcessAffinity(Process.GetCurrentProcess(), cpuIds);
        }

        /// <summary>
        ///     Sets the processor affinity.
        /// </summary>
        /// <param name="process">The process.</param>
        /// <param name="cpuIds">The cpu ids.</param>
        public static void SetProcessAffinity(Process process, params int[] cpuIds)
        {
            SetProcessorAffinityMask(process, (IntPtr)MaskFromIds(cpuIds));
        }

        /// <summary>
        ///     Win32Native Class
        /// </summary>
        private static class Win32Native
        {
            /// <summary>
            ///     The kernel32
            /// </summary>
            private const string Kernel32 = "kernel32.dll";

            /// <summary>
            ///     The NTDLL
            /// </summary>
            private const string Ntdll = "ntdll.dll";

            /// <summary>
            ///     The psapi
            /// </summary>
            private const string Psapi = "psapi.dll";

            /// <summary>
            ///     Enums the processes.
            /// </summary>
            /// <param name="processIds">The process ids.</param>
            /// <param name="size">The size.</param>
            /// <param name="needed">The needed.</param>
            /// <returns></returns>
            [DllImport(Psapi, CharSet = CharSet.Auto, SetLastError = true)]
            [ResourceExposure(ResourceScope.Machine)]
            internal static extern bool EnumProcesses(int[] processIds, int size, out int needed);

            /// <summary>
            ///     Opens the process.
            /// </summary>
            /// <param name="access">The access.</param>
            /// <param name="inherit">if set to <c>true</c> [inherit].</param>
            /// <param name="processId">The process identifier.</param>
            /// <returns></returns>
            [DllImport(Kernel32, CharSet = CharSet.Auto, SetLastError = true)]
            [ResourceExposure(ResourceScope.Machine)]
            internal static extern SafeProcessHandle OpenProcess(int access, bool inherit, int processId);

            /// <summary>
            ///     Sets the thread affinity mask.
            /// </summary>
            /// <param name="handle">The handle.</param>
            /// <param name="mask">The mask.</param>
            /// <returns></returns>
            [DllImport(Kernel32, CharSet = CharSet.Auto, SetLastError = true)]
            [ResourceExposure(ResourceScope.Process)]
            internal static extern IntPtr SetThreadAffinityMask(SafeThreadHandle handle, HandleRef mask);

            /// <summary>
            ///     Get current processor number.
            /// </summary>
            /// <returns></returns>
            [DllImport(Ntdll, CharSet = CharSet.Auto)]
            internal static extern uint NtGetCurrentProcessorNumber();

            /// <summary>
            ///     Gets the current thread. GetCurrentThread() returns only a pseudo handle. No need for a SafeHandle here.
            /// </summary>
            /// <returns></returns>
            [DllImport(Kernel32)]
            internal static extern SafeThreadHandle GetCurrentThread();

            /// <summary>
            ///     Sets the thread affinity mask.
            /// </summary>
            /// <param name="handle">The handle.</param>
            /// <param name="mask">The mask.</param>
            /// <returns></returns>
            [HostProtection(SelfAffectingThreading = true)]
            [DllImport(Kernel32, CharSet = CharSet.Auto, SetLastError = true)]
            internal static extern UIntPtr SetThreadAffinityMask(SafeThreadHandle handle, UIntPtr mask);

            /// <summary>
            ///     Sets the process affinity mask.
            /// </summary>
            /// <param name="handle">The handle.</param>
            /// <param name="mask">The mask.</param>
            /// <returns></returns>
            [DllImport(Kernel32, CharSet = CharSet.Auto, SetLastError = true)]
            [ResourceExposure(ResourceScope.Machine)]
            internal static extern bool SetProcessAffinityMask(SafeProcessHandle handle, IntPtr mask);

            /// <summary>
            ///     Gets the process affinity mask.
            /// </summary>
            /// <param name="handle">The handle.</param>
            /// <param name="processMask">The process mask.</param>
            /// <param name="systemMask">The system mask.</param>
            /// <returns></returns>
            [DllImport(Kernel32, CharSet = CharSet.Auto, SetLastError = true)]
            [ResourceExposure(ResourceScope.None)]
            internal static extern bool GetProcessAffinityMask(
                SafeProcessHandle handle,
                out IntPtr processMask,
                out IntPtr systemMask);
        }

        /// <summary>
        ///     ProcessorAffinityHelper Class
        /// </summary>
        /// <seealso cref="System.IDisposable" />
        public struct ProcessorAffinityHelper : IDisposable
        {
            /// <summary>
            ///     The lastAffinity
            /// </summary>
            private UIntPtr lastAffinity;

            /// <summary>
            ///     Initializes a new instance of the <see cref="ProcessorAffinityHelper" /> struct.
            /// </summary>
            /// <param name="lastAffinity">The lastAffinity.</param>
            internal ProcessorAffinityHelper(UIntPtr lastAffinity)
            {
                this.lastAffinity = lastAffinity;
            }

            #region IDisposable Members

            /// <summary>
            ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                if (this.lastAffinity == UIntPtr.Zero)
                {
                    return;
                }

                Win32Native.SetThreadAffinityMask(Win32Native.GetCurrentThread(), this.lastAffinity);
                this.lastAffinity = UIntPtr.Zero;
            }

            #endregion IDisposable Members
        }
    }
}