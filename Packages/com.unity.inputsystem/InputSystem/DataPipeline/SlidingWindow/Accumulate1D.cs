using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;

namespace UnityEngine.InputSystem.DataPipeline.SlidingWindow
{
    [BurstCompile]
    public unsafe struct Accumulate1D
    {
        [ReadOnly] [NoAlias] public float* src;

        [ReadOnly] [NoAlias] public int* srcLength;

        [WriteOnly] [NoAlias] public float* dst;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute()
        {
            var l = *srcLength;

            // TODO prefix sum on SIMD 
            var value = 0.0f;
            for (var i = 0; i < l; ++i)
            {
                value += src[i];
                dst[i] = value;
            }
        }
    }
}