using System.Collections.Generic;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Chunks;
using _4DMonoEngine.Core.Common.Interfaces;
using _4DMonoEngine.Core.Utils;
using _4DMonoEngine.Core.Utils.Vector;

namespace _4DMonoEngine.Core.Processors
{
    internal class CellularLighting<T> where T : ILightable
    {
        public delegate int MappingFunction(int x, int y, int z);

        private const float SDropoff = 0.9375f;
        private readonly Queue<LightQueueContainer> m_lightQueueToAdd;
        private readonly Queue<SunQueueContainer> m_sunQueueToAdd;
        private readonly Queue<LightQueueContainer> m_lightQueueToRemove;
        private readonly Queue<SunQueueContainer> m_sunQueueToRemove;
        private readonly T[] m_blockSource;
        private readonly MappingFunction m_mappingFunction;
        private readonly int m_chunkSize;
        private readonly int m_stepSizeX;
        private readonly int m_stepSizeY;
        private readonly int m_stepSizeZ;

        internal CellularLighting(T[] blockSource, MappingFunction mappingFunction, int chunkSize, int stepSizeX, int stepSizeY, int stepSizeZ)
        {
            m_blockSource = blockSource;
            m_mappingFunction = mappingFunction;
            m_chunkSize = chunkSize;
            m_stepSizeX = stepSizeX;
            m_stepSizeY = stepSizeY;
            m_stepSizeZ = stepSizeZ;
            m_sunQueueToAdd = new Queue<SunQueueContainer>();
            m_lightQueueToAdd = new Queue<LightQueueContainer>();
            m_sunQueueToRemove = new Queue<SunQueueContainer>();
            m_lightQueueToRemove = new Queue<LightQueueContainer>();
        }

        internal void Process(int chunkIndexX, int chunkIndexY, int chunkIndexZ, SparseArray3D<Vector3Byte> lights)
        {
            ResetLight(chunkIndexX, chunkIndexY, chunkIndexZ,lights);
            PropogateFromSunSources();
            PropogateFromLights();
        }

        private void ResetLight(int chunkIndexX, int chunkIndexY, int chunkIndexZ, SparseArray3D<Vector3Byte> lights)
        {
            for (byte x = 0; x < m_chunkSize; ++x)
            {
                for (byte z = 0; z < m_chunkSize; ++z)
                {
                    for (var y = m_chunkSize - 1; y > 0; --y)
                    {
                        var blockIndex = m_mappingFunction(chunkIndexX + x, chunkIndexY + y, chunkIndexZ + z) ;
                        if (y == m_chunkSize - 1)
                        {
                            if (m_blockSource[blockIndex].Opacity < 1)
                            {
                                PropagateFromSunSource(blockIndex, m_blockSource[blockIndex - 1].LightSun, down: true);
                            }
                        }
                        else
                        {
                            if (lights.ContainsKey(x, y, z))
                            {
                                PropogateFromLight(blockIndex, lights[x, y, z]);
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

        public void AddBlock(int blockIndex)
        {
            ClearCellOrAddLight(blockIndex);
            ClearCellOrAddSunSource(blockIndex);
        }

        public void RemoveBlock(int blockIndex)
        {
            var currentLight = new Vector4();
            MaxLight(blockIndex + m_stepSizeX, ref currentLight);
            MaxLight(blockIndex - m_stepSizeX, ref currentLight); 
            MaxLight(blockIndex + m_stepSizeZ, ref currentLight); 
            MaxLight(blockIndex - m_stepSizeZ, ref currentLight);
            MaxLight(blockIndex + m_stepSizeY, ref currentLight);
            MaxLight(blockIndex - m_stepSizeY, ref currentLight, true);
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
                    m_lightQueueToRemove.Enqueue(new LightQueueContainer(lightContainer.BlockIndex + m_stepSizeX, lightRed, lightGreen, lightBlue));
                    m_lightQueueToRemove.Enqueue(new LightQueueContainer(lightContainer.BlockIndex - m_stepSizeX, lightRed, lightGreen, lightBlue));
                    m_lightQueueToRemove.Enqueue(new LightQueueContainer(lightContainer.BlockIndex + m_stepSizeZ, lightRed, lightGreen, lightBlue));
                    m_lightQueueToRemove.Enqueue(new LightQueueContainer(lightContainer.BlockIndex - m_stepSizeZ, lightRed, lightGreen, lightBlue));
                    m_lightQueueToRemove.Enqueue(new LightQueueContainer(lightContainer.BlockIndex + m_stepSizeY, lightRed, lightGreen, lightBlue));
                    m_lightQueueToRemove.Enqueue(new LightQueueContainer(lightContainer.BlockIndex - m_stepSizeY, lightRed, lightGreen, lightBlue));
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
                    m_sunQueueToRemove.Enqueue(new SunQueueContainer(sunContainer.BlockIndex + m_stepSizeX, lightSun));
                    m_sunQueueToRemove.Enqueue(new SunQueueContainer(sunContainer.BlockIndex - m_stepSizeX, lightSun));
                    m_sunQueueToRemove.Enqueue(new SunQueueContainer(sunContainer.BlockIndex + m_stepSizeZ, lightSun));
                    m_sunQueueToRemove.Enqueue(new SunQueueContainer(sunContainer.BlockIndex - m_stepSizeZ, lightSun));
                    m_sunQueueToRemove.Enqueue(new SunQueueContainer(sunContainer.BlockIndex + m_stepSizeY, lightSun));
                    m_sunQueueToRemove.Enqueue(new SunQueueContainer(sunContainer.BlockIndex - m_stepSizeY, lightSun, true));
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
                    if (CanPropogate(lightContainer.BlockIndex + m_stepSizeX, lightRed, lightGreen, lightBlue))
                    {
                        m_lightQueueToAdd.Enqueue(new LightQueueContainer(lightContainer.BlockIndex + m_stepSizeX, lightRed, lightGreen, lightBlue));
                    }
                    if (CanPropogate(lightContainer.BlockIndex - m_stepSizeX, lightRed, lightGreen, lightBlue))
                    {
                        m_lightQueueToAdd.Enqueue(new LightQueueContainer(lightContainer.BlockIndex - m_stepSizeX, lightRed, lightGreen, lightBlue));
                    }
                    if (CanPropogate(lightContainer.BlockIndex + m_stepSizeZ, lightRed, lightGreen, lightBlue))
                    {
                        m_lightQueueToAdd.Enqueue(new LightQueueContainer(lightContainer.BlockIndex + m_stepSizeZ, lightRed, lightGreen, lightBlue));
                    }
                    if (CanPropogate(lightContainer.BlockIndex - m_stepSizeZ, lightRed, lightGreen, lightBlue))
                    {
                        m_lightQueueToAdd.Enqueue(new LightQueueContainer(lightContainer.BlockIndex - m_stepSizeZ, lightRed, lightGreen, lightBlue));
                    }
                    if (CanPropogate(lightContainer.BlockIndex + 1, lightRed, lightGreen, lightBlue))
                    {
                        m_lightQueueToAdd.Enqueue(new LightQueueContainer(lightContainer.BlockIndex + m_stepSizeY, lightRed, lightGreen, lightBlue));
                    }
                    if (CanPropogate(lightContainer.BlockIndex - 1, lightRed, lightGreen, lightBlue))
                    {
                        m_lightQueueToAdd.Enqueue(new LightQueueContainer(lightContainer.BlockIndex - m_stepSizeY, lightRed, lightGreen, lightBlue));
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
                        if (CanPropogate(sunContainer.BlockIndex + m_stepSizeX, lightSun))
                        {
                            m_sunQueueToAdd.Enqueue(new SunQueueContainer(sunContainer.BlockIndex + m_stepSizeX, lightSun));
                        }
                        if (CanPropogate(sunContainer.BlockIndex - m_stepSizeX, lightSun))
                        {
                            m_sunQueueToAdd.Enqueue(new SunQueueContainer(sunContainer.BlockIndex - m_stepSizeX, lightSun));
                        }
                        if (CanPropogate(sunContainer.BlockIndex + m_stepSizeZ, lightSun))
                        {
                            m_sunQueueToAdd.Enqueue(new SunQueueContainer(sunContainer.BlockIndex + m_stepSizeZ, lightSun));
                        }
                        if (CanPropogate(sunContainer.BlockIndex - m_stepSizeZ, lightSun))
                        {
                            m_sunQueueToAdd.Enqueue(new SunQueueContainer(sunContainer.BlockIndex - m_stepSizeZ, lightSun));
                        }
                        if (CanPropogate(sunContainer.BlockIndex + m_stepSizeY, lightSun))
                        {
                            m_sunQueueToAdd.Enqueue(new SunQueueContainer(sunContainer.BlockIndex + m_stepSizeY, lightSun));
                        }
                        if (CanPropogate(sunContainer.BlockIndex - m_stepSizeY, lightSun))
                        {
                            m_sunQueueToAdd.Enqueue(new SunQueueContainer(sunContainer.BlockIndex - m_stepSizeY, lightSun, true));
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
