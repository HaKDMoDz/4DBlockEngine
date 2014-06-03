using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Common.Enums;
using _4DMonoEngine.Core.Common.Logging;
using _4DMonoEngine.Core.Debugging.Timing;

namespace _4DMonoEngine.Core
{
    public sealed class MainEngine
    {
        private static MainEngine s_engineInstance;
        private readonly LogManager m_logManager;
        private readonly Dictionary<Type, IAssetProvider> m_assetProviders;
        private Game m_game;

        public static MainEngine GetEngineInstance()
        {
            return s_engineInstance ?? (s_engineInstance = new MainEngine());
        }

        private MainEngine()
        {
            m_logManager = new LogManager(new List<LogTarget>
            {
                new ConsoleTarget(Logger.Level.Info, Logger.Level.Fatal, false),
                new FileTarget("EngineLog", Logger.Level.Trace, Logger.Level.PacketDump, true)
            });   
            m_assetProviders = new Dictionary<Type, IAssetProvider>();
        }

        public void Initialize(Game game)
        {
            m_game = game;

        }

        public Logger GetLogger(string name = null)
        {
            return m_logManager.GetOrCreateLogger(name);
        }

        public T GetAsset<T>(string assetId)
        {
            var result = default(T);
            if (m_assetProviders.ContainsKey(typeof (T)))
            {
                result = m_assetProviders[typeof (T)].Get<T>(assetId);
            }
            return result;
        }

        public void Exit()
        {
            m_game.Exit();
        }
    }
}
