namespace Supercell.Laser.Server.Logic.Game
{
    using Supercell.Laser.Logic.Battle;
    using System.Collections.Concurrent;

    public static class Battles
    {
        public static long m_battleIdCounter = 0;
        private static ConcurrentDictionary<long, LogicBattleModeServer> m_battles;

        public static void Init()
        {
            m_battles = new ConcurrentDictionary<long, LogicBattleModeServer>();
            m_battleIdCounter = 0;

            new Thread(Update).Start();
        }

        public static void Update()
        {
            while (true)
            {
                foreach (LogicBattleModeServer battle in m_battles.Values.ToArray())
                {
                    if (battle.IsGameOver)
                    {
                        m_battles.Remove(battle.Id, out _);
                    }
                }
                Thread.Sleep(1000);
            }
        }

        public static long Add(LogicBattleModeServer battle)
        {
            long id = ++m_battleIdCounter;
            m_battles[id] = battle;
            return id;
        }

        public static LogicBattleModeServer Get(long id)
        {
            if (!m_battles.ContainsKey(id)) return null;
            return m_battles[id];
        }
    }
}
