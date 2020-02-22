// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Anori Soft">
// Copyright (c) Anori Soft. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace ProcessorAffinity
{
    using System.Linq;

    internal class Program
    {
        private static void Main(string[] args)
        {
            //Set affinity to first CPU
            var currentaffinity = ProcessorAffinity.SetThreadAffinity(0);
            Console.WriteLine("Running on CPU #{0}", ProcessorAffinity.GetCurrentProcessorNumber());
            //Restore last affinity
            ProcessorAffinity.SetThreadAffinity(currentaffinity.ToArray());

            //Cycle through all logical CPUs
            for (var cpuid = 0; cpuid < Environment.ProcessorCount; cpuid++)
            {
                using (ProcessorAffinity.BeginAffinity(cpuid))
                {
                    Console.WriteLine("Running on CPU #{0}", ProcessorAffinity.GetCurrentProcessorNumber());
                }
            }
        }
    }
}