using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;

namespace UnityEngine.InputSystem.DataPipeline.TypeConversion
{
    // Converts single integer enum component to single float component.
    // Applies bit mask first, then looks into enum LUT with offset.
    // N->N conversion.
    internal struct EnumToFloat
    {
        public StepFunctionInt src;
        public StepFunction1D dst;

        public int mask, offset;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Map(Dataset dataset)
        {
            dataset.MapNToN(src, dst);
        }

        // TODO maybe lut should be in some static array?

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(Dataset dataset, UnsafeNativeSlice<float> lutUnsafe)
        {
            var l = dataset.MapNToN(src, dst);
            var v = dataset.GetValuesOpaque(src);
            var r = dataset.GetValuesX(dst);
            var lut = lutUnsafe.ToNativeSlice();

            // TODO No SIMD here yet :( do we need AVX-512?
            for (var i = 0; i < l; ++i)
                r[i] = lut[(v[i] & mask) + offset];
        }

        // [BurstCompile]
        // public static void Execute(ref EnumToFloat self, ref Dataset dataset, ref UnsafeNativeSlice<float> lutUnsafe)
        // {
        //     self.Execute(dataset, lutUnsafe);
        // }
    }
}