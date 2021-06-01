using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine.InputSystem.DataPipeline.Demux;
using UnityEngine.InputSystem.DataPipeline.Merger;
using UnityEngine.InputSystem.DataPipeline.Processor;
using UnityEngine.InputSystem.DataPipeline.SlidingWindow;
using UnityEngine.InputSystem.DataPipeline.TypeConversion;
using UnityEngine.Profiling;

namespace UnityEngine.InputSystem.DataPipeline
{
    internal struct DataPipeline
    {
        public NativeArray<float> enumsToFloatsLut;
        public NativeArray<EnumToFloat> enumsToFloats;

        public NativeArray<Vec2ToMagnitude> vec2sToMagnitudes;

        public NativeArray<Processor1D> process1Ds;

        public NativeArray<Accumulate1D> accumulate1Ds;

        public NativeArray<Latest1D> latest1Ds;

        private static readonly ProfilerMarker s_MarkerMap = new ProfilerMarker("Map");
        private static readonly ProfilerMarker s_MarkerExecute = new ProfilerMarker("Execute");
        private static readonly ProfilerMarker s_MarkerEnumToFloat = new ProfilerMarker("EnumToFloat");
        private static readonly ProfilerMarker s_MarkerVec2ToMagnitude = new ProfilerMarker("Vec2ToMagnitude");
        private static readonly ProfilerMarker s_MarkerProcessor1D = new ProfilerMarker("Processor1D");
        private static readonly ProfilerMarker s_MarkerAccumulate1D = new ProfilerMarker("Accumulate1D");
        private static readonly ProfilerMarker s_MarkerLatest1D = new ProfilerMarker("Latest1D");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Map(Dataset dataset)
        {
            using (s_MarkerMap.Auto())
            {
                foreach (var op in enumsToFloats)
                    op.Map(dataset);

                foreach (var op in vec2sToMagnitudes)
                    op.Map(dataset);

                foreach (var op in process1Ds)
                    op.Map(dataset);

                foreach (var op in accumulate1Ds)
                    op.Map(dataset);

                foreach (var op in latest1Ds)
                    op.Map(dataset);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(Dataset dataset)
        {
            using (s_MarkerExecute.Auto())
            {
                foreach (var op in enumsToFloats)
                    using (s_MarkerEnumToFloat.Auto())
                        op.Execute(dataset, enumsToFloatsLut.ToUnsafeNativeSlice());

                foreach (var op in vec2sToMagnitudes)
                    using (s_MarkerVec2ToMagnitude.Auto())
                        op.Execute(dataset);

                foreach (var op in process1Ds)
                    using (s_MarkerProcessor1D.Auto())
                        op.Execute(dataset);

                foreach (var op in accumulate1Ds)
                    using (s_MarkerAccumulate1D.Auto())
                        op.Execute(dataset);

                foreach (var op in latest1Ds)
                    using (s_MarkerLatest1D.Auto())
                        op.Execute(dataset);
            }
        }

        public void Dispose()
        {
            enumsToFloatsLut.Dispose();
            enumsToFloats.Dispose();
            vec2sToMagnitudes.Dispose();

            process1Ds.Dispose();

            accumulate1Ds.Dispose();

            latest1Ds.Dispose();
        }
    }
}