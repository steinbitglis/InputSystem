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
        public StepFunctionInt src;
        public StepFunction1D dst;

        public int mask, offset;
        
        // TODO maybe lut should be in some static array?

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(Dataset dataset, float* lut)
        {
            var l = dataset.MapNToN(src, dst);
            var v = dataset.GetValuesOpaque(src);
            var r = dataset.GetValuesX(dst);
            
            // TODO No SIMD here yet :( do we need AVX-512?
            for (var i = 0; i < l; ++i)
                r[i] = lut[(v[i] & mask) + offset];
        }
    }
}