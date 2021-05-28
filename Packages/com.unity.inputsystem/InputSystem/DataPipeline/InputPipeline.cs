using Unity.Burst;
using Unity.Collections;
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
    [BurstCompile]
    internal struct InputPipeline : IJob
    {
        public NativeArray<DynamicDemuxer> dynamicDemuxers;

        public NativeArray<EnumToFloat> enumsToFloats;
        public NativeArray<Vec2ToMagnitude> vec2sToMagnitudes;

        public NativeArray<Processor1D> process1Ds;

        public NativeArray<Accumulate1D> accumulate1Ds;

        public NativeArray<Latest1D> latest1Ds;

        private static readonly ProfilerMarker s_MarkerDynamicDemuxer = new ProfilerMarker("DynamicDemuxer");
        private static readonly ProfilerMarker s_MarkerEnumToFloat = new ProfilerMarker("EnumToFloat");
        private static readonly ProfilerMarker s_MarkerVec2ToMagnitude = new ProfilerMarker("Vec2ToMagnitude");
        private static readonly ProfilerMarker s_MarkerProcessor1D = new ProfilerMarker("Processor1D");
        private static readonly ProfilerMarker s_MarkerAccumulate1D = new ProfilerMarker("Accumulate1D");
        private static readonly ProfilerMarker s_MarkerLatest1D = new ProfilerMarker("Latest1D");

        public void Execute()
        {
            foreach (var op in dynamicDemuxers)
                using (s_MarkerDynamicDemuxer.Auto())
                    op.Execute();
            
            foreach (var op in enumsToFloats)
                using (s_MarkerEnumToFloat.Auto())
                    op.Execute();

            foreach (var op in vec2sToMagnitudes)
                using (s_MarkerVec2ToMagnitude.Auto())
                    op.Execute();

            foreach (var op in process1Ds)
                using (s_MarkerProcessor1D.Auto())
                    op.Execute();

            foreach (var op in accumulate1Ds)
                using (s_MarkerAccumulate1D.Auto())
                    op.Execute();

            foreach (var op in latest1Ds)
                using (s_MarkerLatest1D.Auto())
                    op.Execute();
        }

        public void Dispose()
        {
            dynamicDemuxers.Dispose();

            enumsToFloats.Dispose();
            vec2sToMagnitudes.Dispose();

            process1Ds.Dispose();

            accumulate1Ds.Dispose();

            latest1Ds.Dispose();
        }
    }
}