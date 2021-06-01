using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;

namespace UnityEngine.InputSystem.DataPipeline.Demux
{
    public unsafe struct DynamicDemuxer
    {
        public enum SourceDataType
        {
            TwosComplementSignedBits,
            ExcessKSignedBits,
            UnsignedBits,
            Float32
        }

        public enum DestinationDataType
        {
            SignedBits,
            Float32
        }

        public struct Field
        {
            public ulong maskA;
            public ulong maskB;
            public byte shiftA;
            public byte shiftB;
            public SourceDataType srcType;
            public DestinationDataType dstType;
            public int srcUlongPairIndex;
            public int dstIndex;
        }

        [ReadOnly] [NoAlias] public Field* fields;

        [ReadOnly] public int fieldCount;

        [ReadOnly] [NoAlias] public ulong** srcStructs;

        [ReadOnly] public int srcStructCount;

        [ReadOnly] public int srcStructLength;

        [NoAlias] public ulong* prevState;

        [NoAlias] public bool gotFirst;

        [NoAlias] public ulong* changed;

        [WriteOnly] [NoAlias] public float** dstFloatData;

        [WriteOnly] [NoAlias] public int** dstFloatLengths;

        [WriteOnly] [NoAlias] public int** dstIntData;

        [WriteOnly] [NoAlias] public int** dstIntLengths;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute()
        {
            for (var srcStructIndex = 0; srcStructIndex < srcStructCount; ++srcStructIndex)
            {
                var srcStruct = srcStructs[srcStructIndex];

                if (!gotFirst)
                {
                    // invert all bits so all of them are in changed mask first time
                    for (var i = 0; i < srcStructLength; ++i)
                        prevState[i] = ~srcStruct[i];
                    gotFirst = true;
                }

                for (var i = 0; i < srcStructLength; ++i)
                    changed[i] = srcStruct[i] ^ prevState[i];

                for (var i = 0; i < srcStructLength; ++i)
                    prevState[i] = srcStruct[i];

                for (var i = 0; i < fieldCount; ++i)
                {
                    var f = fields[i];

                    var isChanged = ((changed[f.srcUlongPairIndex] & f.maskA) |
                                     (changed[f.srcUlongPairIndex + 1] & f.maskB)) != 0;

                    if (!isChanged)
                        continue;

                    var rawData = ((srcStruct[f.srcUlongPairIndex] & f.maskA) >> f.shiftA) +
                                  ((srcStruct[f.srcUlongPairIndex + 1] & f.maskB) << f.shiftB);

                    switch (f.srcType)
                    {
                        // case SourceDataType.TwosComplementSignedBits:
                        //     break;
                        // case SourceDataType.ExcessKSignedBits:
                        //     break;
                        case SourceDataType.UnsignedBits:
                        {
                            var data = (uint) rawData;
                            switch (f.dstType)
                            {
                                case DestinationDataType.SignedBits:
                                    dstIntData[f.dstIndex][(*dstIntLengths[f.dstIndex])++] = (int) data;
                                    break;
                                case DestinationDataType.Float32:
                                    dstFloatData[f.dstIndex][(*dstFloatLengths[f.dstIndex])++] = (float) data;
                                    break;
                            }

                            break;
                        }
                        case SourceDataType.Float32:
                        {
                            var data = *(float*) &rawData;
                            switch (f.dstType)
                            {
                                case DestinationDataType.SignedBits:
                                    dstIntData[f.dstIndex][(*dstIntLengths[f.dstIndex])++] = (int) data;
                                    break;
                                case DestinationDataType.Float32:
                                    dstFloatData[f.dstIndex][(*dstFloatLengths[f.dstIndex])++] = (float) data;
                                    break;
                            }

                            break;
                        }
                    }
                }
            }
        }


    }
}