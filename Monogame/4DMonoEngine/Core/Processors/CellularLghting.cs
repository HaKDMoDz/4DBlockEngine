using System.Collections.Generic;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Chunks;
using _4DMonoEngine.Core.Interfaces;
using _4DMonoEngine.Core.Structs.Vector;

namespace _4DMonoEngine.Core.Processors
{
    internal class CellularLighting<T> where T : ILightable
    {
        private const float SDropoff = 0.9375f;
        private readonly Queue<LightQueueContainer> m_lightQueueToAdd;
        private readonly Queue<SunQueueContainer> m_sunQueueToAdd;
        private readonly Queue<LightQueueContainer> m_lightQueueToRemove;
        private readonly Queue<SunQueueContainer> m_sunQueueToRemove;
        private readonly T[] m_blockSource;

        internal CellularLighting(T[] blockSource)
        {
            m_blockSource = blockSource;
            m_sunQueueToAdd = new Queue<SunQueueContainer>();
            m_lightQueueToAdd = new Queue<LightQueueContainer>();
            m_sunQueueToRemove = new Queue<SunQueueContainer>();
            m_lightQueueToRemove = new Queue<LightQueueContainer>();
        }

        //TODO : remove the dependencies on Chunk and ChunkCache
        internal void Process(Chunk chunk)
        {
            if (chunk.ChunkState == ChunkState.AwaitingLighting)
            {
                chunk.ChunkState = ChunkState.Lighting;
                ResetLight(chunk);
                PropogateFromSunSources();
                PropogateFromLights();
                chunk.ChunkState = ChunkState.AwaitingBuild;
            }
        }

        private void ResetLight(Chunk chunk)
        {
            for (byte x = 0; x < Chunk.SizeInBlocks; ++x)
            {
                for (byte z = 0; z < Chunk.SizeInBlocks; ++z)
                {
                    var offset = ChunkCache.BlockIndexByRelativePosition(chunk, x, 0, z);
                    for (var y = Chunk.SizeInBlocks - 1; y > 0; --y)
                    {
                        var blockIndex = offset + y;
                        if (y == Chunk.SizeInBlocks - 1)
                        {
                            if (m_blockSource[blockIndex].Opacity < 1)
                            {
                                PropagateFromSunSource(blockIndex, m_blockSource[blockIndex - 1].LightSun, down: true);
                            }
                        }
                        else
                        {
                            if (chunk.LightSources.ContainsKey(x, y, z))
                            {
                                PropogateFromLight(blockIndex, chunk.LightSources[x, y, z]);
                            }
                            m_blockSource[blockIndex].LightSun = 0;
                            m_blockSource[blockIndex].LightRed = 0;
                            m_blockSource[blockIndex].LightGreen = 0;
                            m_blockSource[blockIndex].LightBlue = 0;
                        }
                    }
                }
            }
        }

        public void AddBlock(Chunk chunk, int x, int y, int z)
        {
            AddBlock(ChunkCache.BlockIndexByRelativePosition(chunk, x, y, z));
        }

        public void AddBlock(int x, int y, int z)
        {
            AddBlock(ChunkCache.BlockIndexByWorldPosition(x, y, z));
        }

        public void AddBlock(int blockIndex)
        {
            ClearCellOrAddLight(blockIndex);
            ClearCellOrAddSunSource(blockIndex);
            
        }

        public void RemoveBlock(Chunk chunk, int x, int y, int z)
        {
            RemoveBlock(ChunkCache.BlockIndexByRelativePosition(chunk, x, y, z));
        }

        public void RemoveBlock(int x, int y, int z)
        {
            RemoveBlock(ChunkCache.BlockIndexByWorldPosition(x, y, z));
        }

        public void RemoveBlock(int blockIndex)
        {
            var currentLight = new Vector4();
            MaxLight(blockIndex + ChunkCache.BlockStepX, ref currentLight);
            MaxLight(blockIndex - ChunkCache.BlockStepX, ref currentLight); 
            MaxLight(blockIndex + ChunkCache.BlockStepZ, ref currentLight); 
            MaxLight(blockIndex - ChunkCache.BlockStepZ, ref currentLight);
            MaxLight(blockIndex + 1, ref currentLight);                
            MaxLight(blockIndex - 1, ref currentLight, true);
            PropogateFromLight(blockIndex, (byte)currentLight.X, (byte)currentLight.Y, (byte)currentLight.Z, immediate: true);
            PropagateFromSunSource(blockIndex, (byte)currentLight.W, down: true, immediate: true);
        }

        private void MaxLight(int blockIndex, ref Vector4 currentLight, bool propogateDown = false)
        {
            var opacity = m_blockSource[blockIndex].Opacity;
            var passThroughRate = (1 - opacity) * SDropoff;
            var lightRed = (byte)(m_blockSource[blockIndex].LightRed * passThroughRate);
            var lightGreen = (byte)(m_blockSource[blockIndex].LightGreen  * passThroughRate);
            var lightBlue = (byte)(m_blockSource[blockIndex].LightBlue * passThroughRate);
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
            var lightSun = m_blockSource[blockIndex].LightSun;
            if (!(propogateDown && lightSun == Chunk.MaxSunValue) || opacity > 0)
            {
                lightSun = (byte)(lightSun * (1 - opacity) * SDropoff);
            }
            if (currentLight.W < lightSun)
            {
                currentLight.W = lightSun;
            }
        }

        public void AddLight(Chunk chunk, int x, int y, int z, Vector3Byte light)
        {
            if (!chunk.LightSources.ContainsKey(x, y, z))
            {
                chunk.LightSources[x, y, z] = light;
                var blockIndex = ChunkCache.BlockIndexByRelativePosition(chunk, x, y, z);
                var propogate = false;
                var lightRed = m_blockSource[blockIndex].LightRed;
                var lightGreen = m_blockSource[blockIndex].LightGreen;
                var lightBlue = m_blockSource[blockIndex].LightBlue;
                if (light.X > lightRed)
                {
                    propogate = true;
                    lightRed = light.X;
                }
                if (light.Y > m_blockSource[blockIndex].LightGreen)
                {
                    propogate = true;
                    lightGreen = light.Y;
                }
                if (light.Z > m_blockSource[blockIndex].LightBlue)
                {
                    propogate = true;
                    lightBlue = light.Z;
                }
                if(propogate)
                {
                    PropogateFromLight(blockIndex, lightRed, lightGreen, lightBlue);
                }
            }
        }

        public void RemoveLight(Chunk chunk, int x, int y, int z)
        {
            if(chunk.LightSources.ContainsKey(x, y, z))
            {
                chunk.LightSources.Remove(x, y, z);
                var blockIndex = ChunkCache.BlockIndexByRelativePosition(chunk, x, y, z);
                ClearCellOrAddLight(blockIndex);
            }
        }

        private void ClearCellOrAddLight(int blockIndex)
        {
            var lightRed = m_blockSource[blockIndex].LightRed;
            m_blockSource[blockIndex].LightRed = 0;
            var lightGreen = m_blockSource[blockIndex].LightGreen;
            m_blockSource[blockIndex].LightGreen = 0;
            var lightBlue = m_blockSource[blockIndex].LightBlue;
            m_blockSource[blockIndex].LightBlue = 0;
            var container = new LightQueueContainer(blockIndex, lightRed, lightGreen, lightBlue);
            m_lightQueueToAdd.Enqueue(container);
            ClearCellOrAddLights();
        }

        private void ClearCellOrAddLights()
        {
            while (m_lightQueueToRemove.Count > 0)
            {
                var lightContainer = m_lightQueueToRemove.Dequeue();
                var propogate = false;
                var isSource = false;
                if (lightContainer.LightBlue > m_blockSource[lightContainer.BlockIndex].LightRed)
                {
                    propogate = true;
                    m_blockSource[lightContainer.BlockIndex].LightRed = 0;
                }
                else
                {
                    isSource = true;
                }
                if (lightContainer.LightBlue > m_blockSource[lightContainer.BlockIndex].LightGreen)
                {
                    propogate = true;
                    m_blockSource[lightContainer.BlockIndex].LightGreen = 0;
                }
                else
                {
                    isSource = true;
                }
                if (lightContainer.LightBlue > m_blockSource[lightContainer.BlockIndex].LightBlue)
                {
                    propogate = true;
                    m_blockSource[lightContainer.BlockIndex].LightBlue = 0;
                }
                else
                {
                    isSource = true;
                }

                if(propogate)
                {
                    var passThroughRate = (1 - m_blockSource[lightContainer.BlockIndex].Opacity) * SDropoff;
                    var lightRed = (byte)(lightContainer.LightRed > 0 ? lightContainer.LightRed * passThroughRate : 0);
                    var lightGreen = (byte)(lightContainer.LightGreen > 0 ? lightContainer.LightGreen * passThroughRate : 0);
                    var lightBlue = (byte)(lightContainer.LightBlue > 0 ? lightContainer.LightBlue * passThroughRate : 0);
                    m_lightQueueToRemove.Enqueue(new LightQueueContainer(lightContainer.BlockIndex + ChunkCache.BlockStepX, lightRed, lightGreen, lightBlue));
                    m_lightQueueToRemove.Enqueue(new LightQueueContainer(lightContainer.BlockIndex - ChunkCache.BlockStepX, lightRed, lightGreen, lightBlue));
                    m_lightQueueToRemove.Enqueue(new LightQueueContainer(lightContainer.BlockIndex + ChunkCache.BlockStepZ, lightRed, lightGreen, lightBlue));
                    m_lightQueueToRemove.Enqueue(new LightQueueContainer(lightContainer.BlockIndex - ChunkCache.BlockStepZ, lightRed, lightGreen, lightBlue));
                    m_lightQueueToRemove.Enqueue(new LightQueueContainer(lightContainer.BlockIndex + 1, lightRed, lightGreen, lightBlue));
                    m_lightQueueToRemove.Enqueue(new LightQueueContainer(lightContainer.BlockIndex - 1, lightRed, lightGreen, lightBlue));
                }
                if (isSource)
                {
                    m_lightQueueToAdd.Enqueue(new LightQueueContainer(lightContainer.BlockIndex,
                                              m_blockSource[lightContainer.BlockIndex].LightRed,
                                              m_blockSource[lightContainer.BlockIndex].LightGreen,
                                              m_blockSource[lightContainer.BlockIndex].LightBlue));
                }
            }
            PropogateFromLights();
        }

        private void ClearCellOrAddSunSource(int blockIndex)
        {
            var incomingSunLight = m_blockSource[blockIndex].LightSun;
            m_blockSource[blockIndex].LightSun = 0;
            var container = new SunQueueContainer(blockIndex, incomingSunLight);
            m_sunQueueToAdd.Enqueue(container);
            ClearCellOrAddSunSources();
        }

        private void ClearCellOrAddSunSources()
        {
            while (m_sunQueueToRemove.Count > 0)
            {
                var sunContainer = m_sunQueueToRemove.Dequeue();
                var propogate = false;
                var isSource = false;
                if (sunContainer.LightSun > m_blockSource[sunContainer.BlockIndex].LightSun || (sunContainer.PropogateDown && sunContainer.LightSun == Chunk.MaxSunValue))
                {
                    propogate = true;
                    m_blockSource[sunContainer.BlockIndex].LightSun = 0;
                }
                else
                {
                    isSource = true;
                }

                if (propogate)
                {
                    var opacity = m_blockSource[sunContainer.BlockIndex].Opacity;
                    var lightSun = sunContainer.LightSun;
                    if (!(sunContainer.PropogateDown && lightSun == Chunk.MaxSunValue) || opacity > 0)
                    {
                        lightSun = (byte)(lightSun * (1 - opacity) * SDropoff);
                    }
                    m_sunQueueToRemove.Enqueue(new SunQueueContainer(sunContainer.BlockIndex + ChunkCache.BlockStepX, lightSun));
                    m_sunQueueToRemove.Enqueue(new SunQueueContainer(sunContainer.BlockIndex - ChunkCache.BlockStepX, lightSun));
                    m_sunQueueToRemove.Enqueue(new SunQueueContainer(sunContainer.BlockIndex + ChunkCache.BlockStepZ, lightSun));
                    m_sunQueueToRemove.Enqueue(new SunQueueContainer(sunContainer.BlockIndex - ChunkCache.BlockStepZ, lightSun));
                    m_sunQueueToRemove.Enqueue(new SunQueueContainer(sunContainer.BlockIndex + 1, lightSun));
                    m_sunQueueToRemove.Enqueue(new SunQueueContainer(sunContainer.BlockIndex - 1, lightSun, true));
                }
                if (isSource)
                {
                    m_sunQueueToAdd.Enqueue(new SunQueueContainer(sunContainer.BlockIndex,
                                              m_blockSource[sunContainer.BlockIndex].LightSun));
                }
            }
            PropogateFromSunSources();
        }

        private void PropogateFromLight(int blockIndex, byte lightRed, byte lightGreen, byte lightBlue, bool immediate = false)
        {
            var container = new LightQueueContainer(blockIndex, lightRed, lightGreen, lightBlue);
            m_lightQueueToAdd.Enqueue(container);
            if (immediate)
            {
                PropogateFromLights();
            }
        }

        private void PropogateFromLight(int blockIndex, Vector3Byte currentLight, bool immediate = false)
        {
            var container = new LightQueueContainer(blockIndex, currentLight.X, currentLight.Y, currentLight.Z);
            m_lightQueueToAdd.Enqueue(container);
            if (immediate)
            {
                PropogateFromLights();
            }
        }

        private void PropogateFromLights()
        {
            while (m_lightQueueToAdd.Count > 0)
            {
                var lightContainer = m_lightQueueToAdd.Dequeue();
                var propogate = false;
                if (lightContainer.LightBlue > m_blockSource[lightContainer.BlockIndex].LightRed)
                {
                    propogate = true;
                    m_blockSource[lightContainer.BlockIndex].LightRed = lightContainer.LightRed;
                }
                if (lightContainer.LightBlue > m_blockSource[lightContainer.BlockIndex].LightGreen)
                {
                    propogate = true;
                    m_blockSource[lightContainer.BlockIndex].LightGreen = lightContainer.LightGreen;
                }
                if (lightContainer.LightBlue > m_blockSource[lightContainer.BlockIndex].LightBlue)
                {
                    propogate = true;
                    m_blockSource[lightContainer.BlockIndex].LightBlue = lightContainer.LightBlue;
                }

                if(propogate)
                {
                    var passThroughRate = (1 - m_blockSource[lightContainer.BlockIndex].Opacity) * SDropoff;
                    var lightRed = (byte)(lightContainer.LightRed > 0 ? lightContainer.LightRed * passThroughRate : 0);
                    var lightGreen = (byte)(lightContainer.LightGreen > 0 ? lightContainer.LightGreen * passThroughRate : 0);
                    var lightBlue = (byte)(lightContainer.LightBlue > 0 ? lightContainer.LightBlue * passThroughRate : 0);
                    if (CanPropogate(lightContainer.BlockIndex + ChunkCache.BlockStepX, lightRed, lightGreen, lightBlue))
                    {
                        m_lightQueueToAdd.Enqueue(new LightQueueContainer(lightContainer.BlockIndex + ChunkCache.BlockStepX, lightRed, lightGreen, lightBlue));
                    }
                    if (CanPropogate(lightContainer.BlockIndex - ChunkCache.BlockStepX, lightRed, lightGreen, lightBlue))
                    {
                        m_lightQueueToAdd.Enqueue(new LightQueueContainer(lightContainer.BlockIndex - ChunkCache.BlockStepX, lightRed, lightGreen, lightBlue));
                    }
                    if (CanPropogate(lightContainer.BlockIndex + ChunkCache.BlockStepZ, lightRed, lightGreen, lightBlue))
                    {
                        m_lightQueueToAdd.Enqueue(new LightQueueContainer(lightContainer.BlockIndex + ChunkCache.BlockStepZ, lightRed, lightGreen, lightBlue));
                    }
                    if (CanPropogate(lightContainer.BlockIndex - ChunkCache.BlockStepZ, lightRed, lightGreen, lightBlue))
                    {
                        m_lightQueueToAdd.Enqueue(new LightQueueContainer(lightContainer.BlockIndex - ChunkCache.BlockStepZ, lightRed, lightGreen, lightBlue));
                    }
                    if (CanPropogate(lightContainer.BlockIndex + 1, lightRed, lightGreen, lightBlue))
                    {
                        m_lightQueueToAdd.Enqueue(new LightQueueContainer(lightContainer.BlockIndex + 1, lightRed, lightGreen, lightBlue));
                    }
                    if (CanPropogate(lightContainer.BlockIndex - 1, lightRed, lightGreen, lightBlue))
                    {
                        m_lightQueueToAdd.Enqueue(new LightQueueContainer(lightContainer.BlockIndex - 1, lightRed, lightGreen, lightBlue));
                    }
                }
            }
        }

        private bool CanPropogate(int blockIndex, byte incomingRedLight, byte incomingGreenLight, byte incomingBlueLight)
        {
            return m_blockSource[blockIndex].LightRed < incomingRedLight ||
                   m_blockSource[blockIndex].LightGreen < incomingGreenLight ||
                   m_blockSource[blockIndex].LightBlue < incomingBlueLight;
        }

        private void PropagateFromSunSource(int blockIndex, byte incomingSunLight, bool down = false, bool immediate = false)
        {
            var container = new SunQueueContainer(blockIndex, incomingSunLight, down);
            m_sunQueueToAdd.Enqueue(container);
            if (immediate)
            {
                PropogateFromSunSources();
            }
        }

        private void PropogateFromSunSources()
        {
            while (m_sunQueueToAdd.Count > 0)
            {
                var sunContainer = m_sunQueueToAdd.Dequeue();
                var opacity = m_blockSource[sunContainer.BlockIndex].Opacity;
                if (opacity < 1)
                {
                    m_blockSource[sunContainer.BlockIndex].LightSun = sunContainer.LightSun;
                    if (sunContainer.LightSun > 0)
                    {
                        var lightSun = sunContainer.LightSun;
                        if (!(sunContainer.PropogateDown && lightSun == Chunk.MaxSunValue) || opacity > 0)
                        {
                            lightSun = (byte)(lightSun * (1 - opacity) * SDropoff);
                        }
                        if (CanPropogate(sunContainer.BlockIndex + ChunkCache.BlockStepX, lightSun))
                        {
                            m_sunQueueToAdd.Enqueue(new SunQueueContainer(sunContainer.BlockIndex + ChunkCache.BlockStepX, lightSun));
                        }
                        if (CanPropogate(sunContainer.BlockIndex - ChunkCache.BlockStepX, lightSun))
                        {
                            m_sunQueueToAdd.Enqueue(new SunQueueContainer(sunContainer.BlockIndex - ChunkCache.BlockStepX, lightSun));
                        }
                        if (CanPropogate(sunContainer.BlockIndex + ChunkCache.BlockStepZ, lightSun))
                        {
                            m_sunQueueToAdd.Enqueue(new SunQueueContainer(sunContainer.BlockIndex + ChunkCache.BlockStepZ, lightSun));
                        }
                        if (CanPropogate(sunContainer.BlockIndex - ChunkCache.BlockStepZ, lightSun))
                        {
                            m_sunQueueToAdd.Enqueue(new SunQueueContainer(sunContainer.BlockIndex - ChunkCache.BlockStepZ, lightSun));
                        }
                        if (CanPropogate(sunContainer.BlockIndex + 1, lightSun))
                        {
                            m_sunQueueToAdd.Enqueue(new SunQueueContainer(sunContainer.BlockIndex + 1, lightSun));
                        }
                        if (CanPropogate(sunContainer.BlockIndex - 1, lightSun))
                        {
                            m_sunQueueToAdd.Enqueue(new SunQueueContainer(sunContainer.BlockIndex - 1, lightSun, true));
                        }
                    }
                }
            }
        }

        private bool CanPropogate(int blockIndex, byte incomingSunLight)
        {
            return m_blockSource[blockIndex].LightSun < incomingSunLight;
        }

        private struct LightQueueContainer
        {
            public readonly int BlockIndex;
            public readonly byte LightRed;
            public readonly byte LightGreen;
            public readonly byte LightBlue;
            public LightQueueContainer(int blockIndex, byte lightRed, byte lightGreen, byte lightBlue)
            {
                BlockIndex = blockIndex;
                LightRed = lightRed;
                LightGreen = lightGreen;
                LightBlue = lightBlue;
            }
        }

        private struct SunQueueContainer
        {
            public readonly int BlockIndex;
            public readonly byte LightSun;
            public readonly bool PropogateDown;
            public SunQueueContainer(int blockIndex, byte lightSun, bool propogateDown = false)
            {
                BlockIndex = blockIndex;
                LightSun = lightSun;
                PropogateDown = propogateDown;
            }
        }
    }
}
