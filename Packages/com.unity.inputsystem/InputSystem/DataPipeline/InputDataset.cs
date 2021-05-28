﻿using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.InputSystem.DataPipeline
{
    /*
    public struct RuntimeLength
    {
        public unsafe int* length;

        public unsafe int Get()
        {
            return *length;
        }
    }
    
    
    public interface ISlice
    {
        public int lengthIndexProperty { get; }
        
        public int timestampsOffsetProperty { get; }
    }
    
    public struct Slice1D : ISlice
    {
        public int offset;
        public int timestampsOffset;
        public int lengthIndex;

        public int lengthIndexProperty => lengthIndex;
        public int timestampsOffsetProperty => timestampsOffset;
    }

    public unsafe struct Slice2D : ISlice
    {
        public fixed int offset[2];
        public int timestampsOffset;
        public int lengthIndex;

        public int lengthIndexProperty => lengthIndex;
        public int timestampsOffsetProperty => timestampsOffset;
    }

    public unsafe struct Slice3D : ISlice
    {
        public fixed int offset[3];
        public int timestampsOffset;
        public int lengthIndex;
        
        public int lengthIndexProperty => lengthIndex;
        public int timestampsOffsetProperty => timestampsOffset;
    }
    
    public struct SliceEnum : ISlice
    {
        public int offset;
        public int timestampsOffset;
        public int lengthIndex;

        public int lengthIndexProperty => lengthIndex;
        public int timestampsOffsetProperty => timestampsOffset;
    }

    public struct InputDataset
    {
        public NativeArray<ulong> timestamps;
        public NativeArray<float> values;
        public NativeArray<int> lengths;

        public NativeArray<int> enumValues;
        public NativeArray<float> enumLUT;

        // TODO values before current ones?
        // TODO many-dimensions values?

        public int SetLengthAsNToNMapping<A, B>(A src, B dst) where A : ISlice where B : ISlice 
        {
            var length = lengths[src.lengthIndexProperty];
            lengths[dst.lengthIndexProperty] = length;
            return length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe RuntimeLength GetLengthWIP(int index)
        {
            return new RuntimeLength
            {
                length = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(lengths) + index
            };
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<ulong> GetTimestamps<T>(T slice) where T : ISlice
        {
            return new NativeSlice<ulong>(timestamps, slice.timestampsOffsetProperty, lengths[slice.lengthIndexProperty]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<float> GetValues(Slice1D slice)
        {
            return new NativeSlice<float>(values, slice.offset, lengths[slice.lengthIndex]);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<int> GetValues(SliceEnum slice)
        {
            return new NativeSlice<int>(enumValues, slice.offset, lengths[slice.lengthIndex]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe (NativeSlice<float> x, NativeSlice<float> y) GetValues(Slice2D slice)
        {
            return (new NativeSlice<float>(values, slice.offset[0], lengths[slice.lengthIndex]),
                new NativeSlice<float>(values, slice.offset[1], lengths[slice.lengthIndex]));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe (NativeSlice<float> x, NativeSlice<float> y, NativeSlice<float> z) GetValues(Slice3D slice)
        {
            return (new NativeSlice<float>(values, slice.offset[0], lengths[slice.lengthIndex]),
                new NativeSlice<float>(values, slice.offset[1], lengths[slice.lengthIndex]),
                new NativeSlice<float>(values, slice.offset[2], lengths[slice.lengthIndex]));
        }
    };
    */
}