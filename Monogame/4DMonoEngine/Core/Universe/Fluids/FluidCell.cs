using System;
using System.Collections.Generic;
using System.Windows.Documents;
using Microsoft.Xna.Framework;

namespace _4DMonoEngine.Core.Universe.Fluids
{
    internal class FluidCell
    {
        public const float MaxLevel = 1;
        public const float MaxFlow = 1;
        public const float EvaporationLevel = 0.08f;
        public const float MinLevel = 0.0001f;
        public const float MaxCompression = 0.07f;
        public const float EvaporationRate = 0.005f;
        public const float MinFlow = 0.000001f;

        public enum CellType
        {
            Solid,
            Water,
            Air
        }

        public readonly int X;
        public readonly int Y;
        public readonly int Z;
        public float Level;
        public float LevelNextStep;
        public CellType Type;
        public FluidCell Up;
        public FluidCell Down;
        public FluidCell North;
        public FluidCell East;
        public FluidCell South;
        public FluidCell West;
        public List<FluidCell> UpdateCells;
        public bool IsSource;
        public bool Awake;

        public FluidCell(CellType type)
        {
            Type = type;
        }

        public FluidCell(int x, int y, int z, CellType type, float level)
        {
            X = x;
            Y = y;
            Z = z;
            Type = type;
            Level = level;
            LevelNextStep = level;
            Awake = true;
        }

        //TODO : break into sub functions
        public void Propagate()
        {
            if (IsSource)
            {
                Level = MathHelper.Max(Level, 1);
                LevelNextStep = Level;
            }
            var levelRemaining = Level;
            float outFlow;
            if (Level > MaxLevel && levelRemaining > (Up.Level + MaxCompression*2) && Up.Type != CellType.Solid)
            {
                outFlow = ClampFlow(levelRemaining - GetStableStateVertical(Up.Level, levelRemaining),
                    levelRemaining);
                LevelNextStep -= outFlow;
                Up.LevelNextStep += outFlow;
                Up.Awake = true;
                UpdateCells.Add(Up);
            }
            else
            {
                if (Down.Type != CellType.Solid)
                {
                    outFlow = ClampFlow(GetStableStateVertical(levelRemaining, Down.Level) - Down.Level,
                        levelRemaining);
                    if (outFlow > 0)
                    {
                        LevelNextStep -= outFlow;
                        levelRemaining -= outFlow;
                        Down.LevelNextStep += outFlow;
                        Down.Awake = true;
                        UpdateCells.Add(Down);
                    }
                }
                if (levelRemaining > 0)
                {
                    float average = 0;
                    var count = 0;
                    if (North.Type != CellType.Solid && North.Level < Level)
                    {
                        average += North.Level;
                        count += 1;
                    }
                    if (East.Type != CellType.Solid && East.Level < Level)
                    {
                        average += East.Level;
                        count += 1;
                    }
                    if (South.Type != CellType.Solid && South.Level < Level)
                    {
                        average += South.Level;
                        count += 1;
                    }
                    if (West.Type != CellType.Solid && West.Level < Level)
                    {
                        average += West.Level;
                        count += 1;
                    }
                    if (count > 0)
                    {
                        average = average/count;
                        outFlow = ClampFlow(levelRemaining - average, levelRemaining);
                        if (outFlow > 0)
                        {
                            LevelNextStep -= outFlow;
                            levelRemaining -= outFlow;
                            outFlow /= count;
                            if (North.Type != CellType.Solid && North.Level <= levelRemaining)
                            {
                                North.LevelNextStep += outFlow;
                                North.Awake = true;
                                UpdateCells.Add(North);
                            }
                            if (East.Type != CellType.Solid && East.Level <= levelRemaining)
                            {
                                East.LevelNextStep += outFlow;
                                East.Awake = true;
                                UpdateCells.Add(East);
                            }
                            if (South.Type != CellType.Solid && South.Level <= levelRemaining)
                            {
                                South.LevelNextStep += outFlow;
                                South.Awake = true;
                                UpdateCells.Add(South);
                            }
                            if (West.Type != CellType.Solid && West.Level <= levelRemaining)
                            {
                                West.LevelNextStep += outFlow;
                                West.Awake = true;
                                UpdateCells.Add(West);
                            }
                        }
                    }
                }
                if (levelRemaining > 0)
                {
                    if (Up.Type != CellType.Solid)
                    {
                        outFlow = ClampFlow(levelRemaining - GetStableStateVertical(Up.Level, levelRemaining),
                            levelRemaining);
                        if (outFlow > 0)
                        {
                            LevelNextStep -= outFlow;
                            levelRemaining -= outFlow;
                            Up.LevelNextStep += outFlow;
                            Up.Awake = true;
                            UpdateCells.Add(Up);
                        }
                    }
                    if (levelRemaining <= EvaporationLevel && Up.Type == CellType.Air)
                    {
                        LevelNextStep -= EvaporationRate;
                    }
                }
            }
            if (Math.Abs(Level - LevelNextStep) >= MinFlow / 2.0f)
            {
                UpdateCells.Add(this);
            }
            else if (!IsSource)
            {
                Awake = false;
            }
        }

        private static float ClampFlow(float flow, float level)
        {
            if (flow > MinFlow)
            {
                flow *= 0.5f;
            }
            return MathHelper.Clamp(flow, 0, MathHelper.Min(level, MaxFlow));
        }

        private static float GetStableStateVertical(float cell, float down)
        {
            var sum = cell + down;
            float newDown;
            if (sum <= MaxLevel)
            {
                newDown = MaxLevel;
            }
            else if (sum < 2*MaxLevel + MaxCompression)
            {
                newDown = (MaxLevel*MaxLevel + sum*MaxCompression)/(MaxLevel + MaxCompression);
            }
            else
            {
                newDown = (sum + MaxCompression)/2;
            }
            return newDown;
        }
    }
}