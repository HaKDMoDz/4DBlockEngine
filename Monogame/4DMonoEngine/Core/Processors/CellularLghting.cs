using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Chunks;
using _4DMonoEngine.Core.Common.AbstractClasses;
using _4DMonoEngine.Core.Common.Interfaces;
using _4DMonoEngine.Core.Utils;
using _4DMonoEngine.Core.Utils.Vector;

namespace _4DMonoEngine.Core.Processors
{
    internal class CellularLighting<T> where T : ILightable
    {
        public delegate VertexBuilderTarget GetTarget(int x, int y, int z);
        private const byte MaxSun = 255;
        private const byte MinLight = 17;
        private const float SDropoff = 0.8f;
        private readonly Queue<LightQueueContainer>[] m_lightQueuesToAdd;
        private readonly Queue<LightQueueContainer>[] m_lightQueuesToRemove;
        private readonly T[] m_blockSource;
        private readonly MappingFunction m_mappingFunction;
        private readonly int m_chunkSize;
        private readonly Dictionary<int, float>[] m_queuedValuesByChannel;
        private readonly GetTarget m_getTarget;
        private readonly Channel[] m_channels;

        private enum Channel
        {
            Sun = 0,
            Red,
            Green,
            Blue,
            Count
        }

        internal CellularLighting(T[] blockSource, MappingFunction mappingFunction, int chunkSize, GetTarget getTarget)
        {
            m_blockSource = blockSource;
            m_mappingFunction = mappingFunction;
            m_chunkSize = chunkSize;
            const int count = (int) Channel.Count;
            m_channels = new []{Channel.Sun, Channel.Red, Channel.Blue, Channel.Green};
            m_lightQueuesToAdd = new Queue<LightQueueContainer>[count];
            m_lightQueuesToRemove = new Queue<LightQueueContainer>[count];
            m_queuedValuesByChannel = new Dictionary<int, float>[count];
            for (var i = 0; i < count; i++)
            {
                m_lightQueuesToAdd[i] = new Queue<LightQueueContainer>();
                m_lightQueuesToRemove[i] = new Queue<LightQueueContainer>();
                m_queuedValuesByChannel[i] = new Dictionary<int, float>();
            }
            m_getTarget = getTarget;
        }

        private void SetChannel(int blockIndex, Channel channel, byte value)
        {
            switch (channel)
            {
                case Channel.Sun:
                    m_blockSource[blockIndex].LightSun = value;
                    break;
                case Channel.Red:
                    m_blockSource[blockIndex].LightRed = value;
                    break;
                case Channel.Green:
                    m_blockSource[blockIndex].LightGreen = value;
                    break;
                case Channel.Blue:
                    m_blockSource[blockIndex].LightBlue = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("channel");
            }
        }

        private byte GetChannel(int blockIndex, Channel channel)
        {
            switch (channel)
            {
                case Channel.Sun:
                    return m_blockSource[blockIndex].LightSun;
                case Channel.Red:
                    return m_blockSource[blockIndex].LightRed;
                case Channel.Green:
                    return m_blockSource[blockIndex].LightGreen;
                case Channel.Blue:
                    return m_blockSource[blockIndex].LightBlue;
                default:
                    throw new ArgumentOutOfRangeException("channel");
            }
        }

        private byte ResetChannel(int blockIndex, Channel channel)
        {
            byte oldValue;
            switch (channel)
            {
                case Channel.Sun:
                    oldValue = m_blockSource[blockIndex].LightSun;
                    m_blockSource[blockIndex].LightSun = 0;
                    return oldValue;
                case Channel.Red:
                    oldValue = m_blockSource[blockIndex].LightRed;
                    m_blockSource[blockIndex].LightRed = 0;
                    return oldValue;
                case Channel.Green:
                    oldValue = m_blockSource[blockIndex].LightGreen;
                    m_blockSource[blockIndex].LightGreen = 0;
                    return oldValue;
                case Channel.Blue:
                    oldValue = m_blockSource[blockIndex].LightBlue;
                    m_blockSource[blockIndex].LightBlue = 0;
                    return oldValue;
                default:
                    throw new ArgumentOutOfRangeException("channel");
            }
        }

        private Queue<LightQueueContainer> GetAddQueueForChannel(Channel channel)
        {
            return m_lightQueuesToAdd[(int)channel];
        }

        private Queue<LightQueueContainer> GetRemoveQueueForChannel(Channel channel)
        {
            return m_lightQueuesToRemove[(int)channel];
        }

        private void SetQueuedLightLevel(Channel channel, int index, float value)
        {
            m_queuedValuesByChannel[(int) channel][index] = value;
        }

        private float GetQueuedLightLevel(Channel channel, int index)
        {
            var dict = m_queuedValuesByChannel[(int)channel];
            return dict.ContainsKey(index) ? dict[index] : 0;
        }

        private void RemoveQueuedLightLevel(Channel channel, int index)
        {
            var dict = m_queuedValuesByChannel[(int)channel];
            dict.Remove(index);
        }

        internal void Process(int chunkIndexX, int chunkIndexY, int chunkIndexZ, SparseArray3D<Vector3Byte> lights)
        {
            GetLightsOnShell(chunkIndexX, chunkIndexY, chunkIndexZ,lights);
            PropogateFromSources();
        }

        private void GetLightsOnShell(int chunkIndexX, int chunkIndexY, int chunkIndexZ, SparseArray3D<Vector3Byte> lights)
        {
            for (var x = 0; x < m_chunkSize; ++x)
            {
                for (var z = 0; z < m_chunkSize; ++z)
                {
                    for (var y = m_chunkSize - 1; y >= 0 ; --y)
                    {
                        var cX = chunkIndexX + x;
                        var cY = chunkIndexY + y;
                        var cZ = chunkIndexZ + z;
                       // if (x == 0 || y == 0 || z == 0 || x == m_chunkSize -1  || y == m_chunkSize - 1 || z == m_chunkSize -1)
                     //   {
                            ConditionallyInsertLight(cX, cY, cZ);
                      //  }
                      //  else
                     //   {
                            if (lights.ContainsKey(cX, cY, cZ))
                            {
                                PropogateFromLight(cX, cY, cZ, lights[cX, cY, cZ]);
                            }
                      //  }
                    }
                }
            }
        }

        private void ConditionallyInsertLight(int x, int y, int z)
        {
            var currentLight = new Vector4();
            var block = m_blockSource[m_mappingFunction(x, y, z)];
            GetMaxLight(x, y, z, ref currentLight);
            if (currentLight.X > block.LightRed)
            {
                PropogateFromLight(x, y, z, Channel.Red, (byte) currentLight.X);
            }
            if (currentLight.Y > block.LightGreen)
            {
                PropogateFromLight(x, y, z, Channel.Green, (byte) currentLight.Y);
            }
            if (currentLight.Z > block.LightBlue)
            {
                PropogateFromLight(x, y, z, Channel.Blue, (byte) currentLight.Z);
            }
            if (currentLight.W <= block.LightSun)
            {
                return;
            }
            var down = m_blockSource[m_mappingFunction(x, y + 1, z)].LightSun >= (byte) currentLight.W;
            PropogateFromLight(x, y, z, Channel.Sun, (byte) currentLight.W, down);
        }

        public void AddBlock(int x, int y, int z)
        {
            ClearAllChannelsFromCell(x, y, z);
        }

        public void RemoveBlock(int x, int y, int z)
        {
            var currentLight = new Vector4();
            GetMaxLight(x, y, z, ref currentLight);
            PropogateFromLight(x, y, z, Channel.Red, (byte)currentLight.X);
            PropogateFromLight(x, y, z, Channel.Green, (byte)currentLight.Y);
            PropogateFromLight(x, y, z, Channel.Blue, (byte)currentLight.Z);
            var down = m_blockSource[m_mappingFunction(x, y + 1, z)].LightSun >= (byte)currentLight.W;
            PropogateFromLight(x, y, z, Channel.Sun, (byte)currentLight.W, down);
        }

        private void GetMaxLight(int x, int y, int z, ref Vector4 currentLight)
        {
            MaxLight(x + 1, y, z, ref currentLight);
            MaxLight(x - 1, y, z, ref currentLight);
            MaxLight(x, y, z + 1, ref currentLight);
            MaxLight(x, y, z - 1, ref currentLight);
            MaxLight(x, y + 1, z, ref currentLight);
            MaxLight(x, y - 1, z, ref currentLight);
        }

        private void MaxLight(int x, int y, int z, ref Vector4 currentLight)
        {
            var blockIndex = m_mappingFunction(x, y, z);
            var lightRed = m_blockSource[blockIndex].LightRed;
            var lightGreen = m_blockSource[blockIndex].LightGreen;
            var lightBlue = m_blockSource[blockIndex].LightBlue;
            var lightSun = m_blockSource[blockIndex].LightSun;
            if (currentLight.X < lightRed)
            {
                currentLight.X = lightRed;
            }
            if (currentLight.Y < lightGreen)
            {
                currentLight.Y = lightGreen;
            }
            if (currentLight.Z < lightBlue)
            {
                currentLight.Z = lightBlue;
            }
            if (currentLight.W < lightSun)
            {
                currentLight.W = lightSun;
            }
        }

        public void AddLight(SparseArray3D<Vector3Byte> lights, int x, int y, int z, Vector3Byte light)
        {
            if (lights.ContainsKey(x, y, z))
            {
                return;
            }
            lights[x, y, z] = light;
            var blockIndex = m_mappingFunction( x, y, z);
            if (light.X > m_blockSource[blockIndex].LightRed)
            {
                PropogateFromLight(x, y, z, Channel.Red, light.X);
            }
            if (light.Y > m_blockSource[blockIndex].LightGreen)
            {
                PropogateFromLight(x, y, z, Channel.Green, light.Y);
            }
            if (light.Z > m_blockSource[blockIndex].LightBlue)
            {
                PropogateFromLight(x, y, z, Channel.Blue, light.Z);
            }
            PropogateFromSources();
        }

        public void RemoveLight(SparseArray3D<Vector3Byte> lights, int x, int y, int z)
        {
            if (!lights.ContainsKey(x, y, z))
            {
                return;
            }
            lights.Remove(x, y, z);
            ClearLightChannelsFromCell(x, y, z);
            PropogateFromSources();
        }

        private void ClearAllChannelsFromCell(int x, int y, int z)
        {
            var blockIndex = m_mappingFunction(x, y, z);
            foreach (var channel in m_channels)
            {
                ClearChannelFromCell(x, y, z, blockIndex, channel);
            }
        }

        private void ClearLightChannelsFromCell(int x, int y, int z)
        {
            var blockIndex = m_mappingFunction(x, y, z);
            ClearChannelFromCell(x, y, z, blockIndex, Channel.Red);
            ClearChannelFromCell(x, y, z, blockIndex, Channel.Blue);
            ClearChannelFromCell(x, y, z, blockIndex, Channel.Green);
        }

        private void ClearChannelFromCell(int x, int y, int z, int blockIndex, Channel channel)
        {
            var incomingSunLight = ResetChannel(blockIndex, channel);
            var container = new LightQueueContainer(x, y, z, incomingSunLight);
            GetRemoveQueueForChannel(channel).Enqueue(container);
            ClearCellsAndAddBoundries(channel);
        }

        private void ClearCellsAndAddBoundries(Channel channel)
        {
            var lightQueueToRemove = GetRemoveQueueForChannel(channel);
            while (lightQueueToRemove.Count > 0)
            {
                var container = lightQueueToRemove.Dequeue();
                var x = container.X;
                var y = container.Y;
                var z = container.Z;
                var target = m_getTarget(x, y, z);
                if (target == null)
                {
                    continue;
                }
                target.SetDirty(x, y, z);
                var blockIndex = m_mappingFunction(x, y, z);
                var propogate = false;
                var isSource = false;
                var light = container.Light;
                var opacity = m_blockSource[blockIndex].Opacity;
                if (opacity >= 1)
                {
                    SetChannel(blockIndex, channel, 0);
                    continue;
                }
                if (!container.PropogateDown || light < MaxSun || opacity > 0)
                {
                    light = light * (1 - opacity) * SDropoff;
                }
                if (light > m_blockSource[blockIndex].LightSun || (container.PropogateDown && light >= Chunk.MaxSunValue))
                {
                    propogate = true;
                    SetChannel(blockIndex, channel, 0);
                }
                else
                {
                    isSource = true;
                }

                if (propogate)
                {
                    lightQueueToRemove.Enqueue(new LightQueueContainer(x + 1, y, z, light));
                    lightQueueToRemove.Enqueue(new LightQueueContainer(x - 1, y, z, light));
                    lightQueueToRemove.Enqueue(new LightQueueContainer(x, y, z + 1, light));
                    lightQueueToRemove.Enqueue(new LightQueueContainer(x, y, z - 1, light));
                    lightQueueToRemove.Enqueue(new LightQueueContainer(x, y + 1, z, light));
                    lightQueueToRemove.Enqueue(new LightQueueContainer(x, y - 1, z, light, true));
                }
                if (isSource)
                {
                    EnqueueLight(new LightQueueContainer(x, y, z, GetChannel(blockIndex, channel)), channel, blockIndex);
                }
            }
        }

        private void PropogateFromLight(int x, int y, int z, Vector3Byte currentLight)
        {
            PropogateFromLight(x, y, z, Channel.Red, currentLight.X);
            PropogateFromLight(x, y, z, Channel.Blue, currentLight.Y);
            PropogateFromLight(x, y, z, Channel.Green, currentLight.Z);
        }

        private void PropogateFromLight(int x, int y, int z, Channel channel, byte incomingLight, bool down = false)
        {
            var container = new LightQueueContainer(x, y, z, incomingLight, down);
            EnqueueLight(container, channel, m_mappingFunction(x, y, z));
        }

        private void EnqueueLight(LightQueueContainer container, Channel channel, int blockIndex)
        {
            GetAddQueueForChannel(channel).Enqueue(container);
            SetQueuedLightLevel(channel, blockIndex, container.Light);
        }

        private void PropogateFromSources()
        {
            PropogateFromSource(Channel.Sun);
            PropogateFromSource(Channel.Red);
            PropogateFromSource(Channel.Green);
            PropogateFromSource(Channel.Blue);
        }

        private void PropogateFromSource(Channel channel)
        {
            var sources = GetAddQueueForChannel(channel);
            while (sources.Count > 0)
            {
                var container = sources.Dequeue();
                var x = container.X;
                var y = container.Y;
                var z = container.Z;
                var blockIndex = m_mappingFunction(x, y, z);
                var target = m_getTarget(x, y, z);
                if (target == null) //we wrapped around!
                {
                    continue;
                }
                target.SetDirty(x, y, z);
                RemoveQueuedLightLevel(channel, blockIndex);
                var opacity = m_blockSource[blockIndex].Opacity;
                if (opacity >= 1)
                {
                    SetChannel(blockIndex, channel, 0);
                    continue;
                }
                var light = container.Light;
                var currentLight = GetChannel(blockIndex, channel);
                if (!container.PropogateDown || light < MaxSun || opacity > 0)
                {
                    light = light * (1 - opacity) * SDropoff;
                }
                if (light <= currentLight)
                {
                    continue;
                }
                if (light <= MinLight)
                {
                    SetChannel(blockIndex, channel, MinLight);
                    continue;
                }
                SetChannel(blockIndex, channel, (byte)light);
                ConditionallyPropogate(x + 1, y, z, channel, light);
                ConditionallyPropogate(x - 1, y, z, channel, light);
                ConditionallyPropogate(x, y, z + 1, channel, light);
                ConditionallyPropogate(x, y, z - 1, channel, light);
                ConditionallyPropogate(x, y + 1, z, channel, light);
                ConditionallyPropogate(x, y - 1, z, channel, light, channel == Channel.Sun);
            }
        }

        private void ConditionallyPropogate(int x, int y, int z, Channel channel, float incomingLight, bool lightDown = false)
        {
            var blockIndex = m_mappingFunction(x, y, z);
            var opacity = m_blockSource[blockIndex].Opacity;
            if (opacity >= 1)
            {
                return;
            }
            var testLight = incomingLight;
            if (!lightDown || incomingLight < MaxSun || opacity > 0)
            {
                testLight = incomingLight * (1 - opacity) * SDropoff;
            }
            if (GetChannel(blockIndex, channel) > testLight || GetQueuedLightLevel(channel, blockIndex) > testLight)
            {
                return;
            }
            EnqueueLight(new LightQueueContainer(x, y, z, incomingLight, lightDown), channel, blockIndex);
        }

        private struct LightQueueContainer
        {
            public readonly int X;
            public readonly int Y;
            public readonly int Z;
            public readonly float Light;
            public readonly bool PropogateDown;
            public LightQueueContainer(int x, int y, int z, float light, bool propogateDown = false)
            {
                X = x;
                Y = y;
                Z = z;
                Light = light;
                PropogateDown = propogateDown;
            }
        }
    }
}
