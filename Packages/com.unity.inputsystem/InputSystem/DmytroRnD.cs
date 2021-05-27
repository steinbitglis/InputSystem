using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
//using UnityEditorInternal;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Profiling;
using UnityEngineInternal.Input;
using UnityEngine.InputSystem.DataPipeline;

namespace UnityEngine.InputSystem.DmytroRnD
{
    internal static class Core
    {
        public static NativeDeviceState[] Devices;
        public static ComputationalGraph Graph;
        public static bool IsInitialized = false;

        internal static void NativeSetup()
        {
            return;
            Devices = new NativeDeviceState[0];
            Graph.Setup();

            IsInitialized = true;
        }

        internal static void NativeClear()
        {
            return;

            for (var i = 0; i < Devices.Length; ++i)
                Devices[i].Clear();
            Devices = new NativeDeviceState[0];

            Graph.Clear();

            // m_TestPreDemuxer.Clear();

            IsInitialized = false;
        }

        internal static void NativeBeforeUpdate(NativeInputUpdateType updateType)
        {
        }

        public static float outputvar;


        internal static unsafe void NativeUpdate(NativeInputUpdateType updateType, NativeInputEventBuffer* buffer)
        {
            Profiler.BeginSample("Core.NativeUpdate");

            var dataset = new InputDataset();

            const int count = 10000;

            dataset.lengths = new NativeArray<int>(2, Allocator.Persistent);
            dataset.timestamps = new NativeArray<ulong>(count * 3, Allocator.Persistent);
            dataset.values = new NativeArray<float>(count * 3, Allocator.Persistent);
            dataset.enumLUT = new NativeArray<float>(1, Allocator.Persistent);
            dataset.enumValues = new NativeArray<int>(1, Allocator.Persistent);

            var step1 = new Slice2D();
            step1.offset[0] = count * 0;
            step1.offset[1] = count * 1;
            step1.lengthIndex = 0;
            dataset.lengths[0] = count;
            
            var step2 = new Slice1D();
            step2.offset = count * 2;
            step2.lengthIndex = 1;
            
            var (px, py) = dataset.GetValues(step1);
            for (var i = 0; i < dataset.lengths[step1.lengthIndex]; ++i)
            {
                px[i] = Random.Range(-5.0f, 5.0f);
                py[i] = Random.Range(-5.0f, 5.0f);
            }

            var enum2int =
                new NativeArray<TypeConversionEnumToFloat.Operation>(new TypeConversionEnumToFloat.Operation[]
                    {
                        
                    },
                    Allocator.Persistent);
            var vec2mag =
                new NativeArray<TypeConversionVector2ToMagnitude.Operation>(
                    new TypeConversionVector2ToMagnitude.Operation[]
                    {
                        new TypeConversionVector2ToMagnitude.Operation
                        {
                            src = step1,
                            dst = step2
                        }
                    },
                    Allocator.Persistent);
            var vec3mag =
                new NativeArray<TypeConversionVector3ToMagnitude.Operation>(
                    new TypeConversionVector3ToMagnitude.Operation[]
                    {
                        
                    },
                    Allocator.Persistent);
            var floatOps =
                new NativeArray<Processor1D.Operation>(new Processor1D.Operation[]
                    {
                        new Processor1D.Operation
                        {
                            slice = step2,
                            minRange = 0.0f,
                            maxRange = 7.0f,
                            compare = 0.0f,
                            normalize = 1.0f,
                            scale = 1.0f,
                            offset = 0.0f
                        }
                    },
                    Allocator.Persistent);
            var vec2Ops =
                new NativeArray<Processor2D.Operation>(new Processor2D.Operation[]
                    {
                    },
                    Allocator.Persistent);
            var accOps =
                new NativeArray<Accumulate1D.Operation>(new Accumulate1D.Operation[]
                    {
                    },
                    Allocator.Persistent);

            var pipeline = new InputPipeline(enum2int, vec2mag, vec3mag, floatOps, vec2Ops, accOps, dataset);

            pipeline.Run(null, null);

            var pz = dataset.GetValues(step2);
            var sum = 0.0f;
            for (var i = 0; i < dataset.lengths[step2.lengthIndex]; ++i)
            {
                sum += pz[i];
            }
            //Debug.Log($"{sum}");

            outputvar = sum;

            dataset.lengths.Dispose();
            dataset.timestamps.Dispose();
            dataset.values.Dispose();
            dataset.enumLUT.Dispose();
            dataset.enumValues.Dispose();
            enum2int.Dispose();
            vec2mag.Dispose();
            vec3mag.Dispose();
            floatOps.Dispose();
            vec2Ops.Dispose();
            accOps.Dispose();

            Profiler.EndSample();

            return;

            Profiler.BeginSample("Core.NativeUpdate");

            // it could be a case, that we get a callback before anything is set at all
            if (!IsInitialized)
            {
                NativeSetup();
                IsInitialized = true;
            }

            //Graph.DropOldStates(Graph.MinTimestampAtCurrentUpdate); // min timestamp from last update 

            long? minTimestamp = null;
            long? maxTimestamp = null;

            // go over all the events
            for (long offset = 0; offset < buffer->sizeInBytes;)
            {
                var afterOffset = (byte*) buffer->eventBuffer + offset;
                var afterInputEvent = afterOffset + sizeof(NativeInputEvent);

                var inputEvent = (NativeInputEvent*) afterOffset;
                var deviceId = (int) inputEvent->deviceId;

                offset += inputEvent->sizeInBytes;

                //Debug.Log($"got {((InputEvent*) inputEvent)->type.ToString()} at {offset} with size {inputEvent->sizeInBytes}");

                var timestamp = TimestampHelper.ConvertToLong(inputEvent->time);

                minTimestamp = minTimestamp.HasValue
                    ? timestamp < minTimestamp.Value ? timestamp : minTimestamp.Value
                    : timestamp;
                maxTimestamp = maxTimestamp.HasValue
                    ? timestamp > maxTimestamp.Value ? timestamp : maxTimestamp.Value
                    : timestamp;

                if (deviceId >= Devices.Length || !Devices[deviceId].IsInitialized(deviceId)
                ) // unknown or uninitialized device
                    continue;

                switch (inputEvent->type)
                {
                    case NativeInputEventType.DeviceRemoved:
#if false && UNITY_EDITOR
                        SurviveDomainReload.Remove(inputEvent->deviceId);
#endif
                        Devices[deviceId].Clear();
                        // TODO notification mechanism
                        break;
                    case NativeInputEventType.DeviceConfigChanged:
                        break;
                    case NativeInputEventType.Text:
                        break;
                    // Demux states into the graph 
                    case NativeInputEventType.State:
                        var stateEvent = (NativeStateEvent*) afterInputEvent;
                        var afterStateEvent = afterInputEvent + sizeof(NativeStateEvent);

                        // calculate all changed bits since last device state change 
                        //var changedBits = Devices[deviceId].PreDemux(inputEvent->deviceId, afterStateEvent,
                        //    inputEvent->sizeInBytes - sizeof(NativeInputEvent) - sizeof(NativeStateEvent));

                        switch (stateEvent->Type)
                        {
                            case NativeStateEventType.Mouse:
                                //MouseDemux.Demux(ref Graph, deviceId, timestamp, changedBits, afterStateEvent);

                                // m_TestPreDemuxer.PreDemux(afterStateEvent, 30);
                                // ProgrammableDemuxer.Demux(m_TestPreDemuxer.GetState(),
                                //     m_TestPreDemuxer.GetChangedBitsBitMask(), m_TestPreDemuxer.GetLength(),
                                //     m_TestDemuxerConfig);

                                break;
                            case NativeStateEventType.Keyboard:
                                break;
                            case NativeStateEventType.Pen:
                                break;
                            case NativeStateEventType.Touch:
                                break;
                            case NativeStateEventType.Touchscreen:
                                break;
                            case NativeStateEventType.Tracking:
                                break;
                            case NativeStateEventType.Gamepad:
                                break;
                            case NativeStateEventType.HID:
                                break;
                            case NativeStateEventType.Accelerometer:
                                break;
                            case NativeStateEventType.Gyroscope:
                                break;
                            case NativeStateEventType.Gravity:
                                break;
                            case NativeStateEventType.Attitude:
                                break;
                            case NativeStateEventType.LinearAcceleration:
                                break;
                            case NativeStateEventType.LinuxJoystick:
                                break;
                            default:
                                // TODO user custom devices
                                break;
                        }

                        break;
                    case NativeInputEventType.Delta:
                        break;
                    default:
                        // ignoring unknown event
                        break;
                }
            }

            /*

            // this is very sketchy at the moment, needs proper frame cursors instead
            // we need "cursor ahead of this one" abstraction here, not just adding 1ns blindly
            if (minTimestamp.HasValue)
            {
                if (Graph.NoUpdatesLastFrame)
                {
                    // roll back by 1 nanosecond
                    Graph.MinTimestampAtCurrentUpdate--;
                    Graph.MaxTimestampAtCurrentUpdate = Graph.MinTimestampAtCurrentUpdate;
                    Graph.NoUpdatesLastFrame = false;
                }

                if (minTimestamp.Value < Graph.MaxTimestampAtCurrentUpdate)
                {
                    var diff = Math.Abs(minTimestamp.Value - Graph.MaxTimestampAtCurrentUpdate);
                    Debug.LogError(
                        $"unstable input frame boundary clock {Graph.MaxTimestampAtCurrentUpdate} -> {minTimestamp.Value} diff {TimestampHelper.ConvertToSeconds(diff) * 1000000.0} us");
                }

                Graph.MinTimestampAtCurrentUpdate = minTimestamp.Value;
                Graph.MaxTimestampAtCurrentUpdate = maxTimestamp.Value;
            }
            else if (!Graph.NoUpdatesLastFrame)
            {
                // no input events, just bump frame boundaries, but don't forget to roll back the interval when we get the events
                Graph.MinTimestampAtCurrentUpdate = Graph.MaxTimestampAtCurrentUpdate + 1;
                Graph.MaxTimestampAtCurrentUpdate = Graph.MinTimestampAtCurrentUpdate;
                Graph.NoUpdatesLastFrame = true;
                //Debug.Log("no events!");
            }

            Graph.Compute();

            if (Graph.DebugMouseLeftWasPressedThisFrame())
                Debug.Log("was pressed");
            if (Graph.DebugMouseLeftWasReleasedThisFrame())
                Debug.Log("was released");
                

            DebuggerWindow.RefreshCurrent();
                */

            Profiler.EndSample();
        }

        internal static unsafe void NativeDeviceDiscovered(int deviceId, string deviceDescriptorJson)
        {
#if false && UNITY_EDITOR
            SurviveDomainReload.Preserve(deviceId, deviceDescriptorJson);
#endif
            // TODO disable me
            return;

            var deviceDescriptor = JsonUtility.FromJson<NativeDeviceDescriptor>(deviceDescriptorJson);
            Debug.Log($"DRND: device discovered {deviceId} -> {deviceDescriptorJson}");

            while (Devices.Length <= deviceId)
                Array.Resize(ref Devices, Devices.Length + 1024);

            switch (deviceDescriptor.type)
            {
                case "Mouse":
                    Devices[deviceId].Setup(deviceId, sizeof(NativeMouseState));
                    Graph.DeviceChannelOffsets[deviceId] =
                        0; // HACK, currently we just expect mouse to be first in the graph

                    break;
                case "Keyboard":
                    Devices[deviceId].Setup(deviceId, sizeof(NativeKeyboardState));
                    break;
                default:
                    // ignoring device
                    break;
            }
        }
    }
}

/*
[BurstCompile(CompileSynchronously = true)]
private struct TestJob : IJob
{
    public NativeArray<ulong> LastState;
    [ReadOnly] public NativeArray<ulong> CurrentState;
    [ReadOnly] public NativeArray<ulong> EnabledBits;
    public bool GotFirstEvent;
    [WriteOnly] public NativeArray<ulong> ChangedBits;

    public unsafe void Execute()
    {
        if (!GotFirstEvent)
        {
            // invert all bits so all of them are in changed mask first time
            for (var i = 0; i < CurrentState.Length; ++i)
                LastState[i] = ~CurrentState[i];
            GotFirstEvent = true;
        }

        // calculate change mask
        for (var i = 0; i < CurrentState.Length; ++i)
            ChangedBits[i] = (LastState[i] ^ CurrentState[i]) & EnabledBits[i];

        // copy to last state
        // TODO change to front-back buffers
        UnsafeUtility.MemCpy(LastState.GetUnsafePtr(), CurrentState.GetUnsafeReadOnlyPtr(),
            LastState.Length * 8);
    }
}*/