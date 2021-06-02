using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.InputSystem.DataPipeline
{
    // Currently burst + NativeArray + temp alloc are having a real hard time with how Input System is wired to Player/Editor loop internals.
    // Rolling a simplistic allocator-array-ish thing to unblock me for now.
    public unsafe struct UnsafeResizableNativeArray
    {
        [NativeDisableUnsafePtrRestriction] [NoAlias]
        internal void* m_Ptr;

        internal int m_AllocationSize;

        internal int m_Length;

        internal AtomicSafetyHandle m_Safety;

        [NativeSetClassTypeToNullOnSchedule] internal DisposeSentinel m_DisposeSentinel;

        private static int s_staticSafetyId;

        private const int k_MinAllocationSize = 1024; // don't bother with allocations <1024 bytes

        private const Allocator k_Label = Allocator.Persistent;

        [BurstDiscard]
        public void Alloc(int length)
        {
            // can't do this inside a job
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 1, k_Label);
            InitStaticSafetyId(ref m_Safety);

            m_Length = length;
            m_AllocationSize = AllocationSizeFromLength(length);
            m_Ptr = UnsafeUtility.Malloc(m_AllocationSize, 16, k_Label);
        }

        public void Realloc(int newLength)
        {
            var oldLength = m_Length;
            m_Length = newLength;

            var newAllocationSize = AllocationSizeFromLength(newLength);

            // add hysteresis to transition points
            // e.g. don't downsize to one below, this will avoid jitter when allocation constantly changing from 1024 to 2048 and back 
            if (newAllocationSize * 2 == m_AllocationSize || newAllocationSize == m_AllocationSize)
                return;

            var newPtr = UnsafeUtility.Malloc(newAllocationSize, 16, k_Label);
            Debug.Assert(m_Ptr != null);
            UnsafeUtility.MemCpy(newPtr, m_Ptr,
                oldLength < newLength ? oldLength : newLength);

            m_Ptr = newPtr;
            m_AllocationSize = newAllocationSize;

            Debug.Assert(m_Length <= m_AllocationSize);
        }

        private static int AllocationSizeFromLength(int length)
        {
            // code from interwebs
            var v = (uint) length;
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;
            return v < k_MinAllocationSize ? k_MinAllocationSize : (int) v;
        }

        [BurstDiscard]
        public void Dispose()
        {
            if (m_Ptr == null)
                return;

            UnsafeUtility.Free(m_Ptr, k_Label);
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
            m_Ptr = null;
            m_AllocationSize = 0;
            m_Length = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<T> ToNativeSlice<T>(int offsetInItems, int lengthInItems) where T : struct
        {
            var stride = UnsafeUtility.SizeOf<T>();
            var offset = stride * offsetInItems;
            var length = lengthInItems * stride;
            Debug.Assert(offset + length < m_Length);
            return NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<T>((byte*) m_Ptr + offset, stride, length);
        }

        [BurstDiscard]
        private static void InitStaticSafetyId(ref AtomicSafetyHandle handle)
        {
            if (s_staticSafetyId == 0)
                s_staticSafetyId =
                    AtomicSafetyHandle.NewStaticSafetyId<UnsafeResizableNativeArray>();
            AtomicSafetyHandle.SetStaticSafetyId(ref handle, s_staticSafetyId);
        }
    }
}