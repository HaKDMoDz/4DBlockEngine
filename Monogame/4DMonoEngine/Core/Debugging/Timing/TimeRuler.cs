using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using _4DMonoEngine.Core.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace _4DMonoEngine.Core.Debugging.Timing
{
    /// <summary>
    /// Realtime CPU measuring tool
    /// </summary>
    /// <remarks>
    /// You can visually find bottle neck, and know how much you can put more CPU jobs
    /// by using this tool.
    /// Because of this is real time profile, you can find glitches in the game too.
    /// 
    /// TimeRuler provide the following features:
    ///  * Up to 8 bars (Configurable)
    ///  * Change colors for each markers
    ///  * Marker logging.
    ///  * It won't even generate BeginMark/EndMark method calls when you got rid of the
    ///    TRACE constant.
    ///  * It supports up to 32 (Configurable) nested BeginMark method calls.
    ///  * Multithreaded safe
    ///  * Automatically changes display frames based on frame duration.
    ///  
    /// How to use:
    /// Added TimerRuler instance to Game.Components and call timerRuler.StartFrame in
    /// top of the Game.Update method.
    /// 
    /// Then, surround the code that you want measure by BeginMark and EndMark.
    /// 
    /// timeRuler.BeginMark( "Update", Color.Blue );
    /// // process that you want to measure.
    /// timerRuler.EndMark( "Update" );
    /// 
    /// Also, you can specify bar index of marker (default value is 0)
    /// 
    /// timeRuler.BeginMark( 1, "Update", Color.Blue );
    /// 
    /// All profiling methods has CondionalAttribute with "TRACE".
    /// If you not specified "TRACE" constant, it doesn't even generate
    /// method calls for BeginMark/EndMark.
    /// So, don't forget remove "TRACE" constant when you release your game.
    /// 
    /// </remarks>
    public class TimeRuler : DrawableGameComponent
    {
        private SpriteBatch m_spriteBatch;
        private Texture2D m_texture;
        private SpriteFont m_debugFont;
        
        #region Constants

        /// <summary>
        /// Max bar count.
        /// </summary>
        const int MaxBars = 8;

        /// <summary>
        /// Maximum sample number for each bar.
        /// </summary>
        const int MaxSamples = 256;

        /// <summary>
        /// Maximum nest calls for each bar.
        /// </summary>
        const int MaxNestCall = 32;

        /// <summary>
        /// Maximum display frames.
        /// </summary>
        const int MaxSampleFrames = 4;

        /// <summary>
        /// Duration (in frame count) for take snap shot of log.
        /// </summary>
        const int LogSnapDuration = 120;

        /// <summary>
        /// Height(in pixels) of bar.
        /// </summary>
        const int BarHeight = 8;

        /// <summary>
        /// Padding(in pixels) of bar.
        /// </summary>
        const int BarPadding = 2;

        /// <summary>
        /// Delay frame count for auto display frame adjustment.
        /// </summary>
        const int AutoAdjustDelay = 30;

        #endregion

        #region Properties

        /// <summary>
        /// Gets/Set log display or no.
        /// </summary>
        public bool ShowLog { get; set; }

        /// <summary>
        /// Gets/Sets target sample frames.
        /// </summary>
        public int TargetSampleFrames { get; set; }

        /// <summary>
        /// Gets/Sets TimeRuler rendering position.
        /// </summary>
        public Vector2 Position { get { return m_position; } set { m_position = value; } }

        /// <summary>
        /// Gets/Sets timer ruler width.
        /// </summary>
        public int Width { get; set; }

        #endregion

        #region Fields

#if TRACE

        /// <summary>
        /// Marker structure.
        /// </summary>
        private struct Marker
        {
            public int MarkerId;
            public float BeginTime;
            public float EndTime;
            public Color Color;
        }

        /// <summary>
        /// Collection of markers.
        /// </summary>
        private class MarkerCollection
        {
            // Marker collection.
            public readonly Marker[] Markers = new Marker[MaxSamples];
            public int MarkCount;

            // Marker nest information.
            public readonly int[] MarkerNests = new int[MaxNestCall];
            public int NestCount;
        }

        /// <summary>
        /// Frame logging information.
        /// </summary>
        private class FrameLog
        {
            public readonly MarkerCollection[] Bars;

            public FrameLog()
            {
                // Initialize markers.
                Bars = new MarkerCollection[MaxBars];
                for (var i = 0; i < MaxBars; ++i)
                    Bars[i] = new MarkerCollection();
            }
        }

        /// <summary>
        /// Marker information
        /// </summary>
        private class MarkerInfo
        {
            // Name of marker.
            public readonly string Name;

            // Marker log.
            public readonly MarkerLog[] Logs = new MarkerLog[MaxBars];

            public MarkerInfo(string name)
            {
                Name = name;
            }
        }

        /// <summary>
        /// Marker log information.
        /// </summary>
        private struct MarkerLog
        {
            public float SnapAvg;

            public float Min;
            public float Max;
            public float Avg;

            public int Samples;

            public Color Color;

            public bool Initialized;
        }

        // Logs for each frames.
        FrameLog[] m_logs;

        // Previous frame log.
        FrameLog m_prevLog;

        // Current log.
        FrameLog m_curLog;

        // Current frame count.
        int m_frameCount;

        // Stopwatch for measure the time.
        readonly Stopwatch m_stopwatch = new Stopwatch();

        // Marker information array.
        readonly List<MarkerInfo> m_markers = new List<MarkerInfo>();

        // Dictionary that maps from marker name to marker id.
        readonly Dictionary<string, int> m_markerNameToIdMap = new Dictionary<string, int>();

        // Display frame adjust counter.
        int m_frameAdjust;

        // Current display frame count.
        int m_sampleFrames;

        // Marker log string.
        readonly StringBuilder m_logString = new StringBuilder(512);

        // You want to call StartFrame at beginning of Game.Update method.
        // But Game.Update gets calls multiple time when game runs slow in fixed time step mode.
        // In this case, we should ignore StartFrame call.
        // To do this, we just keep tracking of number of StartFrame calls until Draw gets called.
        int m_updateCount;

#endif
        // TimerRuler draw position.
        Vector2 m_position;

        #endregion

        #region Initialization

        public TimeRuler(Game game)
            : base(game)
        {
            // Add this as a service.
            Game.Services.AddService(typeof(TimeRuler), this);
        }

        public override void Initialize()
        {
#if TRACE
            m_spriteBatch = new SpriteBatch(Game.GraphicsDevice);
            m_texture = new Texture2D(Game.GraphicsDevice, 1, 1);
            Color[] whitePixels = { Color.White };
            m_texture.SetData(whitePixels);
            m_debugFont = MainEngine.GetEngineInstance().GetAsset<SpriteFont>("Verdana");

            // Initialize Parameters.
            m_logs = new FrameLog[2];
            for (var i = 0; i < m_logs.Length; ++i)
                m_logs[i] = new FrameLog();

            m_sampleFrames = TargetSampleFrames = 1;

            // Time-Ruler's update method doesn't need to get called.
            Enabled = false;

#endif
            base.Initialize();
        }

        protected override void LoadContent()
        {
            Width = (int)(GraphicsDevice.Viewport.Width * 0.8f);

            var layout = new Layout(GraphicsDevice.Viewport);
            m_position = layout.Place(new Vector2(Width, BarHeight),
                                                    0, 0.01f, Alignment.BottomCenter);

            base.LoadContent();
        }

        #endregion

        #region Measuring methods

        /// <summary>
        /// Start new frame.
        /// </summary>
        [Conditional("TRACE")]
        public void StartFrame()
        {
#if TRACE
            lock (this)
            {
                // We skip reset frame when this method gets called multiple times.
                var count = Interlocked.Increment(ref m_updateCount);
                if (Visible && (1 < count && count < MaxSampleFrames))
                    return;

                // Update current frame log.
                m_prevLog = m_logs[m_frameCount++ & 0x1];
                m_curLog = m_logs[m_frameCount & 0x1];

                var endFrameTime = (float)m_stopwatch.Elapsed.TotalMilliseconds;

                // Update marker and create a log.
                for (var barIdx = 0; barIdx < m_prevLog.Bars.Length; ++barIdx)
                {
                    var prevBar = m_prevLog.Bars[barIdx];
                    var nextBar = m_curLog.Bars[barIdx];

                    // Re-open marker that didn't get called EndMark in previous frame.
                    for (var nest = 0; nest < prevBar.NestCount; ++nest)
                    {
                        var markerIdx = prevBar.MarkerNests[nest];

                        prevBar.Markers[markerIdx].EndTime = endFrameTime;

                        nextBar.MarkerNests[nest] = nest;
                        nextBar.Markers[nest].MarkerId =
                            prevBar.Markers[markerIdx].MarkerId;
                        nextBar.Markers[nest].BeginTime = 0;
                        nextBar.Markers[nest].EndTime = -1;
                        nextBar.Markers[nest].Color = prevBar.Markers[markerIdx].Color;
                    }

                    // Update marker log.
                    for (var markerIdx = 0; markerIdx < prevBar.MarkCount; ++markerIdx)
                    {
                        var duration = prevBar.Markers[markerIdx].EndTime -
                                            prevBar.Markers[markerIdx].BeginTime;

                        var markerId = prevBar.Markers[markerIdx].MarkerId;
                        var m = m_markers[markerId];

                        m.Logs[barIdx].Color = prevBar.Markers[markerIdx].Color;

                        if (!m.Logs[barIdx].Initialized)
                        {
                            // First frame process.
                            m.Logs[barIdx].Min = duration;
                            m.Logs[barIdx].Max = duration;
                            m.Logs[barIdx].Avg = duration;

                            m.Logs[barIdx].Initialized = true;
                        }
                        else
                        {
                            // Process after first frame.
                            m.Logs[barIdx].Min = Math.Min(m.Logs[barIdx].Min, duration);
                            m.Logs[barIdx].Max = Math.Min(m.Logs[barIdx].Max, duration);
                            m.Logs[barIdx].Avg += duration;
                            m.Logs[barIdx].Avg *= 0.5f;

                            if (m.Logs[barIdx].Samples++ >= LogSnapDuration)
                            {
                                m.Logs[barIdx].SnapAvg = m.Logs[barIdx].Avg;
                                m.Logs[barIdx].Samples = 0;
                            }
                        }
                    }

                    nextBar.MarkCount = prevBar.NestCount;
                    nextBar.NestCount = prevBar.NestCount;
                }

                // Start measuring.
                m_stopwatch.Reset();
                m_stopwatch.Start();
            }
#endif
        }

        /// <summary>
        /// Start measure time.
        /// </summary>
        /// <param name="markerName">name of marker.</param>
        /// <param name="color"/>color/param>
        [Conditional("TRACE")]
        public void BeginMark(string markerName, Color color)
        {
#if TRACE
            BeginMark(0, markerName, color);
#endif
        }

        /// <summary>
        /// Start measure time.
        /// </summary>
        /// <param name="barIndex">index of bar</param>
        /// <param name="markerName">name of marker.</param>
        /// <param name="color">color</param>
        [Conditional("TRACE")]
        public void BeginMark(int barIndex, string markerName, Color color)
        {
#if TRACE
            lock (this)
            {
                if (barIndex < 0 || barIndex >= MaxBars)
                    throw new ArgumentOutOfRangeException("barIndex");

                var bar = m_curLog.Bars[barIndex];

                if (bar.MarkCount >= MaxSamples)
                {
                    throw new OverflowException(
                        "Exceeded sample count.\n" +
                        "Either set larger number to TimeRuler.MaxSmpale or" +
                        "lower sample count.");
                }

                if (bar.NestCount >= MaxNestCall)
                {
                    throw new OverflowException(
                        "Exceeded nest count.\n" +
                        "Either set larget number to TimeRuler.MaxNestCall or" +
                        "lower nest calls.");
                }

                // Gets registered marker.
                int markerId;
                if (!m_markerNameToIdMap.TryGetValue(markerName, out markerId))
                {
                    // Register this if this marker is not registered.
                    markerId = m_markers.Count;
                    m_markerNameToIdMap.Add(markerName, markerId);
                    m_markers.Add(new MarkerInfo(markerName));
                }

                // Start measuring.
                bar.MarkerNests[bar.NestCount++] = bar.MarkCount;

                // Fill marker parameters.
                bar.Markers[bar.MarkCount].MarkerId = markerId;
                bar.Markers[bar.MarkCount].Color = color;
                bar.Markers[bar.MarkCount].BeginTime =
                                        (float)m_stopwatch.Elapsed.TotalMilliseconds;

                bar.Markers[bar.MarkCount].EndTime = -1;

                bar.MarkCount++;
            }
#endif
        }

        /// <summary>
        /// End measuring.
        /// </summary>
        /// <param name="markerName">Name of marker.</param>
        [Conditional("TRACE")]
        public void EndMark(string markerName)
        {
#if TRACE
            EndMark(0, markerName);
#endif
        }

        /// <summary>
        /// End measuring.
        /// </summary>
        /// <param name="barIndex">Index of bar.</param>
        /// <param name="markerName">Name of marker.</param>
        [Conditional("TRACE")]
        public void EndMark(int barIndex, string markerName)
        {
#if TRACE
            lock (this)
            {
                if (barIndex < 0 || barIndex >= MaxBars)
                    throw new ArgumentOutOfRangeException("barIndex");

                var bar = m_curLog.Bars[barIndex];

                if (bar.NestCount <= 0)
                {
                    throw new InvalidOperationException(
                        "Call BeingMark method before call EndMark method.");
                }

                int markerId;
                if (!m_markerNameToIdMap.TryGetValue(markerName, out markerId))
                {
                    throw new InvalidOperationException(
                        String.Format("Maker '{0}' is not registered." +
                            "Make sure you specifed same name as you used for BeginMark" +
                            " method.",
                            markerName));
                }

                var markerIdx = bar.MarkerNests[--bar.NestCount];
                if (bar.Markers[markerIdx].MarkerId != markerId)
                {
                    throw new InvalidOperationException(
                    "Incorrect call order of BeginMark/EndMark method." +
                    "You call it like BeginMark(A), BeginMark(B), EndMark(B), EndMark(A)" +
                    " But you can't call it like " +
                    "BeginMark(A), BeginMark(B), EndMark(A), EndMark(B).");
                }

                bar.Markers[markerIdx].EndTime =
                    (float)m_stopwatch.Elapsed.TotalMilliseconds;
            }
#endif
        }

        /// <summary>
        /// Get average time of given bar index and marker name.
        /// </summary>
        /// <param name="barIndex">Index of bar</param>
        /// <param name="markerName">name of marker</param>
        /// <returns>average spending time in ms.</returns>
        public float GetAverageTime(int barIndex, string markerName)
        {
#if TRACE
            if (barIndex < 0 || barIndex >= MaxBars)
                throw new ArgumentOutOfRangeException("barIndex");

            float result = 0;
            int markerId;
            if (m_markerNameToIdMap.TryGetValue(markerName, out markerId))
                result = m_markers[markerId].Logs[barIndex].Avg;

            return result;
#else
            return 0f;
#endif
        }

        /// <summary>
        /// Reset marker log.
        /// </summary>
        [Conditional("TRACE")]
        public void ResetLog()
        {
#if TRACE
            lock (this)
            {
                foreach (var markerInfo in m_markers)
                {
                    for (var i = 0; i < markerInfo.Logs.Length; ++i)
                    {
                        markerInfo.Logs[i].Initialized = false;
                        markerInfo.Logs[i].SnapAvg = 0;

                        markerInfo.Logs[i].Min = 0;
                        markerInfo.Logs[i].Max = 0;
                        markerInfo.Logs[i].Avg = 0;

                        markerInfo.Logs[i].Samples = 0;
                    }
                }
            }
#endif
        }

        #endregion

        #region Draw

        public override void Draw(GameTime gameTime)
        {

            Draw(m_position, Width);
            base.Draw(gameTime);
        }

        [Conditional("TRACE")]
        private void Draw(Vector2 position, int width)
        {
#if TRACE
            // Reset update count.
            Interlocked.Exchange(ref m_updateCount, 0);            

            // Adjust size and position based of number of bars we should draw.
            var height = 0;
            float maxTime = 0;
            foreach (var bar in m_prevLog.Bars)
            {
                if (bar.MarkCount > 0)
                {
                    height += BarHeight + BarPadding * 2;
                    maxTime = Math.Max(maxTime,
                                            bar.Markers[bar.MarkCount - 1].EndTime);
                }
            }

            // Auto display frame adjustment.
            // For example, if the entire process of frame doesn't finish in less than 16.6ms
            // thin it will adjust display frame duration as 33.3ms.
            const float frameSpan = 1.0f / 60.0f * 1000f;
            var sampleSpan = m_sampleFrames * frameSpan;

            if (maxTime > sampleSpan)
                m_frameAdjust = Math.Max(0, m_frameAdjust) + 1;
            else
                m_frameAdjust = Math.Min(0, m_frameAdjust) - 1;

            if (Math.Abs(m_frameAdjust) > AutoAdjustDelay)
            {
                m_sampleFrames = Math.Min(MaxSampleFrames, m_sampleFrames);
                m_sampleFrames =
                    Math.Max(TargetSampleFrames, (int)(maxTime / frameSpan) + 1);

                m_frameAdjust = 0;
            }

            // Compute factor that converts from ms to pixel.
            var msToPs = width / sampleSpan;

            // Draw start position.
            var startY = (int)position.Y - (height - BarHeight);

            // Current y position.
            var y = startY;

            m_spriteBatch.Begin();

            // Draw transparency background.
            var rc = new Rectangle((int)position.X, y, width, height);
            m_spriteBatch.Draw(m_texture, rc, new Color(0, 0, 0, 128));

            // Draw markers for each bars.
            rc.Height = BarHeight;
            foreach (var bar in m_prevLog.Bars)
            {
                rc.Y = y + BarPadding;
                if (bar.MarkCount > 0)
                {
                    for (var j = 0; j < bar.MarkCount; ++j)
                    {
                        var bt = bar.Markers[j].BeginTime;
                        var et = bar.Markers[j].EndTime;
                        var sx = (int)(position.X + bt * msToPs);
                        var ex = (int)(position.X + et * msToPs);
                        rc.X = sx;
                        rc.Width = Math.Max(ex - sx, 1);

                        m_spriteBatch.Draw(m_texture, rc, bar.Markers[j].Color);
                    }
                }

                y += BarHeight + BarPadding;
            }

            // Draw grid lines.
            // Each grid represents ms.
            rc = new Rectangle((int)position.X, startY, 1, height);
            for (var t = 1.0f; t < sampleSpan; t += 1.0f)
            {
                rc.X = (int)(position.X + t * msToPs);
                m_spriteBatch.Draw(m_texture, rc, Color.Gray);
            }

            // Draw frame grid.
            for (var i = 0; i <= m_sampleFrames; ++i)
            {
                rc.X = (int)(position.X + frameSpan * i * msToPs);
                m_spriteBatch.Draw(m_texture, rc, Color.White);
            }

            // Draw log.
            if (ShowLog)
            {
                // Generate log string.
                y = startY - m_debugFont.LineSpacing;
                m_logString.Length = 0;
                foreach (var markerInfo in m_markers)
                {
                    for (var i = 0; i < MaxBars; ++i)
                    {
                        if (markerInfo.Logs[i].Initialized)
                        {
                            if (m_logString.Length > 0)
                                m_logString.Append("\n");

                            m_logString.Append(" Bar ");
                            m_logString.AppendNumber(i);
                            m_logString.Append(" ");
                            m_logString.Append(markerInfo.Name);

                            m_logString.Append(" Avg.:");
                            m_logString.AppendNumber(markerInfo.Logs[i].SnapAvg);
                            m_logString.Append("ms ");

                            y -= m_debugFont.LineSpacing;
                        }
                    }
                }

                // Compute background size and draw it.
                var size = m_debugFont.MeasureString(m_logString);
                rc = new Rectangle((int)position.X, y, (int)size.X + 12, (int)size.Y);
                m_spriteBatch.Draw(m_texture, rc, new Color(0, 0, 0, 128));

                // Draw log string.
                m_spriteBatch.DrawString(m_debugFont, m_logString,
                                        new Vector2(position.X + 12, y), Color.White);


                // Draw log color boxes.
                y += (int)(m_debugFont.LineSpacing * 0.3f);
                rc = new Rectangle((int)position.X + 4, y, 10, 10);
                var rc2 = new Rectangle((int)position.X + 5, y + 1, 8, 8);
                foreach (var markerInfo in m_markers)
                {
                    for (var i = 0; i < MaxBars; ++i)
                    {
                        if (markerInfo.Logs[i].Initialized)
                        {
                            rc.Y = y;
                            rc2.Y = y + 1;
                            m_spriteBatch.Draw(m_texture, rc, Color.White);
                            m_spriteBatch.Draw(m_texture, rc2, markerInfo.Logs[i].Color);

                            y += m_debugFont.LineSpacing;
                        }
                    }
                }


            }

            m_spriteBatch.End();
#endif
        }

        #endregion

    }
}
