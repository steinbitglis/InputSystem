using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;

namespace UnityEngine.InputSystem.DataPipeline.TypeConversion
{
    // Converts single integer enum component to single float component.
    // Applies bit mask first, then looks into enum LUT with offset.
    // N->N conversion.
    [BurstCompile]
    internal unsafe struct EnumToFloat
    {
        [ReadOnly] [NoAlias] public int* src;
    
        [ReadOnly] [NoAlias] public float* lut;
    
        [ReadOnly] public int* srcLength;
    
        public int mask;
    
        [WriteOnly] [NoAlias] public float* dst;
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute()
        {
            // TODO No SIMD here yet :( do we need AVX-512?
            var l = *srcLength;
            for (var i = 0; i < l; ++i)
                dst[i] = lut[src[i] & mask];
        }
    }
}