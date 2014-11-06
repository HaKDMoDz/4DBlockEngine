using System;
using System.Collections.Generic;
using _4DMonoEngine.Core.Common.AbstractClasses;
using _4DMonoEngine.Core.Common.Interfaces;
using _4DMonoEngine.Core.Utils;
using _4DMonoEngine.Core.Utils.Vector;

namespace _4DMonoEngine.Core.Processors
{
    internal class CellularLighting<T> where T : ILightable
    {
        public delegate VertexBuilderTarget GetTarget(int x, int y, int z);
        public const byte MaxSun = 255;
        public const byte MinLight = 17;
        private const float SDropoff = 0.84f;
        private readonly Queue<LightQueueContainer>[] m_lightQueuesToAdd;
        private readonly Queue<LightQueueContainer>[] m_lightQueuesToRemove;
        private readonly T[] m_blockSource;
        private readonly MappingFunction m_mappingFunction;
        private readonly int m_chunkSize;
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
            for (var i = 0; i < count; i++)
            {
                m_lightQueuesToAdd[i] = new Queue<LightQueueContainer>();
                m_lightQueuesToRemove[i] = new Queue<LightQueueContainer>();
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

        private Queue<LightQueueContainer> GetAddQueueForChannel(Channel channel)
        {
            return m_lightQueuesToAdd[(int)channel];
        }

        private Queue<LightQueueContainer> GetRemoveQueueForChannel(Channel channel)
        {
            return m_lightQueuesToRemove[(int)channel];
        }

        internal void Process(int chunkIndexX, int chunkIndexY, int chunkIndexZ)
        {
            CalculateInitialLighting(chunkIndexX, chunkIndexY, chunkIndexZ);
            PropogateFromSources(m_getTarget(chunkIndexX, chunkIndexY, chunkIndexZ));
        }

        private void CalculateInitialLighting(int chunkIndexX, int chunkIndexY, int chunkIndexZ)
        {
            for (var x = 0; x < m_chunkSize; ++x)
            {
                for (var z = 0; z < m_chunkSize; ++z)
                {
                    for (var y = m_chunkSize - 1; y >= 0; --y)
                    {
                        var cX = chunkIndexX + x;
                        var cY = chunkIndexY + y;
                        var cZ = chunkIndexZ + z;
                        if (x == 0 || x == m_chunkSize - 1 ||
                            y == 0 || y == m_chunkSize - 1 ||
                            z == 0 || z == m_chunkSize - 1)
                        {
                            CalculateLightOnShell(cX, cY, cZ);
                        }
                        else
                        {
                            ConditionallyInsertLight(cX, cY, cZ);
                        }
                    }
                }
            }
        }

        private void CalculateLightOnShell(int x, int y, int z)
        {
            var neighborLightSun = GetMaxNeighborLightForChannel(x, y, z, Channel.Sun);
            var neighborLightRed =  GetMaxNeighborLightForChannel(x, y, z, Channel.Red);
            var neighborLightGreen = GetMaxNeighborLightForChannel(x, y, z, Channel.Green);
            var neighborLightBlue = GetMaxNeighborLightForChannel(x, y, z, Channel.Blue);
            var block = m_blockSource[m_mappingFunction(x, y, z)];
            if ((byte)neighborLightRed > block.LightRed)
            {
                PropogateFromLight(x, y, z, Channel.Red, neighborLightRed);
            }
            if ((byte)neighborLightGreen > block.LightGreen)
            {
                PropogateFromLight(x, y, z, Channel.Green, neighborLightGreen);
            }
            if ((byte)neighborLightBlue > block.LightBlue)
            {
                PropogateFromLight(x, y, z, Channel.Blue, neighborLightBlue);
            }
            if ((byte)neighborLightSun > block.LightSun)
            {
                PropogateFromLight(x, y, z, Channel.Sun, neighborLightSun);
            }
        }

        private void ConditionallyInsertLight(int x, int y, int z)
        {
            var blockIndex = m_mappingFunction(x, y, z);
            var block = m_blockSource[blockIndex];
            if (block.LightRed > MinLight)
            {
                ClearCellAndEnqueLight(x, y, z, blockIndex, Channel.Red, block.LightRed);
            }
            if (block.LightGreen > MinLight)
            {
                ClearCellAndEnqueLight(x, y, z, blockIndex, Channel.Green, block.LightGreen);
            }
            if (block.LightBlue > MinLight)
            {
                ClearCellAndEnqueLight(x, y, z, blockIndex, Channel.Blue, block.LightBlue);
            }
            if (block.LightSun > MinLight)
            {
                ClearCellAndEnqueLight(x, y, z, blockIndex, Channel.Sun, block.LightSun);
            }
        }

        private float GetMaxNeighborLightForChannel(int x, int y, int z, Channel channel)
        {
            var blockIndex = m_mappingFunction(x + 1, y, z);
            var pX = CalculateDropOff(GetChannel(blockIndex, channel), blockIndex);
            blockIndex = m_mappingFunction(x - 1, y, z);
            var nX = CalculateDropOff(GetChannel(blockIndex, channel), blockIndex);
            var maxX = pX > nX ? pX : nX;
            blockIndex = m_mappingFunction(x, y, z + 1);
            var pZ = CalculateDropOff(GetChannel(blockIndex, channel), blockIndex);
            blockIndex = m_mappingFunction(x, y, z - 1);
            var nZ = CalculateDropOff(GetChannel(blockIndex, channel), blockIndex);
            var maxZ = pZ > nZ ? pZ : nZ;
            blockIndex = m_mappingFunction(x, y + 1, z);
            var pY = CalculateDropOff(GetChannel(blockIndex, channel), blockIndex, channel == Channel.Sun);
            blockIndex = m_mappingFunction(x, y - 1, z);
            var nY = CalculateDropOff(GetChannel(blockIndex, channel), blockIndex);
            var maxY = pY > nY ? pY : nY;
            var max = maxX > maxZ ? maxX : maxZ;
            return max > maxY ? max : maxY;
        }

        private float CalculateDropOff(float light, int blockIndex, bool down = false)
        {
            var opacity = m_blockSource[blockIndex].Opacity;
            if (!down || light < MaxSun || opacity > 0)
            {
                light = light * (1 - opacity) * SDropoff;
            }
            return light;
        }

        public void AddBlock(int x, int y, int z, T source)
        {
            ClearAllChannelsFromCell(x, y, z);
            if (source.EmissivityRed > MinLight)
            {
                PropogateFromLight(x, y, z, Channel.Red, source.EmissivityRed);
            }
            if (source.EmissivityGreen > MinLight)
            {
                PropogateFromLight(x, y, z, Channel.Green, source.EmissivityGreen);
            }
            if (source.EmissivityBlue > MinLight)
            {
                PropogateFromLight(x, y, z, Channel.Blue, source.EmissivityBlue);
            }
            if (source.EmissivitySun > MinLight)
            {
                PropogateFromLight(x, y, z, Channel.Sun, source.EmissivitySun);
            }
            PropogateFromSources(m_getTarget(x, y, z));
        }

        public void RemoveBlock(int x, int y, int z)
        {
            var blockIndex = m_mappingFunction(x, y, z);
            var source = m_blockSource[blockIndex];
            if (source.EmissivityRed > MinLight)
            {
                ClearChannelFromCell(x, y, z, blockIndex, Channel.Red);
            }
            else
            {
                var neighborLightRed = GetMaxNeighborLightForChannel(x, y, z, Channel.Red);
                PropogateFromLight(x, y, z, Channel.Red, neighborLightRed);
            }
            if (source.EmissivityGreen > MinLight)
            {
                ClearChannelFromCell(x, y, z, blockIndex, Channel.Green);
            }
            else
            {
                var neighborLightGreen = GetMaxNeighborLightForChannel(x, y, z, Channel.Green);
                PropogateFromLight(x, y, z, Channel.Green, neighborLightGreen);
            }
            if (source.EmissivityBlue > MinLight)
            {
                ClearChannelFromCell(x, y, z, blockIndex, Channel.Blue);
            }
            else
            {
                var neighborLightBlue = GetMaxNeighborLightForChannel(x, y, z, Channel.Blue);
                PropogateFromLight(x, y, z, Channel.Blue, neighborLightBlue);
            }
            if (source.EmissivitySun > MinLight)
            {
                ClearChannelFromCell(x, y, z, blockIndex, Channel.Sun);
            }
            else
            {
                var neighborLightSun = GetMaxNeighborLightForChannel(x, y, z, Channel.Sun);
                PropogateFromLight(x, y, z, Channel.Sun, neighborLightSun);
            }
        }

        private void ClearAllChannelsFromCell(int x, int y, int z)
        {
            var blockIndex = m_mappingFunction(x, y, z);
            foreach (var channel in m_channels)
            {
                ClearChannelFromCell(x, y, z, blockIndex, channel);
            }
        }

        private void ClearChannelFromCell(int x, int y, int z, int blockIndex, Channel channel)
        {
            var incomingSunLight = GetChannel(blockIndex, channel);
            GetRemoveQueueForChannel(channel).Enqueue(new LightQueueContainer(x, y, z, incomingSunLight));
            ClearCellsAndAddBoundries(m_getTarget(x, y, z), channel);
        }

        private void ClearCellsAndAddBoundries(VertexBuilderTarget sourceTarget, Channel channel)
        {
            var lightQueueToRemove = GetRemoveQueueForChannel(channel);
            while (lightQueueToRemove.Count > 0)
            {
                var container = lightQueueToRemove.Dequeue();
                var x = container.X;
                var y = container.Y;
                var z = container.Z;
                var blockIndex = m_mappingFunction(x, y, z);
                var light = container.Light;
                var currentLight = GetChannel(blockIndex, channel);
                if ((byte) light < currentLight)
				{
                    continue;
                }
                SetChannel(blockIndex, channel, channel == Channel.Sun ? MinLight : (byte)0);
                light = currentLight;
                if ((byte)light < MinLight)
                {
                    continue;
                }
                ConditionallyClear(sourceTarget, x + 1, y, z, channel, light);
                ConditionallyClear(sourceTarget, x - 1, y, z, channel, light);
                ConditionallyClear(sourceTarget, x, y, z + 1, channel, light);
                ConditionallyClear(sourceTarget, x, y, z - 1, channel, light);
                ConditionallyClear(sourceTarget, x, y + 1, z, channel, light);
                ConditionallyClear(sourceTarget, x, y - 1, z, channel, light, channel == Channel.Sun);
            }
            PropogateFromSources(sourceTarget);
        }

        private void ConditionallyClear(VertexBuilderTarget sourceTarget, int x, int y, int z, Channel channel, float incomingLight, bool lightDown = false)
        {
            var blockIndex = m_mappingFunction(x, y, z);
            var currentLight = GetChannel(blockIndex, channel);
            if ((byte)incomingLight >= currentLight || (lightDown && incomingLight >= MaxSun))
            {
                var target = m_getTarget(x, y, z);
                if (target == null) //we wrapped around!
                {
                    return;
                }
                if (target != sourceTarget)
                {
                    target.SetMeshDirty(x, y, z);
                }
                var opacity = m_blockSource[blockIndex].Opacity;
                if (opacity >= 1)
                {
                    return;
                }
                GetRemoveQueueForChannel(channel).Enqueue(new LightQueueContainer(x, y, z, incomingLight));
            }
            else
            {
                ClearCellAndEnqueLight(x, y, z, blockIndex, channel, currentLight);
            }
        }

        private void ClearCellAndEnqueLight(int x, int y, int z, int blockIndex, Channel channel, float incomingLight)
        {
            PropogateFromLight(x, y, z, channel, incomingLight);
             SetChannel(blockIndex, channel, 0);
        }

        private void PropogateFromLight(int x, int y, int z, Vector3Byte currentLight)
        {
            PropogateFromLight(x, y, z, Channel.Red, currentLight.X);
            PropogateFromLight(x, y, z, Channel.Blue, currentLight.Y);
            PropogateFromLight(x, y, z, Channel.Green, currentLight.Z);
        }

        private void PropogateFromLight(int x, int y, int z, Channel channel, float incomingLight)
        {
            GetAddQueueForChannel(channel).Enqueue(new LightQueueContainer(x, y, z, incomingLight));
        }

        private void PropogateFromSources(VertexBuilderTarget sourceTarget)
        {
            PropogateFromSource(sourceTarget, Channel.Sun);
            PropogateFromSource(sourceTarget, Channel.Red);
            PropogateFromSource(sourceTarget, Channel.Green);
            PropogateFromSource(sourceTarget, Channel.Blue);
        }

        private void PropogateFromSource(VertexBuilderTarget sourceTarget, Channel channel)
        {
            var sources = GetAddQueueForChannel(channel);
            while (sources.Count > 0)
            {
                var container = sources.Dequeue();
                var x = container.X;
                var y = container.Y;
                var z = container.Z;
                var blockIndex = m_mappingFunction(x, y, z);
                var light = container.Light;
                if ((byte)light <= GetChannel(blockIndex, channel))
                {
                    continue;
                }
                if ((byte)light <= MinLight)
                {
                    SetChannel(blockIndex, channel, MinLight);
                    continue;
                }
                SetChannel(blockIndex, channel, (byte)light);
                ConditionallyPropogate(sourceTarget, x + 1, y, z, channel, light);
                ConditionallyPropogate(sourceTarget, x - 1, y, z, channel, light);
                ConditionallyPropogate(sourceTarget, x, y, z + 1, channel, light);
                ConditionallyPropogate(sourceTarget, x, y, z - 1, channel, light);
                ConditionallyPropogate(sourceTarget, x, y + 1, z, channel, light);
                ConditionallyPropogate(sourceTarget, x, y - 1, z, channel, light, channel == Channel.Sun);
            }
        }


        private void ConditionallyPropogate(VertexBuilderTarget sourceTarget, int x, int y, int z, Channel channel, float incomingLight, bool lightDown = false)
        {
            var blockIndex = m_mappingFunction(x, y, z);
            if ((byte)incomingLight > GetChannel(blockIndex, channel))
            {
                incomingLight = CalculateDropOff(incomingLight, blockIndex, lightDown);
                var target = m_getTarget(x, y, z);
                if (target == null) //we wrapped around!
                {
                    return;
                }
                if (target != sourceTarget)
                {
                    target.SetMeshDirty(x, y, z);
                }
                var opacity = m_blockSource[blockIndex].Opacity;
                if (opacity >= 1)
                {
                    return;
                }
                PropogateFromLight(x, y, z, channel, incomingLight);
            }
        }

        private struct LightQueueContainer
        {
            public readonly int X;
            public readonly int Y;
            public readonly int Z;
            public readonly float Light;
            public LightQueueContainer(int x, int y, int z, float light)
            {
                X = x;
                Y = y;
                Z = z;
                Light = light;
            }
        }
    }
}
