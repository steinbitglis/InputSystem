using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using static Unity.Burst.CompilerServices.Aliasing;

namespace UnityEngine.InputSystem.DataPipeline.TypeConversion
{
    // Converts 2 dimensional vector to single float magnitude.
    // N->N conversion.
    [BurstCompile]
    internal unsafe struct Vec2ToMagnitude
    {
        [ReadOnly] [NoAlias] public float* srcX;
    
        [ReadOnly] [NoAlias] public float* srcY;
    
        [ReadOnly] public int* srcLength;
    
        [WriteOnly] [NoAlias] public float* dst;
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute()
        {
            var l = *srcLength;
            for (var i = 0; i < l; ++i)
                dst[i] = new Vector2(srcX[i], srcY[i]).magnitude;
        }
    }
}