using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.InputSystem.DataPipeline
{
    // A generic step function interface.
    // Only describes X axis: time.
    // timestampsProperty is index in offset time of timestamps
    // length is described in lengths array. 
    public interface IStepFunction
    {
        public int timestampsProperty { get; }
    };
    
    // A step function where values have actuation/magnitude notion. Like a single value, 2d/3d vector, etc.
    // Every dimension is Y axis: float value.
    public interface IStepFunctionWithActuationNotion : IStepFunction
    {
        public int dimensionsCount { get; }
        // this is silly workaround to avoid managed arrays
        public int valuesXProperty { get; }
        public int valuesYProperty { get; }
        public int valuesZProperty { get; }
    };
    
    // A step function where values are an opaque binary blob.
    // Useful for plumbing through values that don't have actuation concept: pose, quaternion, etc.
    public interface IStepFunctionOpaque : IStepFunction
    {
        public int opaqueValuesProperty { get; }
        public int valueStrideProperty { get; }
    };

    public struct StepFunction1D : IStepFunctionWithActuationNotion
    {
        public int timestamps;
        public int valuesX;

        public int timestampsProperty => timestamps;
        public int dimensionsCount => 1;
        public int valuesXProperty => valuesX;
        public int valuesYProperty => 0;
        public int valuesZProperty => 0;
    };

    public struct StepFunction2D : IStepFunctionWithActuationNotion
    {
        public int timestamps;
        public int valuesX;
        public int valuesY;

        public int timestampsProperty => timestamps;
        public int dimensionsCount => 2;
        public int valuesXProperty => valuesX;
        public int valuesYProperty => valuesY;
        public int valuesZProperty => 0;
    };

    public struct StepFunction3D : IStepFunctionWithActuationNotion
    {
        public int timestamps;
        public int valuesX;
        public int valuesY;
        public int valuesZ;

        public int timestampsProperty => timestamps;
        public int dimensionsCount => 3;
        public int valuesXProperty => valuesX;
        public int valuesYProperty => valuesY;
        public int valuesZProperty => valuesZ;
    };

    // A completely opaque step function, useful for kernels that don't look inside the data.
    public struct StepFunctionOpaque : IStepFunctionOpaque
    {
        public int timestamps;
        public int opaqueValues;
        public int valueStride;

        public int timestampsProperty => timestamps;
        public int opaqueValuesProperty => opaqueValues;
        public int valueStrideProperty => valueStride;
    };

    public struct StepFunctionQuaternion : IStepFunctionOpaque
    {
        public int timestamps;
        public int opaqueValues;

        public int timestampsProperty => timestamps;
        public int opaqueValuesProperty => opaqueValues;
        public unsafe int valueStrideProperty => sizeof(Quaternion);
    };
    
    public struct StepFunctionInt : IStepFunctionOpaque
    {
        public int timestamps;
        public int opaqueValues;

        public int timestampsProperty => timestamps;
        public int opaqueValuesProperty => opaqueValues;
        public int valueStrideProperty => sizeof(int);
    };

    [BurstCompile]
    public unsafe struct Dataset
    {
        // TODO values before current ones?

        public int* lengths;

        public ulong* timestamps;
        public int timestampsAllocCount;
        public int* timestampsOffset;
        
        public float* values;
        public int valuesAllocCount;
        public int* valuesOffsets;
        
        public void* valuesOpaque;
        public int valuesOpaqueAllocSize;
        // opaque offsets are in bytes
        public int* valuesOpaqueOffsets;

        // Sets destination length to be equal to source length, and returns the length.
        // Destination should be pointing to the same timestamp index as the source.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public int MapNToN<T1, T2>(T1 src, T2 dst) where T1 : IStepFunction where T2 : IStepFunction
        {
            var length = lengths[src.timestampsProperty];
            lengths[dst.timestampsProperty] = length;
            Debug.Assert(src.timestampsProperty == dst.timestampsProperty);
            return length;
        }
        
        // Sets destination length to be equal to sum of source lengths, and returns the length.
        // All arguments should be pointing to different timestamp indices.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public (int lenth1, int length2) MapNAndMToNPlusM<T1, T2, T3>(T1 src1, T2 src2, T3 dst) where T1 : IStepFunction where T2 : IStepFunction where T3 : IStepFunction
        {
            var length1 = lengths[src1.timestampsProperty];
            var length2 = lengths[src2.timestampsProperty];
            lengths[dst.timestampsProperty] = length1 + length2;
            Debug.Assert(src1.timestampsProperty != src2.timestampsProperty);
            Debug.Assert(src1.timestampsProperty != dst.timestampsProperty);
            Debug.Assert(src2.timestampsProperty != dst.timestampsProperty);
            return (length1, length2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public NativeSlice<ulong> GetTimestamps<T>(T stepfunction) where T : IStepFunction
        {
            var offset = timestampsOffset[stepfunction.timestampsProperty];
            var length = lengths[stepfunction.timestampsProperty];
            return NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<ulong>(timestamps + offset, sizeof(ulong), length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public NativeSlice<float> GetValuesX<T>(T stepfunction) where T : IStepFunctionWithActuationNotion
        {
            Debug.Assert(stepfunction.dimensionsCount >= 1);
            var offsetX = valuesOffsets[stepfunction.valuesXProperty];
            var length = lengths[stepfunction.timestampsProperty];
            return NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<float>(values + offsetX, sizeof(float), length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public NativeSlice<float> GetValuesY<T>(T stepfunction) where T : IStepFunctionWithActuationNotion
        {
            Debug.Assert(stepfunction.dimensionsCount >= 2);
            var offsetY = valuesOffsets[stepfunction.valuesYProperty];
            var length = lengths[stepfunction.timestampsProperty];
            return NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<float>(values + offsetY, sizeof(float), length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public NativeSlice<float> GetValuesZ<T>(T stepfunction) where T : IStepFunctionWithActuationNotion
        {
            Debug.Assert(stepfunction.dimensionsCount >= 3);
            var offsetZ = valuesOffsets[stepfunction.valuesZProperty];
            var length = lengths[stepfunction.timestampsProperty];
            return NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<float>(values + offsetZ, sizeof(float), length);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public NativeSlice<TResult> GetValuesOpaque<TResult, TStepFunction>(TStepFunction stepfunction) where TStepFunction : IStepFunctionOpaque where TResult : struct
        {
            var offset = valuesOpaqueOffsets[stepfunction.opaqueValuesProperty];
            var stride = stepfunction.valueStrideProperty;
            var length = lengths[stepfunction.timestampsProperty];
            return NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<TResult>((byte*)valuesOpaque + offset, stride, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public NativeSlice<int> GetValuesOpaque(StepFunctionInt stepfunction)
        {
            var offset = valuesOpaqueOffsets[stepfunction.opaqueValuesProperty];
            var length = lengths[stepfunction.timestampsProperty];
            return NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<int>((byte*)valuesOpaque + offset, sizeof(int), length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public NativeSlice<Quaternion> GetValuesOpaque(StepFunctionQuaternion stepfunction)
        {
            var offset = valuesOpaqueOffsets[stepfunction.opaqueValuesProperty];
            var length = lengths[stepfunction.timestampsProperty];
            return NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<Quaternion>((byte*)valuesOpaque + offset, sizeof(Quaternion), length);
        }
    };
}