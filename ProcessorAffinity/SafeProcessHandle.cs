// -----------------------------------------------------------------------
// <copyright file="SafeProcessHandle.cs" company="Anori Soft">
// Copyright (c) Anori Soft. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace ProcessorAffinity
{
    using System;
    using System.Diagnostics;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Security.Permissions;

    using Microsoft.Win32.SafeHandles;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid" />
    public sealed class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// The invalid handle
        /// </summary>
        internal static SafeProcessHandle InvalidHandle = new SafeProcessHandle(IntPtr.Zero);

        // Note that OpenProcess returns 0 on failure

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeProcessHandle"/> class.
        /// </summary>
        internal SafeProcessHandle()
            : base(true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeProcessHandle"/> class.
        /// </summary>
        /// <param name="handle">The handle.</param>
        internal SafeProcessHandle(IntPtr handle)
            : base(true)
        {
            this.SetHandle(handle);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeProcessHandle"/> class.
        /// </summary>
        /// <param name="existingHandle">The existing handle.</param>
        /// <param name="ownsHandle">if set to <c>true</c> [owns handle].</param>
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        public SafeProcessHandle(IntPtr existingHandle, bool ownsHandle)
            : base(ownsHandle)
        {
            this.SetHandle(existingHandle);
        }

        /// <summary>
        /// Initials the set handle.
        /// </summary>
        /// <param name="h">The h.</param>
        internal void InitialSetHandle(IntPtr h)
        {
            Debug.Assert(this.IsInvalid, "Safe handle should only be set once");
            this.handle = h;
        }

        /// <summary>
        /// When overridden in a derived class, executes the code required to free the handle.
        /// </summary>
        /// <returns>
        /// true if the handle is released successfully; otherwise, in the event of a catastrophic failure, false. In this case, it generates a releaseHandleFailed MDA Managed Debugging Assistant.
        /// </returns>
        protected override bool ReleaseHandle()
        {
            return Win32Native.CloseHandle(this.handle);
        }

        /// <summary>
        /// Win32Native
        /// </summary>
        private static class Win32Native
        {
            /// <summary>
            ///     The kernel32
            /// </summary>
            private const string Kernel32 = "kernel32.dll";

            /// <summary>
            ///     Closes the handle.
            /// </summary>
            /// <param name="handle">The handle.</param>
            /// <returns></returns>
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [ResourceExposure(ResourceScope.None)]
            [DllImport(Kernel32, ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool CloseHandle(IntPtr handle);
        }
    }
}