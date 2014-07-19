﻿using System.Collections.Generic;
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
        private readonly HashSet<int> m_queuedSunHashSet;
        private readonly HashSet<int> m_queuedLightHashSet;

        internal CellularLighting(T[] blockSource, MappingFunction mappingFunction, int chunkSize)
        {
            m_blockSource = blockSource;
            m_mappingFunction = mappingFunction;
            m_chunkSize = chunkSize;
            m_sunQueueToAdd = new Queue<SunQueueContainer>();
            m_lightQueueToAdd = new Queue<LightQueueContainer>();
            m_sunQueueToRemove = new Queue<SunQueueContainer>();
            m_lightQueueToRemove = new Queue<LightQueueContainer>();
            m_queuedSunHashSet = new HashSet<int>();
            m_queuedLightHashSet = new HashSet<int>();
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
                            if (m_blockSource[blockIndex].Opacity < 1 && m_blockSource[blockIndex - 1].LightSun > 0)
                            {
                                PropagateFromSunSource(x, y, z, m_blockSource[blockIndex - 1].LightSun, down: true);
                            }
                        }
                        else
                        {
                            if (lights.ContainsKey(x, y, z))
                            {
                                PropogateFromLight(x, y, z, lights[x, y, z]);
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

        public void AddBlock(int x, int y, int z)
        {
            ClearLightFromCell(x, y, z);
            ClearSunFromCell(x, y, z);
        }

        public void RemoveBlock(int x, int y, int z)
        {
            var currentLight = new Vector4();
            MaxLight(x + 1, y, z, ref currentLight);
            MaxLight(x - 1, y, z, ref currentLight);
            MaxLight(x, y, z + 1 , ref currentLight);
            MaxLight(x, y, z - 1, ref currentLight);
            MaxLight(x, y + 1, z, ref currentLight);
            MaxLight(x, y - 1, z, ref currentLight, true);
            PropogateFromLight(x, y, z, (byte)currentLight.X, (byte)currentLight.Y, (byte)currentLight.Z, immediate: true);
            PropagateFromSunSource(x, y, z, (byte)currentLight.W, down: true, immediate: true);
        }

        private void MaxLight(int x, int y, int z, ref Vector4 currentLight, bool propogateDown = false)
        {
            var blockIndex = m_mappingFunction(x, y, z);
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
            if (chunk.LightSources.ContainsKey(x, y, z))
            {
                return;
            }
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
                PropogateFromLight(x, y, z, lightRed, lightGreen, lightBlue, true);
            }
        }

        public void RemoveLight(Chunk chunk, int x, int y, int z)
        {
            if (!chunk.LightSources.ContainsKey(x, y, z))
            {
                return;
            }
            chunk.LightSources.Remove(x, y, z);
            ClearLightFromCell(x, y, z);
        }

        private void ClearLightFromCell(int x, int y, int z)
        {
            var blockIndex = m_mappingFunction(x, y, z);
            var lightRed = m_blockSource[blockIndex].LightRed;
            m_blockSource[blockIndex].LightRed = 0;
            var lightGreen = m_blockSource[blockIndex].LightGreen;
            m_blockSource[blockIndex].LightGreen = 0;
            var lightBlue = m_blockSource[blockIndex].LightBlue;
            m_blockSource[blockIndex].LightBlue = 0;
            var container = new LightQueueContainer(x, y, z, lightRed, lightGreen, lightBlue);
            m_lightQueueToRemove.Enqueue(container);
            ClearCellsOrAddLightSources();
        }

        private void ClearCellsOrAddLightSources()
        {
            while (m_lightQueueToRemove.Count > 0)
            {
                var lightContainer = m_lightQueueToRemove.Dequeue();
                var x = lightContainer.X;
                var y = lightContainer.Y;
                var z = lightContainer.Z;
                var blockIndex = m_mappingFunction(x, y, z);
                var propogate = false;
                var isSource = false;
                if (lightContainer.LightBlue > m_blockSource[blockIndex].LightRed)
                {
                    propogate = true;
                    m_blockSource[blockIndex].LightRed = 0;
                }
                else
                {
                    isSource = true;
                }
                if (lightContainer.LightBlue > m_blockSource[blockIndex].LightGreen)
                {
                    propogate = true;
                    m_blockSource[blockIndex].LightGreen = 0;
                }
                else
                {
                    isSource = true;
                }
                if (lightContainer.LightBlue > m_blockSource[blockIndex].LightBlue)
                {
                    propogate = true;
                    m_blockSource[blockIndex].LightBlue = 0;
                }
                else
                {
                    isSource = true;
                }

                if(propogate)
                {
                    var passThroughRate = (1 - m_blockSource[blockIndex].Opacity) * SDropoff;
                    var lightRed = (byte)(lightContainer.LightRed > 0 ? lightContainer.LightRed * passThroughRate : 0);
                    var lightGreen = (byte)(lightContainer.LightGreen > 0 ? lightContainer.LightGreen * passThroughRate : 0);
                    var lightBlue = (byte)(lightContainer.LightBlue > 0 ? lightContainer.LightBlue * passThroughRate : 0);
                    m_lightQueueToRemove.Enqueue(new LightQueueContainer(x + 1, y, z, lightRed, lightGreen, lightBlue));
                    m_lightQueueToRemove.Enqueue(new LightQueueContainer(x - 1, y, z, lightRed, lightGreen, lightBlue));
                    m_lightQueueToRemove.Enqueue(new LightQueueContainer(x, y, z + 1, lightRed, lightGreen, lightBlue));
                    m_lightQueueToRemove.Enqueue(new LightQueueContainer(x, y, z - 1, lightRed, lightGreen, lightBlue));
                    m_lightQueueToRemove.Enqueue(new LightQueueContainer(x, y + 1, z, lightRed, lightGreen, lightBlue));
                    m_lightQueueToRemove.Enqueue(new LightQueueContainer(x, y - 1, z, lightRed, lightGreen, lightBlue));
                }
                if (isSource)
                {
                    EnqueueLight(new LightQueueContainer(x, y, z,
                                              m_blockSource[blockIndex].LightRed,
                                              m_blockSource[blockIndex].LightGreen,
                                              m_blockSource[blockIndex].LightBlue), blockIndex);
                }
            }
            PropogateFromLights();
        }

        private void EnqueueLight(LightQueueContainer container, int blockIndex)
        {
            if (m_queuedLightHashSet.Contains(blockIndex))
            {
                return;
            }
            m_lightQueueToAdd.Enqueue(container);
            m_queuedLightHashSet.Add(blockIndex);
        }

        private void EnqueueSun(SunQueueContainer container, int blockIndex)
        {
            if (m_queuedSunHashSet.Contains(blockIndex))
            {
                return;
            }
            m_sunQueueToAdd.Enqueue(container);
            m_queuedSunHashSet.Add(blockIndex);
        }

        private void ClearSunFromCell(int x, int y, int z)
        {
            var blockIndex = m_mappingFunction(x, y, z);
            var incomingSunLight = m_blockSource[blockIndex].LightSun;
            m_blockSource[blockIndex].LightSun = 0;
            var container = new SunQueueContainer(x, y, z, incomingSunLight);
            m_sunQueueToRemove.Enqueue(container);
            ClearCellOrAddSunSources();
        }

        private void ClearCellOrAddSunSources()
        {
            while (m_sunQueueToRemove.Count > 0)
            {
                var sunContainer = m_sunQueueToRemove.Dequeue();
                var x = sunContainer.X;
                var y = sunContainer.Y;
                var z = sunContainer.Z;
                var blockIndex = m_mappingFunction(x, y, z);
                var propogate = false;
                var isSource = false;
                if (sunContainer.LightSun > m_blockSource[blockIndex].LightSun || (sunContainer.PropogateDown && sunContainer.LightSun == Chunk.MaxSunValue))
                {
                    propogate = true;
                    m_blockSource[blockIndex].LightSun = 0;
                }
                else
                {
                    isSource = true;
                }

                if (propogate)
                {
                    var opacity = m_blockSource[blockIndex].Opacity;
                    var lightSun = sunContainer.LightSun;
                    if (!(sunContainer.PropogateDown && lightSun == Chunk.MaxSunValue) || opacity > 0)
                    {
                        lightSun = (byte)(lightSun * (1 - opacity) * SDropoff);
                    }
                    m_sunQueueToRemove.Enqueue(new SunQueueContainer(x + 1, y, z, lightSun));
                    m_sunQueueToRemove.Enqueue(new SunQueueContainer(x - 1, y, z, lightSun));
                    m_sunQueueToRemove.Enqueue(new SunQueueContainer(x, y, z + 1, lightSun));
                    m_sunQueueToRemove.Enqueue(new SunQueueContainer(x, y, z - 1, lightSun));
                    m_sunQueueToRemove.Enqueue(new SunQueueContainer(x, y + 1, z, lightSun));
                    m_sunQueueToRemove.Enqueue(new SunQueueContainer(x, y - 1, z, lightSun, true));
                }
                if (isSource)
                {
                    EnqueueSun(new SunQueueContainer(x, y, z, m_blockSource[blockIndex].LightSun), blockIndex);
                }
            }
            PropogateFromSunSources();
        }

        private void PropogateFromLight(int x, int y, int z, byte lightRed, byte lightGreen, byte lightBlue, bool immediate = false)
        {
            var container = new LightQueueContainer(x, y, z, lightRed, lightGreen, lightBlue);
            EnqueueLight(container, m_mappingFunction(x, y, z));
            if (immediate)
            {
                PropogateFromLights();
            }
        }

        private void PropogateFromLight(int x, int y, int z, Vector3Byte currentLight, bool immediate = false)
        {
            var container = new LightQueueContainer(x, y, z, currentLight.X, currentLight.Y, currentLight.Z);
            EnqueueLight(container, m_mappingFunction(x, y, z));
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
                var x = lightContainer.X;
                var y = lightContainer.Y;
                var z = lightContainer.Z;
                var blockIndex = m_mappingFunction(x, y, z);
                m_queuedLightHashSet.Remove(blockIndex);
                var propogate = false;
                if (lightContainer.LightBlue > m_blockSource[blockIndex].LightRed)
                {
                    propogate = true;
                    m_blockSource[blockIndex].LightRed = lightContainer.LightRed;
                }
                if (lightContainer.LightBlue > m_blockSource[blockIndex].LightGreen)
                {
                    propogate = true;
                    m_blockSource[blockIndex].LightGreen = lightContainer.LightGreen;
                }
                if (lightContainer.LightBlue > m_blockSource[blockIndex].LightBlue)
                {
                    propogate = true;
                    m_blockSource[blockIndex].LightBlue = lightContainer.LightBlue;
                }

                if (!propogate)
                {
                    continue;
                }
                var passThroughRate = (1 - m_blockSource[blockIndex].Opacity) * SDropoff;
                var lightRed = (byte)(lightContainer.LightRed > 0 ? lightContainer.LightRed * passThroughRate : 0);
                var lightGreen = (byte)(lightContainer.LightGreen > 0 ? lightContainer.LightGreen * passThroughRate : 0);
                var lightBlue = (byte)(lightContainer.LightBlue > 0 ? lightContainer.LightBlue * passThroughRate : 0);
                blockIndex = m_mappingFunction(x + 1, y, z);
                if (CanPropogate(blockIndex, lightRed, lightGreen, lightBlue))
                {
                    EnqueueLight(new LightQueueContainer(x + 1, y, z, lightRed, lightGreen, lightBlue), blockIndex); 
                }
                blockIndex = m_mappingFunction(x - 1, y, z);
                if (CanPropogate(blockIndex, lightRed, lightGreen, lightBlue))
                {
                    EnqueueLight(new LightQueueContainer(x - 1, y, z, lightRed, lightGreen, lightBlue), blockIndex);
                }
                blockIndex = m_mappingFunction(x, y, z + 1);
                if (CanPropogate(blockIndex, lightRed, lightGreen, lightBlue))
                {
                    EnqueueLight(new LightQueueContainer(x, y, z + 1, lightRed, lightGreen, lightBlue), blockIndex);
                }
                blockIndex = m_mappingFunction(x, y, z - 1);
                if (CanPropogate(blockIndex, lightRed, lightGreen, lightBlue))
                {
                    EnqueueLight(new LightQueueContainer(x, y, z - 1, lightRed, lightGreen, lightBlue), blockIndex);
                }
                blockIndex = m_mappingFunction(x, y + 1, z);
                if (CanPropogate(blockIndex, lightRed, lightGreen, lightBlue))
                {
                    EnqueueLight(new LightQueueContainer(x, y + 1, z, lightRed, lightGreen, lightBlue), blockIndex);
                }
                blockIndex = m_mappingFunction(x, y - 1, z);
                if (CanPropogate(blockIndex, lightRed, lightGreen, lightBlue))
                {
                    EnqueueLight(new LightQueueContainer(x, y - 1, z, lightRed, lightGreen, lightBlue), blockIndex);
                }
            }
        }

        private bool CanPropogate(int blockIndex, byte incomingRedLight, byte incomingGreenLight, byte incomingBlueLight)
        {
            return m_blockSource[blockIndex].LightRed < incomingRedLight ||
                   m_blockSource[blockIndex].LightGreen < incomingGreenLight ||
                   m_blockSource[blockIndex].LightBlue < incomingBlueLight;
        }

        private void PropagateFromSunSource(int x, int y, int z, byte incomingSunLight, bool down = false, bool immediate = false)
        {
            var container = new SunQueueContainer(x, y, z, incomingSunLight, down);
            EnqueueSun(container,  m_mappingFunction(x, y, z));
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
                var x = sunContainer.X;
                var y = sunContainer.Y;
                var z = sunContainer.Z;
                var blockIndex = m_mappingFunction(x, y, z);
                m_queuedSunHashSet.Remove(blockIndex);
                var opacity = m_blockSource[blockIndex].Opacity;
                if (opacity >= 1)
                {
                    continue;
                }
                m_blockSource[blockIndex].LightSun = sunContainer.LightSun;
                if (sunContainer.LightSun <= 0)
                {
                    continue;
                }
                var lightSun = sunContainer.LightSun;
                if (!(sunContainer.PropogateDown && lightSun == Chunk.MaxSunValue) || opacity > 0)
                {
                    lightSun = (byte)(lightSun * (1 - opacity) * SDropoff);
                }
                blockIndex = m_mappingFunction(x + 1, y, z);
                if (CanPropogate(blockIndex, lightSun))
                {
                    EnqueueSun(new SunQueueContainer(x + 1, y, z, lightSun), blockIndex);
                }
                blockIndex = m_mappingFunction(x - 1, y, z);
                if (CanPropogate(blockIndex, lightSun))
                {
                    EnqueueSun(new SunQueueContainer(x - 1, y, z, lightSun), blockIndex);
                }
                blockIndex = m_mappingFunction(x, y, z + 1);
                if (CanPropogate(blockIndex, lightSun))
                {
                    EnqueueSun(new SunQueueContainer(x, y, z + 1, lightSun), blockIndex);
                }
                blockIndex = m_mappingFunction(x, y, z - 1);
                if (CanPropogate(blockIndex, lightSun))
                {
                    EnqueueSun(new SunQueueContainer(x, y, z - 1, lightSun), blockIndex);
                }
                blockIndex = m_mappingFunction(x, y + 1, z );
                if (CanPropogate(blockIndex, lightSun))
                {
                    EnqueueSun(new SunQueueContainer(x, y + 1, z, lightSun), blockIndex);
                }
                blockIndex = m_mappingFunction(x, y - 1, z);
                if (CanPropogate(blockIndex, lightSun))
                {
                    EnqueueSun(new SunQueueContainer(x, y - 1, z, lightSun, true), blockIndex);
                }
            }
        }

        private bool CanPropogate(int blockIndex, byte incomingSunLight)
        {
            return m_blockSource[blockIndex].Opacity < 1 && m_blockSource[blockIndex].LightSun < incomingSunLight;
        }

        private struct LightQueueContainer
        {
            public readonly int X;
            public readonly int Y;
            public readonly int Z;
            public readonly byte LightRed;
            public readonly byte LightGreen;
            public readonly byte LightBlue;
            public LightQueueContainer(int x, int y, int z, byte lightRed, byte lightGreen, byte lightBlue)
            {
                X = x;
                Y = y;
                Z = z;
                LightRed = lightRed;
                LightGreen = lightGreen;
                LightBlue = lightBlue;
            }
        }

        private struct SunQueueContainer
        {
            public readonly int X;
            public readonly int Y;
            public readonly int Z;
            public readonly byte LightSun;
            public readonly bool PropogateDown;
            public SunQueueContainer(int x, int y, int z, byte lightSun, bool propogateDown = false)
            {
                X = x;
                Y = y;
                Z = z;
                LightSun = lightSun;
                PropogateDown = propogateDown;
            }
        }
    }
}
