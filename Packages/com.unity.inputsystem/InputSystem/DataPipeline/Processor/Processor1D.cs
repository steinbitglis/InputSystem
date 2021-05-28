﻿using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;

namespace UnityEngine.InputSystem.DataPipeline.Processor
{
    // Processes single component value
    // N->N conversion.
    [BurstCompile]
    internal unsafe struct Processor1D
    {
        [ReadOnly] [NoAlias] public float* src;

        [ReadOnly] public int* srcLength;

        [WriteOnly] [NoAlias] public float* dst;

        // [minRange, maxRange] for clamp or compare 
        public float minRange, maxRange;

        // if 1.0f uses compare results instead of clamped value
        public float compare;

        // if compare == 1.0f and value is in [minRange, maxRange] then value is replaced by compareResultIfInRange
        // if compare == 1.0f and value is not in [minRange, maxRange] then value is replaced by compareResultIfOutOfRange
        public float compareResultIfInRange, compareResultIfOutOfRange;

        // if 1.0f value is normalized to 0.0f where 0.0f point is defined by minRange, 1.0f is maxRange
        public float normalize;

        // result value scale and offset factor
        public float scale;
        public float offset;

        // if 1.0f value is converted to absolute value before processing and converted back to signed afterwards
        public float processAsAbs;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute()
        {
            var l = *srcLength;

            for (var i = 0; i < l; ++i)
            {
                var v0 = src[i];

                // branchless conditional abs
                var v1 = Mathf.LerpUnclamped(v0, (v0 < 0.0f ? -v0 : v0), processAsAbs);

                // branchless conditional clamp|compare
                var clamped = Mathf.Clamp(v1, minRange, maxRange);
                var banded = (v1 <= maxRange)
                    ? (v1 >= minRange ? compareResultIfInRange : compareResultIfOutOfRange)
                    : compareResultIfOutOfRange;
                var v2 = Mathf.LerpUnclamped(clamped, banded, compare);

                // branchless conditional normalize
                var normalized = (v2 - minRange) / (maxRange - minRange);
                var v3 = Mathf.LerpUnclamped(v2, normalized, normalize);

                // branchless conditional sign restore
                var v4 = Mathf.LerpUnclamped(v3, (v0 < 0.0f ? -v3 : v3), processAsAbs);

                // FMA
                var v5 = v4 * scale + offset;

                dst[i] = v5;
            }
        }
    }
}