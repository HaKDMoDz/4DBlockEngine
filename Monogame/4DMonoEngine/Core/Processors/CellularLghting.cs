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
        private const float SDropoff = 0.6f;
        private readonly Queue<LightQueueContainer> m_lightQueueToAdd;
        private readonly Queue<SunQueueContainer> m_sunQueueToAdd;
        private readonly Queue<LightQueueContainer> m_lightQueueToRemove;
        private readonly Queue<SunQueueContainer> m_sunQueueToRemove;
        private readonly T[] m_blockSource;
        private readonly MappingFunction m_mappingFunction;
        private readonly int m_chunkSize;
        private readonly Dictionary<int, float> m_queuedSunValue;
        private readonly GetTarget m_getTarget;

        internal CellularLighting(T[] blockSource, MappingFunction mappingFunction, int chunkSize, GetTarget getTarget)
        {
            m_blockSource = blockSource;
            m_mappingFunction = mappingFunction;
            m_chunkSize = chunkSize;
            m_sunQueueToAdd = new Queue<SunQueueContainer>();
            m_lightQueueToAdd = new Queue<LightQueueContainer>();
            m_sunQueueToRemove = new Queue<SunQueueContainer>();
            m_lightQueueToRemove = new Queue<LightQueueContainer>();
            m_queuedSunValue = new Dictionary<int, float>();
            m_getTarget = getTarget;
        }

        internal void Process(int chunkIndexX, int chunkIndexY, int chunkIndexZ, SparseArray3D<Vector3Byte> lights)
        {
            ResetLight(chunkIndexX, chunkIndexY, chunkIndexZ,lights);
            PropogateFromSunSources();
            PropogateFromLights();
        }

        private void ResetLight(int chunkIndexX, int chunkIndexY, int chunkIndexZ, SparseArray3D<Vector3Byte> lights)
        {
            for (var x = -1; x < m_chunkSize + 1; ++x)
            {
                for (var z = -1; z < m_chunkSize + 1; ++z)
                {
                    for (var y = -1; y < m_chunkSize + 1; ++y)
                    {
                        var cX = chunkIndexX + x;
                        var cY = chunkIndexY + y;
                        var cZ = chunkIndexZ + z;
                        var blockIndex = m_mappingFunction(cX, cY, cZ);
                        if (x < 0 || y < 0 || z < 0 || x == m_chunkSize || y == m_chunkSize || z == m_chunkSize)
                        {
                            if (m_blockSource[blockIndex].LightSun > MinLight)
                            {
                                PropagateFromSunSource(cX, cY, cZ, m_blockSource[blockIndex].LightSun, y == m_chunkSize);
                            }
                         /*   if (m_blockSource[blockIndex].LightRed > 0 || m_blockSource[blockIndex].LightGreen > 0 || m_blockSource[blockIndex].LightBlue > 0)
                            {
                                PropogateFromLight(cX, cY, cZ, lights[cX, cY, cZ]);
                            }*/
                        }
                        else
                        {
                            m_blockSource[blockIndex].LightRed = MinLight;
                            m_blockSource[blockIndex].LightGreen = MinLight;
                            m_blockSource[blockIndex].LightBlue = MinLight;
                            m_blockSource[blockIndex].LightSun = MinLight;
                            if (lights.ContainsKey(cX, cY, cZ))
                            {
                                PropogateFromLight(cX, cY, cZ, lights[cX, cY, cZ]);
                            }
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

        public void AddLight(SparseArray3D<Vector3Byte> lights, int x, int y, int z, Vector3Byte light)
        {
            if (lights.ContainsKey(x, y, z))
            {
                return;
            }
            lights[x, y, z] = light;
            var blockIndex = m_mappingFunction( x, y, z);
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

        public void RemoveLight(SparseArray3D<Vector3Byte> lights, int x, int y, int z)
        {
            if (!lights.ContainsKey(x, y, z))
            {
                return;
            }
            lights.Remove(x, y, z);
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
            m_lightQueueToAdd.Enqueue(container);
        }

        private void EnqueueSun(SunQueueContainer container, int blockIndex)
        {
            m_sunQueueToAdd.Enqueue(container);
            m_queuedSunValue[blockIndex] = container.LightSun;
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
                var target = m_getTarget(x, y, z);
                target.SetDirty();
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
                var propogate = false;
                if (lightContainer.LightBlue > m_blockSource[blockIndex].LightRed)
                {
                    propogate = true;
                    m_blockSource[blockIndex].LightRed = (byte)lightContainer.LightRed;
                }
                if (lightContainer.LightBlue > m_blockSource[blockIndex].LightGreen)
                {
                    propogate = true;
                    m_blockSource[blockIndex].LightGreen = (byte)lightContainer.LightGreen;
                }
                if (lightContainer.LightBlue > m_blockSource[blockIndex].LightBlue)
                {
                    propogate = true;
                    m_blockSource[blockIndex].LightBlue = (byte)lightContainer.LightBlue;
                }

                if (!propogate)
                {
                    continue;
                }
                var passThroughRate = 1 - m_blockSource[blockIndex].Opacity * SDropoff;
                var lightRed = lightContainer.LightRed > 0 ? lightContainer.LightRed * passThroughRate : 0;
                var lightGreen = lightContainer.LightGreen > 0 ? lightContainer.LightGreen * passThroughRate : 0;
                var lightBlue = lightContainer.LightBlue > 0 ? lightContainer.LightBlue * passThroughRate : 0;
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

        private bool CanPropogate(int blockIndex, float incomingRedLight, float incomingGreenLight, float incomingBlueLight)
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
                var target = m_getTarget(x, y, z);
                if (target == null) //we wrapped around!
                {
                    continue;
                }
                target.SetDirty();
                m_queuedSunValue.Remove(blockIndex);
                var opacity = m_blockSource[blockIndex].Opacity;
                if (opacity >= 1)
                {
                    m_blockSource[blockIndex].LightSun = 0;
                    continue;
                }
                var lightSun = sunContainer.LightSun;
                if (!sunContainer.PropogateDown || lightSun < MaxSun || opacity > 0)
                {
                    lightSun = lightSun * (1 - opacity) * SDropoff;
                }
                m_blockSource[blockIndex].LightSun = (byte)lightSun;
                if (lightSun <= MinLight)
                {
                    m_blockSource[blockIndex].LightSun = MinLight;
                    continue;
                }
                ConditionallyPropogate(x + 1, y, z, lightSun);
                ConditionallyPropogate(x - 1, y, z, lightSun);
                ConditionallyPropogate(x, y, z + 1, lightSun);
                ConditionallyPropogate(x, y, z - 1, lightSun);
                ConditionallyPropogate(x, y + 1, z, lightSun);
                ConditionallyPropogate(x, y - 1, z, lightSun, true);
                
            }
        }

        private void ConditionallyPropogate(int x, int y, int z, float incomingSunLight, bool lightDown = false)
        {
            var blockIndex = m_mappingFunction(x, y, z);
            if (m_blockSource[blockIndex].Opacity >= 1 || m_blockSource[blockIndex].LightSun >= incomingSunLight || (m_queuedSunValue.ContainsKey(blockIndex) && m_queuedSunValue[blockIndex] >= incomingSunLight))
            {
                return;
            }
            EnqueueSun(new SunQueueContainer(x, y, z, incomingSunLight, lightDown), blockIndex);
        }

        private class LightQueueContainer
        {
            public readonly int X;
            public readonly int Y;
            public readonly int Z;
            public float LightRed;
            public float LightGreen;
            public float LightBlue;
            public LightQueueContainer(int x, int y, int z, float lightRed, float lightGreen, float lightBlue)
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
            public readonly float LightSun;
            public readonly bool PropogateDown;
            public SunQueueContainer(int x, int y, int z, float lightSun, bool propogateDown = false)
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
