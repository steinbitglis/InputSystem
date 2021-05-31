using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;

namespace UnityEngine.InputSystem.DataPipeline.SlidingWindow
{
    [BurstCompile]
    public struct Accumulate1D
    {
        public StepFunction1D src, dst;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(Dataset dataset)
        {
            var l = dataset.MapNToN(src, dst);
            var v = dataset.GetValuesX(src);
            var r = dataset.GetValuesX(dst);

            // TODO prefix sum on SIMD 
            var acc = 0.0f;
            for (var i = 0; i < l; ++i)
            {
                acc += v[i];
                r[i] = acc;
            }
        }
    }
}