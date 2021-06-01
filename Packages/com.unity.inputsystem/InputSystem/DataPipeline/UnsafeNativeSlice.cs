using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.InputSystem.DataPipeline
{
    // Can't use NativeSlice because Dataset is passed around as an argument, so it has to be a non-managed type.
    // All memory ownership is done on root level, so nuking safety handles from slices should be safe enough ish.
    public unsafe struct UnsafeNativeSlice<T> where T : struct
    {
        [NativeDisableUnsafePtrRestriction] [NoAlias]
        public void* ptr;

        public int length;

        public int stride;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeNativeSlice(NativeArray<T> array)
        {
            ptr = array.GetUnsafePtr();
            length = array.Length;
            stride = UnsafeUtility.SizeOf<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<T> ToNativeSlice()
        {
            return NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<T>(ptr, stride, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<T> ToNativeSlice(int offsetInItems, int lengthsInItems)
        {
            return NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<T>(
                (byte*) ptr + stride * offsetInItems, stride, lengthsInItems);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<TOther> ToNativeSlice<TOther>(int offsetInBytes, int itemStride, int lengthsInItems)
            where TOther : struct
        {
            return NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<TOther>((byte*) ptr + offsetInBytes,
                itemStride, lengthsInItems);
        }
    }
    
    public static class UnsafeNativeSliceExtensions
    {
        public static UnsafeNativeSlice<T> ToUnsafeNativeSlice<T>(this NativeArray<T> array) where T : struct
        {
            return new UnsafeNativeSlice<T>(array);
        }
    }
}