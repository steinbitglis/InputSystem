using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using static Unity.Burst.CompilerServices.Aliasing;

namespace UnityEngine.InputSystem.DataPipeline.Merger
{
    // Merges two 1D slices into one 1D slice in order of timestamps.
    // x(N)+y(M)->z(N+M) conversion.
    [BurstCompile]
    public unsafe struct Latest1D
    {
        [ReadOnly] [NoAlias] public ulong* srcTimestamps1;
        [ReadOnly] [NoAlias] public float* srcValues1;
        [ReadOnly] [NoAlias] public int* srcLength1;

        [ReadOnly] [NoAlias] public ulong* srcTimestamps2;
        [ReadOnly] [NoAlias] public float* srcValues2;
        [ReadOnly] [NoAlias] public int* srcLength2;

        [WriteOnly] [NoAlias] public ulong* dstTimestamps;
        [WriteOnly] [NoAlias] public float* dstValues;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute()
        {
            var i1 = 0;
            var i2 = 0;

            var l1 = *srcLength1;
            var l2 = *srcLength2;
            
            for (var i3 = 0; i3 < l1 + l2; ++i3)
            {
                if (i1 < l1 && (i2 >= l2 || srcTimestamps1[i1] <= srcTimestamps2[i2]))
                {
                    dstTimestamps[i3] = srcTimestamps1[i1];
                    dstValues[i3] = srcValues1[i1];
                    ++i1;
                }
                else
                {
                    dstTimestamps[i3] = srcTimestamps2[i2];
                    dstValues[i3] = srcValues2[i2];
                    ++i2;
                }
            }
        }
    }
}