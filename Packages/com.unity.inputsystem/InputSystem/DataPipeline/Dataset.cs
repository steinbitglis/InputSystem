using System.Runtime.CompilerServices;
using Unity.Collections;

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

    public struct Dataset
    {
        // TODO values before current ones?

        // AOT allocated, shouldn't change size during pipeline run.
        internal UnsafeNativeSlice<int> lengthsUnsafe;
        internal UnsafeNativeSlice<int> maxLengthsUnsafe;

        // Runtime adjusted based on input size
        internal UnsafeNativeSlice<ulong> timestampsUnsafe;

        // AOT allocated, shouldn't change size during pipeline run.
        // Count and indexing is the same as lengths
        internal UnsafeNativeSlice<int> timestampsOffsetUnsafe;

        // Runtime adjusted based on input size
        internal UnsafeNativeSlice<float> valuesUnsafe;

        // AOT allocated, shouldn't change size during pipeline run.
        internal UnsafeNativeSlice<int> valuesOffsetsUnsafe;

        // Runtime adjusted based on input size
        internal UnsafeNativeSlice<byte> valuesOpaqueUnsafe;

        // Opaque offsets are in bytes.
        // AOT allocated, shouldn't change size during pipeline run.
        internal UnsafeNativeSlice<int> valuesOpaqueOffsetsUnsafe;

        // Sets destination length to be equal to source length, and returns the length.
        // Destination should be pointing to the same timestamp index as the source.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int MapNToN<T1, T2>(T1 src, T2 dst) where T1 : IStepFunction where T2 : IStepFunction
        {
            var lengths = lengthsUnsafe.ToNativeSlice();
            var maxLenghts = maxLengthsUnsafe.ToNativeSlice();

            var length = lengths[src.timestampsProperty];
            lengths[dst.timestampsProperty] = length;

            Debug.Assert(length <= maxLenghts[dst.timestampsProperty]);
            Debug.Assert(src.timestampsProperty == dst.timestampsProperty);

            return length;
        }

        // Sets destination length to be equal to sum of source lengths, and returns the length.
        // All arguments should be pointing to different timestamp indices.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (int lenth1, int length2) MapNAndMToNPlusM<T1, T2, T3>(T1 src1, T2 src2, T3 dst) where T1 : IStepFunction
            where T2 : IStepFunction
            where T3 : IStepFunction
        {
            var lengths = lengthsUnsafe.ToNativeSlice();
            var maxLenghts = maxLengthsUnsafe.ToNativeSlice();

            var length1 = lengths[src1.timestampsProperty];
            var length2 = lengths[src2.timestampsProperty];
            lengths[dst.timestampsProperty] = length1 + length2;

            Debug.Assert((length1 + length2) <= maxLenghts[dst.timestampsProperty]);
            Debug.Assert(src1.timestampsProperty != src2.timestampsProperty);
            Debug.Assert(src1.timestampsProperty != dst.timestampsProperty);
            Debug.Assert(src2.timestampsProperty != dst.timestampsProperty);

            return (length1, length2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<ulong> GetTimestamps<T>(T stepfunction) where T : IStepFunction
        {
            var lengths = lengthsUnsafe.ToNativeSlice();
            var timestampsOffset = timestampsOffsetUnsafe.ToNativeSlice();

            var offset = timestampsOffset[stepfunction.timestampsProperty];
            var length = lengths[stepfunction.timestampsProperty];
            return timestampsUnsafe.ToNativeSlice(offset, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<float> GetValuesX<T>(T stepfunction) where T : IStepFunctionWithActuationNotion
        {
            var lengths = lengthsUnsafe.ToNativeSlice();
            var valuesOffsets = valuesOffsetsUnsafe.ToNativeSlice();

            Debug.Assert(stepfunction.dimensionsCount >= 1);
            var offset = valuesOffsets[stepfunction.valuesXProperty];
            var length = lengths[stepfunction.timestampsProperty];
            return valuesUnsafe.ToNativeSlice(offset, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<float> GetValuesY<T>(T stepfunction) where T : IStepFunctionWithActuationNotion
        {
            var lengths = lengthsUnsafe.ToNativeSlice();
            var valuesOffsets = valuesOffsetsUnsafe.ToNativeSlice();

            Debug.Assert(stepfunction.dimensionsCount >= 2);
            var offset = valuesOffsets[stepfunction.valuesYProperty];
            var length = lengths[stepfunction.timestampsProperty];
            return valuesUnsafe.ToNativeSlice(offset, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<float> GetValuesZ<T>(T stepfunction) where T : IStepFunctionWithActuationNotion
        {
            var lengths = lengthsUnsafe.ToNativeSlice();
            var valuesOffsets = valuesOffsetsUnsafe.ToNativeSlice();

            Debug.Assert(stepfunction.dimensionsCount >= 3);
            var offset = valuesOffsets[stepfunction.valuesZProperty];
            var length = lengths[stepfunction.timestampsProperty];
            return valuesUnsafe.ToNativeSlice(offset, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<TResult> GetValuesOpaque<TResult, TStepFunction>(TStepFunction stepfunction)
            where TStepFunction : IStepFunctionOpaque where TResult : struct
        {
            var lengths = lengthsUnsafe.ToNativeSlice();
            var valuesOpaqueOffsets = valuesOpaqueOffsetsUnsafe.ToNativeSlice();

            var offset = valuesOpaqueOffsets[stepfunction.opaqueValuesProperty];
            var stride = stepfunction.valueStrideProperty;
            var length = lengths[stepfunction.timestampsProperty];
            return valuesOpaqueUnsafe.ToNativeSlice<TResult>(offset, stride, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<int> GetValuesOpaque(StepFunctionInt stepfunction)
        {
            var lengths = lengthsUnsafe.ToNativeSlice();
            var valuesOpaqueOffsets = valuesOpaqueOffsetsUnsafe.ToNativeSlice();

            var offset = valuesOpaqueOffsets[stepfunction.opaqueValuesProperty];
            var length = lengths[stepfunction.timestampsProperty];
            return valuesOpaqueUnsafe.ToNativeSlice<int>(offset, sizeof(int), length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe NativeSlice<Quaternion> GetValuesOpaque(StepFunctionQuaternion stepfunction)
        {
            var lengths = lengthsUnsafe.ToNativeSlice();
            var valuesOpaqueOffsets = valuesOpaqueOffsetsUnsafe.ToNativeSlice();

            var offset = valuesOpaqueOffsets[stepfunction.opaqueValuesProperty];
            var length = lengths[stepfunction.timestampsProperty];
            return valuesOpaqueUnsafe.ToNativeSlice<Quaternion>(offset, sizeof(Quaternion), length);
        }
    };

    internal struct DatasetAllocator
    {
        public struct Config
        {
            // Amount of all timestamp axes across all step functions.
            // e.g. if you have StepFunction2D and StepFunction3D, amount of timestamp axes will be 2.
            public int timestampAxesCount;

            // Amount of all value axes across all step functions.
            // e.g. if you have StepFunction2D and StepFunction3D, amount of value axes will be 5.
            public int valuesAxesCount;

            // Amount of all opaque value axes across all step functions.
            // e.g. if you have StepFunctionQuaternion and StepFunctionInt,
            // amount of opaque value axes will be 2, because Quaternion value is treated as one dimensional axis.
            public int opaqueValuesAxesCount;

            // Amount of timestamp samples across all step functions.
            // e.g. if you have StepFunction2D with 2 timestamps and StepFunction3D with 3 timestamps, amount of timestamp axes will be 5.
            public int timestampsCount;

            // Amount of value samples across all step functions.
            // e.g. if you have StepFunction2D with 4 timestamps and StepFunction3D with 5 timestamps, amount of values will be 2*4 + 3*5 = 23.
            public int valuesCount;

            // Amount of opaque value bytes across all step functions.
            public int opaqueValuesBytes;
        }

        private NativeArray<int> lengths;
        private NativeArray<int> maxLengths;

        private NativeArray<ulong> timestamps;

        // Count and indexing is the same as lengths
        private NativeArray<int> timestampsOffset;

        private NativeArray<float> values;

        private NativeArray<int> valuesOffsets;

        private NativeArray<byte> valuesOpaque;

        // Opaque offsets are in bytes.
        private NativeArray<int> valuesOpaqueOffsets;

        private Config config;

        public void InitialConfiguration()
        {
            // burst doesn't like empty arrays, so we have to allocate _something_
            const int length = 1;
            lengths = new NativeArray<int>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            maxLengths = new NativeArray<int>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            timestampsOffset =
                new NativeArray<int>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            valuesOffsets = new NativeArray<int>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            valuesOpaqueOffsets =
                new NativeArray<int>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            timestamps = new NativeArray<ulong>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            values = new NativeArray<float>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            valuesOpaque = new NativeArray<byte>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }

        public Dataset Reconfigure(Config newConfig)
        {
            var oldConfig = config;
            
            // TODO make is a smart allocator that doesn't deallocate memory if not needed 

            if (oldConfig.timestampAxesCount != newConfig.timestampAxesCount)
            {
                if (lengths.IsCreated)
                    lengths.Dispose();
                if (maxLengths.IsCreated)
                    maxLengths.Dispose();
                if (timestampsOffset.IsCreated)
                    timestampsOffset.Dispose();

                // burst doesn't like empty arrays, so we have to allocate _something_
                var length = newConfig.timestampAxesCount >= 1 ? newConfig.timestampAxesCount : 1;
                lengths = new NativeArray<int>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                maxLengths = new NativeArray<int>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                timestampsOffset =
                    new NativeArray<int>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }

            if (oldConfig.valuesAxesCount != newConfig.valuesAxesCount)
            {
                if (valuesOffsets.IsCreated)
                    valuesOffsets.Dispose();
                var length = newConfig.valuesAxesCount >= 1 ? newConfig.valuesAxesCount : 1;
                valuesOffsets =
                    new NativeArray<int>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }

            if (oldConfig.opaqueValuesAxesCount != newConfig.opaqueValuesAxesCount)
            {
                if (valuesOpaqueOffsets.IsCreated)
                    valuesOpaqueOffsets.Dispose();
                var length = newConfig.opaqueValuesAxesCount >= 1 ? newConfig.opaqueValuesAxesCount : 1;
                valuesOpaqueOffsets =
                    new NativeArray<int>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }

            if (oldConfig.timestampsCount != newConfig.timestampsCount)
            {
                if (timestamps.IsCreated)
                    timestamps.Dispose();
                var length = newConfig.timestampsCount >= 1 ? newConfig.timestampsCount : 1;
                timestamps =
                    new NativeArray<ulong>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }

            if (oldConfig.valuesCount != newConfig.valuesCount)
            {
                if (values.IsCreated)
                    values.Dispose();
                var length = newConfig.valuesCount >= 1 ? newConfig.valuesCount : 1;
                values = new NativeArray<float>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }

            if (oldConfig.opaqueValuesBytes != newConfig.opaqueValuesBytes)
            {
                if (valuesOpaque.IsCreated)
                    valuesOpaque.Dispose();
                var length = newConfig.opaqueValuesBytes >= 1 ? newConfig.opaqueValuesBytes : 1;
                valuesOpaque =
                    new NativeArray<byte>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }

            config = newConfig;

            return new Dataset
            {
                lengthsUnsafe = lengths.ToUnsafeNativeSlice(),
                maxLengthsUnsafe = maxLengths.ToUnsafeNativeSlice(),
                timestampsUnsafe = timestamps.ToUnsafeNativeSlice(),
                timestampsOffsetUnsafe = timestampsOffset.ToUnsafeNativeSlice(),
                valuesUnsafe = values.ToUnsafeNativeSlice(),
                valuesOffsetsUnsafe = valuesOffsets.ToUnsafeNativeSlice(),
                valuesOpaqueUnsafe = valuesOpaque.ToUnsafeNativeSlice(),
                valuesOpaqueOffsetsUnsafe = valuesOpaqueOffsets.ToUnsafeNativeSlice()
            };
        }

        public void CalculateOffsetsBasedOnLengths()
        {
            // TODO
        }

        public void Dispose()
        {
            lengths.Dispose();
            maxLengths.Dispose();
            timestamps.Dispose();
            timestampsOffset.Dispose();
            values.Dispose();
            valuesOffsets.Dispose();
            valuesOpaque.Dispose();
            valuesOpaqueOffsets.Dispose();
        }
    }
}