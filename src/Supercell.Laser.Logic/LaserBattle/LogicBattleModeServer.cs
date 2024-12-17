namespace Supercell.Laser.Logic.Battle
{
    using Supercell.Laser.Logic.Battle.Component;
    using Supercell.Laser.Logic.Battle.Input;
    using Supercell.Laser.Logic.Battle.Level;
    using Supercell.Laser.Logic.Battle.Level.Factory;
    using Supercell.Laser.Logic.Battle.Objects;
    using Supercell.Laser.Logic.Battle.Structures;
    using Supercell.Laser.Logic.Csv;
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Logic.Data.Helper;
    using Supercell.Laser.Logic.Home;
    using Supercell.Laser.Logic.Home.Structures;
    using Supercell.Laser.Logic.Listener;
    using Supercell.Laser.Logic.Message.Account;
    using Supercell.Laser.Logic.Message.Account.Auth;
    using Supercell.Laser.Logic.Message.Battle;
    using Supercell.Laser.Logic.Time;
    using Supercell.Laser.Logic.Util;
    using Supercell.Laser.Titan.DataStream;
    using Supercell.Laser.Titan.Debug;
    using Supercell.Laser.Titan.Math;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public struct DiedEntry
    {
        public int DeathTick;
        public BattlePlayer Player;
    }

    public class LogicBattleModeServer
    {
        public const int ORBS_TO_COLLECT_NORMAL = 0xA;

        private Timer m_updateTimer;

        private int m_locationId;
        private int m_gameModeVariation;
        private int m_playersCountWithGameModeVariation;

        private Queue<ClientInput> m_inputQueue;

        public List<BattlePlayer> m_players;
        private Dictionary<long, BattlePlayer> m_playersBySessionId;
        private Dictionary<int, BattlePlayer> m_playersByObjectGlobalId;
        private Dictionary<long, LogicGameListener> m_spectators;
        private LogicGameObjectManagerServer m_gameObjectManager;

        private readonly LogicSkillData _logicSkillData;

        private readonly LogicSkillServer _logicSkill;

        private Rect m_playArea;
        private TileMap m_tileMap;
        private GameTime m_time;
        private LogicRandom m_random;
        private int m_randomSeed;

        private int m_winnerTeam;

        private int isBugStop;

        private Queue<DiedEntry> m_deadPlayers;
        private int m_playersAlive;

        private int m_gemGrabCountdown;

        public bool IsGameOver { get; private set; }

        public LogicBattleModeServer(int locationId)
        {
            m_winnerTeam = -1;

            m_locationId = locationId;
            m_gameModeVariation = GameModeUtil.GetGameModeVariation(Location.GameMode);
            m_playersCountWithGameModeVariation = GamePlayUtil.GetPlayerCountWithGameModeVariation(m_gameModeVariation);

            m_inputQueue = new Queue<ClientInput>();

            m_randomSeed = 0;
            m_random = new LogicRandom(m_randomSeed);

            m_players = new List<BattlePlayer>();
            m_playersBySessionId = new Dictionary<long, BattlePlayer>();
            m_playersByObjectGlobalId = new Dictionary<int, BattlePlayer>();
            m_deadPlayers = new Queue<DiedEntry>();

            m_time = new GameTime();
            m_tileMap = TileMapFactory.CreateTileMap(Location.AllowedMaps);
            m_playArea = new Rect(0, 0, m_tileMap.LogicWidth, m_tileMap.LogicHeight);
            m_gameObjectManager = new LogicGameObjectManagerServer(this);

            m_spectators = new Dictionary<long, LogicGameListener>();

            Console.WriteLine(m_gameModeVariation);
        }

        public void CreateOBJCharacter(string name, int x, int y, int z)
        {
            CharacterData CharacterData = DataTables.Get(16).GetData<CharacterData>(name);
            LogicCharacterServer Character = new LogicCharacterServer(this, 16, CharacterData.GetInstanceId());
            Character.SetPosition(x, y, z);
            Character.SetIndex(10 * 16);
            m_gameObjectManager.AddGameObject(Character);
        }

        public void CreateOBJItem(string name, int x, int tyTYTYWTYWTIIYWOPJETIYWJETIOYWJ)
        {
            // лень 
        }

        public int GetGemGrabCountdown()
        {
            return m_gemGrabCountdown;
        }

        public int GetPlayersAliveCountForBattleRoyale()
        {
            return m_playersAlive;
        }

        private async void TickSpawnHeroes()
        {
            DiedEntry[] entries = m_deadPlayers.ToArray();
            m_deadPlayers.Clear();

            foreach (DiedEntry entry in entries)
            {
                if (GetTicksGone() - entry.DeathTick < GameModeUtil.GetRespawnSeconds(m_gameModeVariation) * 2)
                {
                    m_deadPlayers.Enqueue(entry);
                    continue;
                }

                BattlePlayer player = entry.Player;
                LogicVector2 spawnPoint = player.GetSpawnPoint();

                await Task.Delay(GameModeUtil.GetRespwanSecondsForTask(m_gameModeVariation));
                {
                    AreaEffectData areaEffect = DataTables.Get(17).GetData<AreaEffectData>("HeroSpawn");
                    LogicAreaEffectServer areaEffectObject = new LogicAreaEffectServer(this, 17, areaEffect.GetInstanceId());
                    areaEffectObject.SetPosition(spawnPoint.X, spawnPoint.Y, 0);
                    areaEffectObject.SetIndex(player.PlayerIndex + (16 * player.TeamIndex));
                    m_gameObjectManager.AddGameObject(areaEffectObject); // spawn белый презик я хз
                }


                LogicCharacterServer character = new LogicCharacterServer(this, 16, GlobalId.GetInstanceId(player.CharacterId));
                character.SetIndex(player.PlayerIndex + (16 * player.TeamIndex));
                character.SetHeroLevel(9);// player.HeroPowerLevel);
                character.SetPosition(spawnPoint.X, spawnPoint.Y, 0);
                character.SetBot(player.IsBot());
                character.SetImmunity(60, 100);
                await Task.Delay(2000);
                m_gameObjectManager.AddGameObject(character);
                player.OwnObjectId = character.GetGlobalID();
                m_playersByObjectGlobalId.Add(player.OwnObjectId, player);
            }
        }

        public void PlayerDied(BattlePlayer player)
        {
            if (m_gameModeVariation == 6)
            {
                int rank = m_playersAlive;
                m_playersAlive--;
                player.IsAlive = false;
                player.BattleRoyaleRank = rank;
                new DisconnectedMessage();

                BattleEndMessage message = new BattleEndMessage();
                message.GameMode = 2;
                message.IsPvP = true;
                message.Players = new List<BattlePlayer>();
                message.Players.Add(player);
                message.OwnPlayer = player;

                if (player.Avatar == null) return;
                player.Avatar.BattleId = -1;

                if (player.GameListener == null) return;

                Hero hero = player.Avatar.GetHero(player.CharacterId);

                message.Result = rank;
                int tokensReward = 40 / rank;
                message.TokensReward = tokensReward;
                Console.WriteLine("[BattleMode] Result " + message.Result);
                bool isWin = false;

                if (rank == 1 || rank == 2 || rank == 3 || rank == 4)
                {
                    isWin = true;
                }

                if (message.OwnPlayer.Trophies >= 0 && message.OwnPlayer.Trophies <= 49)
                {
                    if (message.Result == 1)
                    {
                        message.TrophiesReward = 1000;
                        message.TokensReward = 35;

                        player.Home.Exp += 15;
                        player.Avatar.SoloWins += 1;
                    }
                    if (message.Result == 2)
                    {
                        message.TrophiesReward = 8000;
                        message.TokensReward = 28;

                        player.Home.Exp += 12;
                    }
                    if (message.Result == 3)
                    {
                        message.TrophiesReward = 7;
                        message.TokensReward = 22;

                        player.Home.Exp += 9;
                    }
                    if (message.Result == 4)
                    {
                        message.TrophiesReward = 6;
                        message.TokensReward = 16;

                        player.Home.Exp += 6;
                    }
                    if (message.Result == 5)
                    {
                        message.TrophiesReward = 4;
                        message.TokensReward = 12;

                        player.Home.Exp += 5;
                    }
                    if (message.Result == 6)
                    {
                        message.TrophiesReward = 2;
                        message.TokensReward = 8;

                        player.Home.Exp += 4;
                    }
                    if (message.Result == 7)
                    {
                        message.TrophiesReward = 2;
                        message.TokensReward = 6;

                        player.Home.Exp += 3;
                    }
                    if (message.Result == 8)
                    {
                        message.TrophiesReward = 1;
                        message.TokensReward = 4;

                        player.Home.Exp += 2;
                    }
                    if (message.Result == 9)
                    {
                        message.TrophiesReward = 0;
                        message.TokensReward = 2;

                        player.Home.Exp += 1;
                    }
                    if (message.Result == 10)
                    {
                        message.TrophiesReward = 0;
                        message.TokensReward = 0;

                        player.Home.Exp += 0;
                    }
                }

                if (message.OwnPlayer.Trophies >= 50 && message.OwnPlayer.Trophies <= 99)
                {
                    if (message.Result == 1)
                    {
                        message.TrophiesReward = 10;
                        message.TokensReward = 35;

                        player.Home.Exp += 15;
                        player.Avatar.SoloWins += 1;
                    }
                    if (message.Result == 2)
                    {
                        message.TrophiesReward = 8;
                        message.TokensReward = 28;

                        player.Home.Exp += 12;
                    }
                    if (message.Result == 3)
                    {
                        message.TrophiesReward = 7;
                        message.TokensReward = 22;

                        player.Home.Exp += 9;
                    }
                    if (message.Result == 4)
                    {
                        message.TrophiesReward = 6;
                        message.TokensReward = 16;

                        player.Home.Exp += 6;
                    }
                    if (message.Result == 5)
                    {
                        message.TrophiesReward = 3;
                        message.TokensReward = 12;

                        player.Home.Exp += 5;
                    }
                    if (message.Result == 6)
                    {
                        message.TrophiesReward = 2;
                        message.TokensReward = 8;

                        player.Home.Exp += 4;
                    }
                    if (message.Result == 7)
                    {
                        message.TrophiesReward = 2;
                        message.TokensReward = 6;

                        player.Home.Exp += 3;
                    }
                    if (message.Result == 8)
                    {
                        message.TrophiesReward = 0;
                        message.TokensReward = 4;

                        player.Home.Exp += 2;
                    }
                    if (message.Result == 9)
                    {
                        message.TrophiesReward = -1;
                        message.TokensReward = 2;

                        player.Home.Exp += 1;
                    }
                    if (message.Result == 10)
                    {
                        message.TrophiesReward = -2;
                        message.TokensReward = 0;

                        player.Home.Exp += 0;
                    }
                }
                if (message.OwnPlayer.Trophies >= 100 && message.OwnPlayer.Trophies <= 199)
                {
                    if (message.Result == 1)
                    {
                        message.TrophiesReward = 10;
                        message.TokensReward = 35;

                        player.Home.Exp += 15;
                        player.Avatar.SoloWins += 1;
                    }
                    if (message.Result == 2)
                    {
                        message.TrophiesReward = 8;
                        message.TokensReward = 28;

                        player.Home.Exp += 12;
                    }
                    if (message.Result == 3)
                    {
                        message.TrophiesReward = 7;
                        message.TokensReward = 22;

                        player.Home.Exp += 9;
                    }
                    if (message.Result == 4)
                    {
                        message.TrophiesReward = 6;
                        message.TokensReward = 16;

                        player.Home.Exp += 6;
                    }
                    if (message.Result == 5)
                    {
                        message.TrophiesReward = 3;
                        message.TokensReward = 12;

                        player.Home.Exp += 5;
                    }
                    if (message.Result == 6)
                    {
                        message.TrophiesReward = 1;
                        message.TokensReward = 8;

                        player.Home.Exp += 4;
                    }
                    if (message.Result == 7)
                    {
                        message.TrophiesReward = 0;
                        message.TokensReward = 6;

                        player.Home.Exp += 3;
                    }
                    if (message.Result == 8)
                    {
                        message.TrophiesReward = -1;
                        message.TokensReward = 4;

                        player.Home.Exp += 2;
                    }
                    if (message.Result == 9)
                    {
                        message.TrophiesReward = -2;
                        message.TokensReward = 2;

                        player.Home.Exp += 1;
                    }
                    if (message.Result == 10)
                    {
                        message.TrophiesReward = -2;
                        message.TokensReward = 0;

                        player.Home.Exp += 0;
                    }
                }

                if (message.OwnPlayer.Trophies >= 200 && message.OwnPlayer.Trophies <= 299)
                {
                    if (message.Result == 1)
                    {
                        message.TrophiesReward = 10;
                        message.TokensReward = 35;

                        player.Home.Exp += 15;
                        player.Avatar.SoloWins += 1;
                    }
                    if (message.Result == 2)
                    {
                        message.TrophiesReward = 8;
                        message.TokensReward = 28;

                        player.Home.Exp += 12;
                    }
                    if (message.Result == 3)
                    {
                        message.TrophiesReward = 6;
                        message.TokensReward = 22;

                        player.Home.Exp += 9;
                    }
                    if (message.Result == 4)
                    {
                        message.TrophiesReward = 5;
                        message.TokensReward = 16;

                        player.Home.Exp += 6;
                    }
                    if (message.Result == 5)
                    {
                        message.TrophiesReward = 3;
                        message.TokensReward = 12;

                        player.Home.Exp += 5;
                    }
                    if (message.Result == 6)
                    {
                        message.TrophiesReward = 1;
                        message.TokensReward = 8;

                        player.Home.Exp += 4;
                    }
                    if (message.Result == 7)
                    {
                        message.TrophiesReward = 0;
                        message.TokensReward = 6;

                        player.Home.Exp += 3;
                    }
                    if (message.Result == 8)
                    {
                        message.TrophiesReward = -2;
                        message.TokensReward = 4;

                        player.Home.Exp += 2;
                    }
                    if (message.Result == 9)
                    {
                        message.TrophiesReward = -3;
                        message.TokensReward = 2;

                        player.Home.Exp += 1;
                    }
                    if (message.Result == 10)
                    {
                        message.TrophiesReward = -3;
                        message.TokensReward = 0;

                        player.Home.Exp += 0;
                    }
                }

                if (message.OwnPlayer.Trophies >= 300 && message.OwnPlayer.Trophies <= 399)
                {
                    if (message.Result == 1)
                    {
                        message.TrophiesReward = 10;
                        message.TokensReward = 35;

                        player.Home.Exp += 15;
                        player.Avatar.SoloWins += 1;
                    }
                    if (message.Result == 2)
                    {
                        message.TrophiesReward = 8;
                        message.TokensReward = 28;

                        player.Home.Exp += 12;
                    }
                    if (message.Result == 3)
                    {
                        message.TrophiesReward = 6;
                        message.TokensReward = 22;

                        player.Home.Exp += 9;
                    }
                    if (message.Result == 4)
                    {
                        message.TrophiesReward = 5;
                        message.TokensReward = 16;

                        player.Home.Exp += 6;
                    }
                    if (message.Result == 5)
                    {
                        message.TrophiesReward = 2;
                        message.TokensReward = 12;

                        player.Home.Exp += 5;
                    }
                    if (message.Result == 6)
                    {
                        message.TrophiesReward = 0;
                        message.TokensReward = 8;

                        player.Home.Exp += 4;
                    }
                    if (message.Result == 7)
                    {
                        message.TrophiesReward = 0;
                        message.TokensReward = 6;

                        player.Home.Exp += 3;
                    }
                    if (message.Result == 8)
                    {
                        message.TrophiesReward = -3;
                        message.TokensReward = 4;

                        player.Home.Exp += 2;
                    }
                    if (message.Result == 9)
                    {
                        message.TrophiesReward = -4;
                        message.TokensReward = 2;

                        player.Home.Exp += 1;
                    }
                    if (message.Result == 10)
                    {
                        message.TrophiesReward = -4;
                        message.TokensReward = 0;

                        player.Home.Exp += 0;
                    }
                }

                if (message.OwnPlayer.Trophies >= 400 && message.OwnPlayer.Trophies <= 499)
                {
                    if (message.Result == 1)
                    {
                        message.TrophiesReward = 10;
                        message.TokensReward = 35;

                        player.Home.Exp += 15;
                        player.Avatar.SoloWins += 1;
                    }
                    if (message.Result == 2)
                    {
                        message.TrophiesReward = 8;
                        message.TokensReward = 28;

                        player.Home.Exp += 12;
                    }
                    if (message.Result == 3)
                    {
                        message.TrophiesReward = 6;
                        message.TokensReward = 22;

                        player.Home.Exp += 9;
                    }
                    if (message.Result == 4)
                    {
                        message.TrophiesReward = 5;
                        message.TokensReward = 16;

                        player.Home.Exp += 6;
                    }
                    if (message.Result == 5)
                    {
                        message.TrophiesReward = 2;
                        message.TokensReward = 12;

                        player.Home.Exp += 5;
                    }
                    if (message.Result == 6)
                    {
                        message.TrophiesReward = -1;
                        message.TokensReward = 8;

                        player.Home.Exp += 4;
                    }
                    if (message.Result == 7)
                    {
                        message.TrophiesReward = -2;
                        message.TokensReward = 6;

                        player.Home.Exp += 3;
                    }
                    if (message.Result == 8)
                    {
                        message.TrophiesReward = -3;
                        message.TokensReward = 4;

                        player.Home.Exp += 2;
                    }
                    if (message.Result == 9)
                    {
                        message.TrophiesReward = -5;
                        message.TokensReward = 2;

                        player.Home.Exp += 1;
                    }
                    if (message.Result == 10)
                    {
                        message.TrophiesReward = -5;
                        message.TokensReward = 0;

                        player.Home.Exp += 0;
                    }
                }

                if (message.OwnPlayer.Trophies >= 500 && message.OwnPlayer.Trophies <= 599)
                {
                    if (message.Result == 1)
                    {
                        message.TrophiesReward = 10;
                        message.TokensReward = 35;

                        player.Home.Exp += 15;
                        player.Avatar.SoloWins += 1;
                    }
                    if (message.Result == 2)
                    {
                        message.TrophiesReward = 8;
                        message.TokensReward = 28;

                        player.Home.Exp += 12;
                    }
                    if (message.Result == 3)
                    {
                        message.TrophiesReward = 6;
                        message.TokensReward = 22;

                        player.Home.Exp += 9;
                    }
                    if (message.Result == 4)
                    {
                        message.TrophiesReward = 4;
                        message.TokensReward = 16;

                        player.Home.Exp += 6;
                    }
                    if (message.Result == 5)
                    {
                        message.TrophiesReward = 2;
                        message.TokensReward = 12;

                        player.Home.Exp += 5;
                    }
                    if (message.Result == 6)
                    {
                        message.TrophiesReward = -1;
                        message.TokensReward = 8;

                        player.Home.Exp += 4;
                    }
                    if (message.Result == 7)
                    {
                        message.TrophiesReward = -2;
                        message.TokensReward = 6;

                        player.Home.Exp += 3;
                    }
                    if (message.Result == 8)
                    {
                        message.TrophiesReward = -5;
                        message.TokensReward = 4;

                        player.Home.Exp += 2;
                    }
                    if (message.Result == 9)
                    {
                        message.TrophiesReward = -6;
                        message.TokensReward = 2;

                        player.Home.Exp += 1;
                    }
                    if (message.Result == 10)
                    {
                        message.TrophiesReward = -6;
                        message.TokensReward = 0;

                        player.Home.Exp += 0;
                    }
                }

                if (message.OwnPlayer.Trophies >= 600 && message.OwnPlayer.Trophies <= 699)
                {
                    if (message.Result == 1)
                    {
                        message.TrophiesReward = 10;
                        message.TokensReward = 35;

                        player.Home.Exp += 15;
                        player.Avatar.SoloWins += 1;
                    }
                    if (message.Result == 2)
                    {
                        message.TrophiesReward = 8;
                        message.TokensReward = 28;

                        player.Home.Exp += 12;
                    }
                    if (message.Result == 3)
                    {
                        message.TrophiesReward = 6;
                        message.TokensReward = 22;

                        player.Home.Exp += 9;
                    }
                    if (message.Result == 4)
                    {
                        message.TrophiesReward = 4;
                        message.TokensReward = 16;

                        player.Home.Exp += 6;
                    }
                    if (message.Result == 5)
                    {
                        message.TrophiesReward = 1;
                        message.TokensReward = 12;

                        player.Home.Exp += 5;
                    }
                    if (message.Result == 6)
                    {
                        message.TrophiesReward = -2;
                        message.TokensReward = 8;

                        player.Home.Exp += 4;
                    }
                    if (message.Result == 7)
                    {
                        message.TrophiesReward = -2;
                        message.TokensReward = 6;

                        player.Home.Exp += 3;
                    }
                    if (message.Result == 8)
                    {
                        message.TrophiesReward = -5;
                        message.TokensReward = 4;

                        player.Home.Exp += 2;
                    }
                    if (message.Result == 9)
                    {
                        message.TrophiesReward = -7;
                        message.TokensReward = 2;

                        player.Home.Exp += 1;
                    }
                    if (message.Result == 10)
                    {
                        message.TrophiesReward = -8;
                        message.TokensReward = 0;

                        player.Home.Exp += 0;
                    }
                }

                if (message.OwnPlayer.Trophies >= 700 && message.OwnPlayer.Trophies <= 799)
                {
                    if (message.Result == 1)
                    {
                        message.TrophiesReward = 10;
                        message.TokensReward = 35;

                        player.Home.Exp += 15;
                        player.Avatar.SoloWins += 1;
                    }
                    if (message.Result == 2)
                    {
                        message.TrophiesReward = 8;
                        message.TokensReward = 28;

                        player.Home.Exp += 12;
                    }
                    if (message.Result == 3)
                    {
                        message.TrophiesReward = 6;
                        message.TokensReward = 22;

                        player.Home.Exp += 9;
                    }
                    if (message.Result == 4)
                    {
                        message.TrophiesReward = 4;
                        message.TokensReward = 16;

                        player.Home.Exp += 6;
                    }
                    if (message.Result == 5)
                    {
                        message.TrophiesReward = 1;
                        message.TokensReward = 12;

                        player.Home.Exp += 5;
                    }
                    if (message.Result == 6)
                    {
                        message.TrophiesReward = -3;
                        message.TokensReward = 8;

                        player.Home.Exp += 4;
                    }
                    if (message.Result == 7)
                    {
                        message.TrophiesReward = -4;
                        message.TokensReward = 6;

                        player.Home.Exp += 3;
                    }
                    if (message.Result == 8)
                    {
                        message.TrophiesReward = -5;
                        message.TokensReward = 4;

                        player.Home.Exp += 2;
                    }
                    if (message.Result == 9)
                    {
                        message.TrophiesReward = -8;
                        message.TokensReward = 2;

                        player.Home.Exp += 1;
                    }
                    if (message.Result == 10)
                    {
                        message.TrophiesReward = -9;
                        message.TokensReward = 0;

                        player.Home.Exp += 0;
                    }
                }

                if (message.OwnPlayer.Trophies >= 800 && message.OwnPlayer.Trophies <= 899)
                {
                    if (message.Result == 1)
                    {
                        message.TrophiesReward = 9;
                        message.TokensReward = 35;

                        player.Home.Exp += 15;
                        player.Avatar.SoloWins += 1;
                    }
                    if (message.Result == 2)
                    {
                        message.TrophiesReward = 7;
                        message.TokensReward = 28;

                        player.Home.Exp += 12;
                    }
                    if (message.Result == 3)
                    {
                        message.TrophiesReward = 5;
                        message.TokensReward = 22;

                        player.Home.Exp += 9;
                    }
                    if (message.Result == 4)
                    {
                        message.TrophiesReward = 2;
                        message.TokensReward = 16;

                        player.Home.Exp += 6;
                    }
                    if (message.Result == 5)
                    {
                        message.TrophiesReward = 0;
                        message.TokensReward = 12;

                        player.Home.Exp += 5;
                    }
                    if (message.Result == 6)
                    {
                        message.TrophiesReward = -3;
                        message.TokensReward = 8;

                        player.Home.Exp += 4;
                    }
                    if (message.Result == 7)
                    {
                        message.TrophiesReward = -4;
                        message.TokensReward = 6;

                        player.Home.Exp += 3;
                    }
                    if (message.Result == 8)
                    {
                        message.TrophiesReward = -7;
                        message.TokensReward = 4;

                        player.Home.Exp += 2;
                    }
                    if (message.Result == 9)
                    {
                        message.TrophiesReward = -9;
                        message.TokensReward = 2;

                        player.Home.Exp += 1;
                    }
                    if (message.Result == 10)
                    {
                        message.TrophiesReward = -10;
                        message.TokensReward = 0;

                        player.Home.Exp += 0;
                    }
                }

                if (message.OwnPlayer.Trophies >= 900 && message.OwnPlayer.Trophies <= 999)
                {
                    if (message.Result == 1)
                    {
                        message.TrophiesReward = 8;
                        message.TokensReward = 35;

                        player.Home.Exp += 15;
                        player.Avatar.SoloWins += 1;
                    }
                    if (message.Result == 2)
                    {
                        message.TrophiesReward = 6;
                        message.TokensReward = 28;

                        player.Home.Exp += 12;
                    }
                    if (message.Result == 3)
                    {
                        message.TrophiesReward = 4;
                        message.TokensReward = 22;

                        player.Home.Exp += 9;
                    }
                    if (message.Result == 4)
                    {
                        message.TrophiesReward = 1;
                        message.TokensReward = 16;

                        player.Home.Exp += 6;
                    }
                    if (message.Result == 5)
                    {
                        message.TrophiesReward = -1;
                        message.TokensReward = 12;

                        player.Home.Exp += 5;
                    }
                    if (message.Result == 6)
                    {
                        message.TrophiesReward = -3;
                        message.TokensReward = 8;

                        player.Home.Exp += 4;
                    }
                    if (message.Result == 7)
                    {
                        message.TrophiesReward = -6;
                        message.TokensReward = 6;

                        player.Home.Exp += 3;
                    }
                    if (message.Result == 8)
                    {
                        message.TrophiesReward = -8;
                        message.TokensReward = 4;

                        player.Home.Exp += 2;
                    }
                    if (message.Result == 9)
                    {
                        message.TrophiesReward = -10;
                        message.TokensReward = 2;

                        player.Home.Exp += 1;
                    }
                    if (message.Result == 10)
                    {
                        message.TrophiesReward = -11;
                        message.TokensReward = 0;

                        player.Home.Exp += 0;
                    }
                }

                if (message.OwnPlayer.Trophies >= 1000 && message.OwnPlayer.Trophies <= 1099)
                {
                    if (message.Result == 1)
                    {
                        message.TrophiesReward = 6;
                        message.TokensReward = 35;

                        player.Home.Exp += 15;
                        player.Avatar.SoloWins += 1;
                    }
                    if (message.Result == 2)
                    {
                        message.TrophiesReward = 5;
                        message.TokensReward = 28;

                        player.Home.Exp += 12;
                    }
                    if (message.Result == 3)
                    {
                        message.TrophiesReward = 3;
                        message.TokensReward = 22;

                        player.Home.Exp += 9;
                    }
                    if (message.Result == 4)
                    {
                        message.TrophiesReward = 1;
                        message.TokensReward = 16;

                        player.Home.Exp += 6;
                    }
                    if (message.Result == 5)
                    {
                        message.TrophiesReward = -2;
                        message.TokensReward = 12;

                        player.Home.Exp += 5;
                    }
                    if (message.Result == 6)
                    {
                        message.TrophiesReward = -5;
                        message.TokensReward = 8;

                        player.Home.Exp += 4;
                    }
                    if (message.Result == 7)
                    {
                        message.TrophiesReward = -6;
                        message.TokensReward = 6;

                        player.Home.Exp += 3;
                    }
                    if (message.Result == 8)
                    {
                        message.TrophiesReward = -9;
                        message.TokensReward = 4;

                        player.Home.Exp += 2;
                    }
                    if (message.Result == 9)
                    {
                        message.TrophiesReward = -11;
                        message.TokensReward = 2;

                        player.Home.Exp += 1;
                    }
                    if (message.Result == 10)
                    {
                        message.TrophiesReward = -12;
                        message.TokensReward = 0;

                        player.Home.Exp += 0;
                    }
                }

                if (message.OwnPlayer.Trophies >= 1100 && message.OwnPlayer.Trophies <= 1199)
                {
                    if (message.Result == 1)
                    {
                        message.TrophiesReward = 5;
                        message.TokensReward = 35;

                        player.Home.Exp += 15;
                        player.Avatar.SoloWins += 1;
                    }
                    if (message.Result == 2)
                    {
                        message.TrophiesReward = 4;
                        message.TokensReward = 28;

                        player.Home.Exp += 12;
                    }
                    if (message.Result == 3)
                    {
                        message.TrophiesReward = 1;
                        message.TokensReward = 22;

                        player.Home.Exp += 9;
                    }
                    if (message.Result == 4)
                    {
                        message.TrophiesReward = 0;
                        message.TokensReward = 16;

                        player.Home.Exp += 6;
                    }
                    if (message.Result == 5)
                    {
                        message.TrophiesReward = -2;
                        message.TokensReward = 12;

                        player.Home.Exp += 5;
                    }
                    if (message.Result == 6)
                    {
                        message.TrophiesReward = -6;
                        message.TokensReward = 8;

                        player.Home.Exp += 4;
                    }
                    if (message.Result == 7)
                    {
                        message.TrophiesReward = -7;
                        message.TokensReward = 6;

                        player.Home.Exp += 3;
                    }
                    if (message.Result == 8)
                    {
                        message.TrophiesReward = -10;
                        message.TokensReward = 4;

                        player.Home.Exp += 2;
                    }
                    if (message.Result == 9)
                    {
                        message.TrophiesReward = -12;
                        message.TokensReward = 2;

                        player.Home.Exp += 1;
                    }
                    if (message.Result == 10)
                    {
                        message.TrophiesReward = -13;
                        message.TokensReward = 0;

                        player.Home.Exp += 0;
                    }
                }

                if (message.OwnPlayer.Trophies >= 1200)
                {
                    if (message.Result == 1)
                    {
                        message.TrophiesReward = 5;
                        message.TokensReward = 35;

                        player.Home.Exp += 15;
                        player.Avatar.SoloWins += 1;
                    }
                    if (message.Result == 2)
                    {
                        message.TrophiesReward = 3;
                        message.TokensReward = 28;

                        player.Home.Exp += 12;
                    }
                    if (message.Result == 3)
                    {
                        message.TrophiesReward = 0;
                        message.TokensReward = 22;

                        player.Home.Exp += 9;
                    }
                    if (message.Result == 4)
                    {
                        message.TrophiesReward = -1;
                        message.TokensReward = 16;

                        player.Home.Exp += 6;
                    }
                    if (message.Result == 5)
                    {
                        message.TrophiesReward = -2;
                        message.TokensReward = 12;

                        player.Home.Exp += 5;
                    }
                    if (message.Result == 6)
                    {
                        message.TrophiesReward = -6;
                        message.TokensReward = 8;

                        player.Home.Exp += 4;
                    }
                    if (message.Result == 7)
                    {
                        message.TrophiesReward = -8;
                        message.TokensReward = 6;

                        player.Home.Exp += 3;
                    }
                    if (message.Result == 8)
                    {
                        message.TrophiesReward = -11;
                        message.TokensReward = 4;

                        player.Home.Exp += 2;
                    }
                    if (message.Result == 9)
                    {
                        message.TrophiesReward = -12;
                        message.TokensReward = 2;

                        player.Home.Exp += 1;
                    }
                    if (message.Result == 10)
                    {
                        message.TrophiesReward = -13;
                        message.TokensReward = 0;

                        player.Home.Exp += 0;
                    }
                }

                player.Avatar.AddTokens(message.TokensReward);

                try
                {
                    HomeMode homeMode = LogicServerListener.Instance.GetHomeMode(player.AccountId);
                    //message.ProgressiveQuests = homeMode.Home.Quests.UpdateQuestsProgress(m_gameModeVariation, player.CharacterId, player.Kills, player.Damage, player.Heals, homeMode.Home);
                }
                catch
                {
                    Console.WriteLine("Yeah");
                }

                hero.AddTrophies(message.TrophiesReward);


                player.GameListener.SendTCPMessage(message);

                return;
            }

            if (m_gameModeVariation == 0)
            {
                player.ResetScore();
            }

            player.OwnObjectId = 0;

            DiedEntry entry = new DiedEntry();
            entry.Player = player;
            entry.DeathTick = GetTicksGone();

            m_deadPlayers.Enqueue(entry);
        }

        public BattlePlayer GetPlayerWithObject(int globalId)
        {
            if (m_playersByObjectGlobalId.ContainsKey(globalId))
            {
                return m_playersByObjectGlobalId[globalId];
            }
            return null;
        }

        public TileMap GetTileMap()
        {
            return m_tileMap;
        }

        public void Start()
        {
            m_updateTimer = new Timer(new TimerCallback(Update), null, 0, 1000 / 20);
        }

        public void Update(object stateInfo)
        {

            this.ExecuteOneTick();
        }

        public BattlePlayer GetPlayer(int globalId)
        {
            if (m_playersByObjectGlobalId.ContainsKey(globalId))
            {
                return m_playersByObjectGlobalId[globalId];
            }
            return null;
        }

        public void AddSpectator(long sessionId, LogicGameListener gameListener)
        {
            m_spectators.Add(sessionId, gameListener);
        }

        public void ChangePlayerSessionId(long old, long newId)
        {
            if (m_playersBySessionId.ContainsKey(old))
            {
                BattlePlayer player = m_playersBySessionId[old];
                player.LastHandledInput = 0;
                m_playersBySessionId.Remove(old);
                m_playersBySessionId.Add(newId, player);
            }
        }

        public BattlePlayer GetPlayerBySessionId(long sessionId)
        {
            if (m_playersBySessionId.ContainsKey(sessionId))
            {
                return m_playersBySessionId[sessionId];
            }
            return null;
        }

        public void AddPlayer(BattlePlayer player, long sessionId)
        {
            if (Debugger.DoAssert(player != null, "LogicBattle::AddPlayer - player is NULL!"))
            {
                player.SessionId = sessionId;
                m_players.Add(player);
                if (sessionId > 0)
                {
                    m_playersBySessionId.Add(sessionId, player);
                }
                if (player.Avatar != null)
                {
                    player.Avatar.BattleId = Id;
                    player.Avatar.TeamIndex = player.TeamIndex;
                    player.Avatar.OwnIndex = player.PlayerIndex;
                }
            }
        }

        public async void AddGameObjects()
        {
            m_playersAlive = m_players.Count;


            int team1Indexer = 0;
            int team2Indexer = 0;

            foreach (BattlePlayer player in m_players)
            {
                try
                {
                    LogicCharacterServer character = new LogicCharacterServer(this, 16, GlobalId.GetInstanceId(player.CharacterId));
                    character.SetIndex(player.PlayerIndex + (16 * player.TeamIndex));
                    character.SetHeroLevel(player.HeroPowerLevel);
                    character.SetBot(player.IsBot());
                    character.SetImmunity(60, 100);

                    if (GameModeUtil.HasTwoTeams(m_gameModeVariation))
                    {
                        if (player.TeamIndex == 0)
                        {
                            Tile tile = m_tileMap.SpawnPointsTeam1[team1Indexer++];
                            character.SetPosition(tile.X, tile.Y, 0);
                            player.SetSpawnPoint(tile.X, tile.Y);
                        }
                        else
                        {
                            try
                            {
                                Tile tile = m_tileMap.SpawnPointsTeam2[team2Indexer++];
                                character.SetPosition(tile.X, tile.Y, 0);
                                player.SetSpawnPoint(tile.X, tile.Y);
                            }
                            catch (Exception)
                            {
                                foreach (BattlePlayer player1 in GetPlayers())
                                {
                                    AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
                                    loginFailed.ErrorCode = 1;
                                    loginFailed.Message = "Сannot init battle\nPlease rejoin in game.";
                                    player1.GameListener.SendTCPMessage(loginFailed);
                                }
                            }

                        }
                    }
                    else
                    {
                        Tile tile = m_tileMap.SpawnPointsTeam1[team1Indexer++];
                        character.SetPosition(tile.X, tile.Y, 0);
                        player.SetSpawnPoint(tile.X, tile.Y);
                    }

                    m_gameObjectManager.AddGameObject(character);
                    player.OwnObjectId = character.GetGlobalID();
                    m_playersByObjectGlobalId.Add(player.OwnObjectId, player);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Cannot spawn character: " + ex);
                    EndBattleError(43);
                }
            }

            if (m_gameModeVariation == 0) // gemgrab
            {
                ItemData data = DataTables.Get(18).GetData<ItemData>("OrbSpawner");
                LogicItemServer item = new LogicItemServer(18, data.GetInstanceId());
                item.SetPosition(3000, 5000, 0);
                item.DisableAppearAnimation();
                m_gameObjectManager.AddGameObject(item);
            }

            if (m_gameModeVariation == 3) // bounty
            {
                ItemData data = DataTables.Get(18).GetData<ItemData>("Money");
                LogicItemServer item = new LogicItemServer(18, data.GetInstanceId());
                item.SetPosition(3000, 5000, 0);
                item.DisableAppearAnimation();
                m_gameObjectManager.AddGameObject(item);
            }

            if (m_gameModeVariation == 15)
            {
                ItemData data = DataTables.Get(18).GetData<ItemData>("Money");
                LogicItemServer item = new LogicItemServer(18, data.GetInstanceId());
                item.SetPosition(3000, 5000, 0);
                item.DisableAppearAnimation();
                m_gameObjectManager.AddGameObject(item);
            }

            if (m_gameModeVariation == 6 || m_gameModeVariation == 9) // sd parnoe i solo
            {
                CharacterData data = DataTables.Get(16).GetData<CharacterData>("LootBox");
                for (int i = 0; i < m_tileMap.Height; i++)
                {
                    for (int j = 0; j < m_tileMap.Width; j++)
                    {
                        Tile tile = m_tileMap.GetTile(i, j, true);
                        if (tile.Code == '4')
                        {
                            bool shouldSpawnBox = GetRandomInt(0, 120) < 60;

                            if (shouldSpawnBox)
                            {
                                LogicCharacterServer box = new LogicCharacterServer(this, 16, data.GetInstanceId());
                                box.SetPosition(tile.X, tile.Y, 0);
                                box.SetIndex(10 * 16);
                                m_gameObjectManager.AddGameObject(box);
                            }
                        }
                    }
                }
            }
            if (m_gameModeVariation == 2) // heist
            {
                CharacterData safe2 = DataTables.Get(16).GetData<CharacterData>("Safe");
                LogicCharacterServer safe3 = new LogicCharacterServer(this, safe2.GetClassId(), safe2.GetInstanceId());
                safe3.SetPosition(3000, 5000, 0);
                safe3.SetIndex(1 + (16 * 1));
                m_gameObjectManager.AddGameObject(safe3);

                CharacterData safe = DataTables.Get(16).GetData<CharacterData>("Safe");
                LogicCharacterServer safe1 = new LogicCharacterServer(this, safe.GetClassId(), safe.GetInstanceId());
                safe1.SetPosition(3000, 5000, 0);
                safe1.SetIndex(0 + (16 * 0));
                m_gameObjectManager.AddGameObject(safe1);

            }
            if (m_gameModeVariation == 5) // footbrawl
            {
                CharacterData safe12 = DataTables.Get(16).GetData<CharacterData>("LaserBall");
                LogicCharacterServer ball = new LogicCharacterServer(this, safe12.GetClassId(), safe12.GetInstanceId());
                ball.SetPosition(3000, 5000, 0);
                m_gameObjectManager.AddGameObject(ball);


            }

            if (m_gameModeVariation == 17)
            {
                ItemData data4 = DataTables.Get(18).GetData<ItemData>("ScorePole");
                LogicItemServer item4 = new LogicItemServer(18, data4.GetInstanceId());
                item4.SetPosition(3000, 5000, 0);
                item4.SetAngle(m_gameObjectManager.GetBattle().GetRandomInt(0, 360));
                m_gameObjectManager.AddGameObject(item4);


                ItemData data22 = DataTables.Get(18).GetData<ItemData>("ScoreFlag");
                LogicItemServer item11 = new LogicItemServer(18, data22.GetInstanceId());
                item11.SetPosition(3000, 5000, 0);
                item11.SetAngle(m_gameObjectManager.GetBattle().GetRandomInt(0, 360));
                m_gameObjectManager.AddGameObject(item11);
            }


            await Task.Delay(14000);
            CharacterData data1 = DataTables.Get(16).GetData<CharacterData>("BeeSniperSlowPot");
            LogicCharacterServer dummy = new LogicCharacterServer(this, 16, data1.GetInstanceId());
            dummy.SetPosition(3000, 5000, 0);
            dummy.SetIndex(10 * 16);
            m_gameObjectManager.AddGameObject(dummy);

        }

        public void RemoveSpectator(long id)
        {
            if (m_spectators.ContainsKey(id))
            {
                m_spectators.Remove(id);

            }
        }

        public bool IsInPlayArea(int x, int y)
        {
            return m_playArea.IsInside(x, y);
        }

        public int GetTeamPlayersCount(int teamIndex)
        {
            int result = 0;
            foreach (BattlePlayer player in GetPlayers())
            {
                if (player.TeamIndex == teamIndex) result++;
            }
            return result;
        }

        /*public bool IsAFK(int afkTicks)
        {
            int result = 0;
            foreach (BattlePlayer player in GetPlayers())
            {
                if (player.TeamIndex == teamIndex) result++;
            }
            return result;
        }
        */

        public bool IsPlayerAfk(int afkTicks, LogicCharacterServer logicCharacterServer)
        {
            var v1 = 15;


            //if (!logicCharacterServer.GetCharacterData().IsHero()) return false;
            return afkTicks > (v1 + 1) * GetTick() && !logicCharacterServer.GetPlayer()!.IsBot();
        }

        public void AddClientInput(ClientInput input, long sessionId)
        {
            if (!m_playersBySessionId.ContainsKey(sessionId)) return;

            input.OwnerSessionId = sessionId;
            m_inputQueue.Enqueue(input);
        }

        public void HandleSpectatorInput(ClientInput input, long sessionId)
        {
            if (input == null) return;

            if (!m_spectators.ContainsKey(sessionId)) return;
            m_spectators[sessionId].HandledInputs = input.Index;
        }




        private void HandleClientInput(ClientInput input)
        {
            if (input == null) return;

            BattlePlayer player = GetPlayerBySessionId(input.OwnerSessionId);


            if (player == null) return;
            if (player.LastHandledInput >= input.Index) return;


            player.LastHandledInput = input.Index;
            switch (input.Type)
            { // ПОСХАЛКО
                case 0:
                    {

                        LogicCharacterServer character = (LogicCharacterServer)m_gameObjectManager.GetGameObjectByID(player.OwnObjectId);

                        if (character == null) return;

                        LogicSkillServer skill = character.GetWeaponSkill();
                        if (skill == null) return;
                        character.ResetAFKTicks();
                        character.UltiDisabled();

                        bool indirection = false;
                        if (skill.SkillData.Projectile != null)
                        {
                            ProjectileData projectileData = DataTables.Get(DataType.Projectile).GetData<ProjectileData>(skill.SkillData.Projectile);
                            indirection = projectileData.Indirect;
                        }

                        if (!input.AutoAttack && !indirection)
                        {
                            character.ActivateSkill(false, input.X, input.Y);
                        }
                        else
                        {
                            character.ActivateSkill(false, input.X - character.GetX(), input.Y - character.GetY());
                        }
                        character.ResetAFKTicks();

                        break;
                    }
                case 1:
                    {



                        LogicCharacterServer character = (LogicCharacterServer)m_gameObjectManager.GetGameObjectByID(player.OwnObjectId);
                        if (character == null) return;
                        character.ResetAFKTicks();
                        LogicSkillServer skill = character.GetUltimateSkill();
                        if (skill == null) return;

                        if (!player.HasUlti()) return;
                        player.UseUlti();

                        character.UltiEnabled();

                        bool indirection = false;
                        if (skill.SkillData.Projectile != null)
                        {
                            ProjectileData projectileData = DataTables.Get(DataType.Projectile).GetData<ProjectileData>(skill.SkillData.Projectile);
                            indirection = projectileData.Indirect;
                        }

                        if (!input.AutoAttack && !indirection)
                        {
                            character.ActivateSkill(true, input.X, input.Y);
                        }
                        else
                        {
                            character.ActivateSkill(true, input.X - character.GetX(), input.Y - character.GetY());
                        }
                        character.ResetAFKTicks();

                        break;
                    }
                case 2:
                    {
                        LogicCharacterServer logicCharacterServer = (LogicCharacterServer)m_gameObjectManager.GetGameObjectByID(player.OwnObjectId);
                        if (logicCharacterServer == null) return;

                        var v12 = logicCharacterServer.GetX();
                        var v13 = logicCharacterServer.GetY();
                        var v7 = input.X;
                        var v90 = input.Y;
                        var v14 = v90;
                        var v15 = GamePlayUtil.GetDistanceSquaredBetween(v12, v13, v7, v90);

                        if (v15 > 810001)
                        {
                            var v16 = LogicMath.Sqrt(v15);
                            var v17 = logicCharacterServer.GetX();
                            var v18 = 900 * (v7 - logicCharacterServer.GetX()) / v16;
                            var v19 = logicCharacterServer.GetY();

                            v7 = v18 + v17;
                            v14 = 900 * (v90 - logicCharacterServer.GetY()) / v16 + v19;
                        }

                        logicCharacterServer.MoveTo(input.X, input.Y);
                        logicCharacterServer.ResetAFKTicks();
                        break;
                    }


                case 5:
                    {
                        LogicCharacterServer character = (LogicCharacterServer)m_gameObjectManager.GetGameObjectByID(player.OwnObjectId);
                        if (character == null) return;
                        character?.UltiEnabled();
                        character.ResetAFKTicks();
                        break;
                    }
                case 6:
                    {
                        LogicCharacterServer character = (LogicCharacterServer)m_gameObjectManager.GetGameObjectByID(player.OwnObjectId);
                        if (character == null) return;
                        character?.UltiDisabled();
                        character.ResetAFKTicks();
                        break;
                    }
                case 8:
                    {
                        LogicCharacterServer character = (LogicCharacterServer)m_gameObjectManager.GetGameObjectByID(player.OwnObjectId);
                        if (character == null) return;
                        character.LogicAccessory?.TriggerAccessory();
                        character.ResetAFKTicks();
                        break;
                    }
                default:
                    Debugger.Warning("Input is unhandled: " + input.Type);
                    break;
            }
        }

        public bool IsTileOnPoisonArea(int xTile, int yTile)
        {
            if (m_gameModeVariation != 6) return false;

            int tick = GetTicksGone();

            if (tick > 500)
            {
                int poisons = 0;
                poisons += (tick - 500) / 100;

                if (xTile <= poisons || xTile >= 59 - poisons || yTile <= poisons || yTile >= 59 - poisons)
                {
                    return true;
                }
            }
            return false;
        }

        private void HandleIncomingInputMessages()
        {
            while (m_inputQueue.Count > 0)
            {
                this.HandleClientInput(m_inputQueue.Dequeue());
            }
        }

        public void ExecuteOneTick()
        {
            try
            {

                this.HandleIncomingInputMessages();
                foreach (BattlePlayer player in GetPlayers())
                {
                    player.KillList.Clear();
                }

                if (this.CalculateIsGameOver())
                {

                    foreach (BattlePlayer player in m_players)
                    {
                        bool isVictory = m_winnerTeam == player.TeamIndex;
                        if (isVictory) // Draw
                        {
                            m_gameObjectManager.SetBattleEnd(1);
                        }

                    }
                    Task.Delay(5000);
                    m_updateTimer.Dispose();
                    GameOver();
                    IsGameOver = true;
                    return;
                }

                this.m_gameObjectManager.PreTick();
                this.Tick();
                this.m_time.IncreaseTick();
                this.SendVisionUpdateToPlayers();
            }
            catch (Exception e)
            {
                EndBattleError(43);
                Console.WriteLine("Battle stopped with exception! Message: " + e.Message + " Trace: " + e.StackTrace);
                m_updateTimer.Dispose();
                IsGameOver = true;

                SendBattleEndToPlayers();
            }
        }

        private void TickSpawnEventStuffDelayed()
        {
            if (m_gameModeVariation == 0)
            {
                if (GetTicksGone() % 100 == 0)
                {
                    int instanceId = DataTables.Get(18).GetInstanceId("Point");
                    LogicItemServer gem = new LogicItemServer(18, instanceId);
                    gem.SetPosition(2950, 4950, 0);
                    gem.SetAngle(GetRandomInt(0, 360));
                    m_gameObjectManager.AddGameObject(gem);
                }
            }
        }

        public void GameOver()
        {
            SendBattleEndToPlayers();
            //m_updateTimer.Dispose();
        }

        public void SendBattleEndToPlayers()
        {
            Random rand = new Random();

            foreach (BattlePlayer player in m_players)
            {
                if (player.SessionId < 0) continue;
                if (!player.IsAlive) continue;
                if (player.BattleRoyaleRank == -1) player.BattleRoyaleRank = 1;
                if (player.Avatar == null) continue;
                int rank = player.BattleRoyaleRank;
                player.Avatar.BattleId = -1;

                bool isWin = false;

                bool isVictory = m_winnerTeam == player.TeamIndex;

                BattleEndMessage message = new BattleEndMessage();
                Hero hero = player.Avatar.GetHero(player.CharacterId);

                if (!player.IsBot())
                {
                    HomeMode homeMode = LogicServerListener.Instance.GetHomeMode(player.AccountId);
                    if (homeMode != null)
                    {
                        if (homeMode.Home.Quests != null)
                        {
                            message.ProgressiveQuests = homeMode.Home.Quests.UpdateQuestsProgress(m_gameModeVariation, player.CharacterId, player.Kills, player.Damage, player.Heals, homeMode.Home);
                        }
                    }
                }

                if (m_gameModeVariation != 6)
                {
                    message.GameMode = 1;
                    message.IsPvP = true;
                    message.Players = m_players;
                    message.OwnPlayer = player;

                    if (m_winnerTeam == -1) // Draw
                    {
                        //GameObjectManager.StartEndTypeCountdown(1);
                        message.Result = 2;
                    }

                    if (isVictory)
                    {
                        message.Result = 0;
                    }
                    else if (m_winnerTeam != -1)
                    {
                        message.Result = 1;
                        //GameObjectManager.StartEndTypeCountdown(1);
                    }

                    if (isVictory)
                    {
                        //GameObjectManager.StartEndTypeCountdown(0);
                        isWin = true;
                    }



                    if (message.OwnPlayer.Trophies >= 0 && message.OwnPlayer.Trophies <= 49)
                    {
                        if (message.Result == 0)
                        {
                            message.TrophiesReward = 8000;
                            message.TokensReward = 20;
                            message.ExpReward = 8;


                            player.Home.Exp += 8;
                            player.Avatar.TrioWins += 1;
                        }
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 0;
                            message.TokensReward = 10;
                            message.ExpReward = 4;


                            player.Home.Exp += 4;
                            player.Avatar.TrioWins += 0;
                        }
                    }

                    if (message.OwnPlayer.Trophies >= 50 && message.OwnPlayer.Trophies <= 99)
                    {
                        if (message.Result == 0)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 20;
                            message.ExpReward = 8;


                            player.Home.Exp += 8;
                            player.Avatar.TrioWins += 1;
                        }
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = -1;
                            message.TokensReward = 10;
                            message.ExpReward = 4;


                            player.Home.Exp += 4;
                            player.Avatar.TrioWins += 0;
                        }
                    }

                    if (message.OwnPlayer.Trophies >= 100 && message.OwnPlayer.Trophies <= 199)
                    {
                        if (message.Result == 0)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 20;
                            message.ExpReward = 8;


                            player.Home.Exp += 8;
                            player.Avatar.TrioWins += 1;
                        }
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = -2;
                            message.TokensReward = 10;
                            message.ExpReward = 4;


                            player.Home.Exp += 4;
                            player.Avatar.TrioWins += 0;
                        }
                    }

                    if (message.OwnPlayer.Trophies >= 200 && message.OwnPlayer.Trophies <= 299)
                    {
                        if (message.Result == 0)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 20;
                            message.ExpReward = 8;


                            player.Home.Exp += 8;
                            player.Avatar.TrioWins += 1;
                        }
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = -3;
                            message.TokensReward = 10;
                            message.ExpReward = 4;


                            player.Home.Exp += 4;
                            player.Avatar.TrioWins += 0;
                        }
                    }

                    if (message.OwnPlayer.Trophies >= 300 && message.OwnPlayer.Trophies <= 399)
                    {
                        if (message.Result == 0)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 20;
                            message.ExpReward = 8;


                            player.Home.Exp += 8;
                            player.Avatar.TrioWins += 1;
                        }
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = -4;
                            message.TokensReward = 10;
                            message.ExpReward = 4;


                            player.Home.Exp += 4;
                            player.Avatar.TrioWins += 0;
                        }
                    }

                    if (message.OwnPlayer.Trophies >= 400 && message.OwnPlayer.Trophies <= 499)
                    {
                        if (message.Result == 0)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 20;
                            message.ExpReward = 8;


                            player.Home.Exp += 8;
                            player.Avatar.TrioWins += 1;
                        }
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = -5;
                            message.TokensReward = 10;
                            message.ExpReward = 4;


                            player.Home.Exp += 4;
                            player.Avatar.TrioWins += 0;
                        }
                    }

                    if (message.OwnPlayer.Trophies >= 500 && message.OwnPlayer.Trophies <= 599)
                    {
                        if (message.Result == 0)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 20;
                            message.ExpReward = 8;


                            player.Home.Exp += 8;
                            player.Avatar.TrioWins += 1;
                        }
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = -6;
                            message.TokensReward = 10;
                            message.ExpReward = 4;


                            player.Home.Exp += 4;
                            player.Avatar.TrioWins += 0;
                        }
                    }

                    if (message.OwnPlayer.Trophies >= 600 && message.OwnPlayer.Trophies <= 699)
                    {
                        if (message.Result == 0)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 20;
                            message.ExpReward = 8;


                            player.Home.Exp += 8;
                            player.Avatar.TrioWins += 1;
                        }
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = -7;
                            message.TokensReward = 10;
                            message.ExpReward = 4;


                            player.Home.Exp += 4;
                            player.Avatar.TrioWins += 0;
                        }
                    }

                    if (message.OwnPlayer.Trophies >= 700 && message.OwnPlayer.Trophies <= 799)
                    {
                        if (message.Result == 0)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 20;
                            message.ExpReward = 8;


                            player.Home.Exp += 8;
                            player.Avatar.TrioWins += 1;
                        }
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = -8;
                            message.TokensReward = 10;
                            message.ExpReward = 4;


                            player.Home.Exp += 4;
                            player.Avatar.TrioWins += 0;
                        }
                    }

                    if (message.OwnPlayer.Trophies >= 800 && message.OwnPlayer.Trophies <= 899)
                    {
                        if (message.Result == 0)
                        {
                            message.TrophiesReward = 7;
                            message.TokensReward = 20;
                            message.ExpReward = 8;


                            player.Home.Exp += 8;
                            player.Avatar.TrioWins += 1;
                        }
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = -9;
                            message.TokensReward = 10;
                            message.ExpReward = 4;


                            player.Home.Exp += 4;
                            player.Avatar.TrioWins += 0;
                        }
                    }

                    if (message.OwnPlayer.Trophies >= 900 && message.OwnPlayer.Trophies <= 999)
                    {
                        if (message.Result == 0)
                        {
                            message.TrophiesReward = 6;
                            message.TokensReward = 20;
                            message.ExpReward = 8;


                            player.Home.Exp += 8;
                            player.Avatar.TrioWins += 1;
                        }
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = -10;
                            message.TokensReward = 10;
                            message.ExpReward = 4;


                            player.Home.Exp += 4;
                            player.Avatar.TrioWins += 0;
                        }
                    }

                    if (message.OwnPlayer.Trophies >= 1000 && message.OwnPlayer.Trophies <= 1099)
                    {
                        if (message.Result == 0)
                        {
                            message.TrophiesReward = 5;
                            message.TokensReward = 20;
                            message.ExpReward = 8;


                            player.Home.Exp += 8;
                            player.Avatar.TrioWins += 1;
                        }
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = -11;
                            message.TokensReward = 10;
                            message.ExpReward = 4;


                            player.Home.Exp += 4;
                            player.Avatar.TrioWins += 0;
                        }
                    }

                    if (message.OwnPlayer.Trophies >= 1100 && message.OwnPlayer.Trophies <= 1199)
                    {
                        if (message.Result == 0)
                        {
                            message.TrophiesReward = 4;
                            message.TokensReward = 20;
                            message.ExpReward = 8;


                            player.Home.Exp += 8;
                            player.Avatar.TrioWins += 1;
                        }
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = -12;
                            message.TokensReward = 10;
                            message.ExpReward = 4;


                            player.Home.Exp += 4;
                            player.Avatar.TrioWins += 0;
                        }
                    }

                    if (message.OwnPlayer.Trophies >= 1200)
                    {
                        if (message.Result == 0)
                        {
                            message.TrophiesReward = 3;
                            message.TokensReward = 20;
                            message.ExpReward = 8;


                            player.Home.Exp += 8;
                            player.Avatar.TrioWins += 1;
                        }
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = -12;
                            message.TokensReward = 10;
                            message.ExpReward = 4;


                            player.Home.Exp += 4;
                            player.Avatar.TrioWins += 0;
                        }
                    }

                    player.Avatar.AddTokens(message.TokensReward);
                    hero.AddTrophies(message.TrophiesReward);
                }
                else
                {
                    message.IsPvP = true;
                    message.GameMode = 2;
                    message.Result = player.BattleRoyaleRank;
                    message.Players = new List<BattlePlayer>();
                    message.Players.Add(player);
                    message.OwnPlayer = player;
                    int tokensReward = 40 / rank;
                    message.TokensReward = tokensReward;

                    if (rank == 1 || rank == 2 || rank == 3 || rank == 4)
                    {
                        isWin = true;
                    }

                    if (message.OwnPlayer.Trophies >= 0 && message.OwnPlayer.Trophies <= 49)
                    {
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 1000;
                            message.TokensReward = 35;
                            player.Home.Exp += 15;
                            player.Avatar.SoloWins += 1;
                        }
                        if (message.Result == 2)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 28;
                            player.Home.Exp += 12;
                        }
                        if (message.Result == 3)
                        {
                            message.TrophiesReward = 7;
                            message.TokensReward = 22;
                            player.Home.Exp += 9;
                        }
                        if (message.Result == 4)
                        {
                            message.TrophiesReward = 6;
                            message.TokensReward = 16;
                            player.Home.Exp += 6;
                        }
                        if (message.Result == 5)
                        {
                            message.TrophiesReward = 4;
                            message.TokensReward = 12;
                            player.Home.Exp += 5;
                        }
                        if (message.Result == 6)
                        {
                            message.TrophiesReward = 2;
                            message.TokensReward = 8;
                            player.Home.Exp += 4;
                        }
                        if (message.Result == 7)
                        {
                            message.TrophiesReward = 2;
                            message.TokensReward = 6;
                            player.Home.Exp += 3;
                        }
                        if (message.Result == 8)
                        {
                            message.TrophiesReward = 1;
                            message.TokensReward = 4;
                            player.Home.Exp += 2;
                        }
                        if (message.Result == 9)
                        {
                            message.TrophiesReward = 0;
                            message.TokensReward = 2;
                            player.Home.Exp += 1;
                        }
                        if (message.Result == 10)
                        {
                            message.TrophiesReward = 0;
                            message.TokensReward = 0;
                            player.Home.Exp += 0;
                        }
                    }
                    if (message.OwnPlayer.Trophies >= 50 && message.OwnPlayer.Trophies <= 99)
                    {
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 10;
                            message.TokensReward = 35;
                            player.Home.Exp += 15;
                            player.Avatar.SoloWins += 1;
                        }
                        if (message.Result == 2)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 28;
                            player.Home.Exp += 12;
                        }
                        if (message.Result == 3)
                        {
                            message.TrophiesReward = 7;
                            message.TokensReward = 22;
                            player.Home.Exp += 9;
                        }
                        if (message.Result == 4)
                        {
                            message.TrophiesReward = 6;
                            message.TokensReward = 16;
                            player.Home.Exp += 6;
                        }
                        if (message.Result == 5)
                        {
                            message.TrophiesReward = 3;
                            message.TokensReward = 12;
                            player.Home.Exp += 5;
                        }
                        if (message.Result == 6)
                        {
                            message.TrophiesReward = 2;
                            message.TokensReward = 8;
                            player.Home.Exp += 4;
                        }
                        if (message.Result == 7)
                        {
                            message.TrophiesReward = 2;
                            message.TokensReward = 6;
                            player.Home.Exp += 3;
                        }
                        if (message.Result == 8)
                        {
                            message.TrophiesReward = 0;
                            message.TokensReward = 4;
                            player.Home.Exp += 2;
                        }
                        if (message.Result == 9)
                        {
                            message.TrophiesReward = -1;
                            message.TokensReward = 2;
                            player.Home.Exp += 1;
                        }
                        if (message.Result == 10)
                        {
                            message.TrophiesReward = -2;
                            message.TokensReward = 0;
                            player.Home.Exp += 0;
                        }
                    }
                    if (message.OwnPlayer.Trophies >= 100 && message.OwnPlayer.Trophies <= 199)
                    {
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 10;
                            message.TokensReward = 35;
                            player.Home.Exp += 15;
                            player.Avatar.SoloWins += 1;
                        }
                        if (message.Result == 2)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 28;
                            player.Home.Exp += 12;
                        }
                        if (message.Result == 3)
                        {
                            message.TrophiesReward = 7;
                            message.TokensReward = 22;
                            player.Home.Exp += 9;
                        }
                        if (message.Result == 4)
                        {
                            message.TrophiesReward = 6;
                            message.TokensReward = 16;
                            player.Home.Exp += 6;
                        }
                        if (message.Result == 5)
                        {
                            message.TrophiesReward = 3;
                            message.TokensReward = 12;
                            player.Home.Exp += 5;
                        }
                        if (message.Result == 6)
                        {
                            message.TrophiesReward = 1;
                            message.TokensReward = 8;
                            player.Home.Exp += 4;
                        }
                        if (message.Result == 7)
                        {
                            message.TrophiesReward = 0;
                            message.TokensReward = 6;
                            player.Home.Exp += 3;
                        }
                        if (message.Result == 8)
                        {
                            message.TrophiesReward = -1;
                            message.TokensReward = 4;
                            player.Home.Exp += 2;
                        }
                        if (message.Result == 9)
                        {
                            message.TrophiesReward = -2;
                            message.TokensReward = 2;
                            player.Home.Exp += 1;
                        }
                        if (message.Result == 10)
                        {
                            message.TrophiesReward = -2;
                            message.TokensReward = 0;
                            player.Home.Exp += 0;
                        }
                    }
                    if (message.OwnPlayer.Trophies >= 200 && message.OwnPlayer.Trophies <= 299)
                    {
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 10;
                            message.TokensReward = 35;
                            player.Home.Exp += 15;
                            player.Avatar.SoloWins += 1;
                        }
                        if (message.Result == 2)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 28;
                            player.Home.Exp += 12;
                        }
                        if (message.Result == 3)
                        {
                            message.TrophiesReward = 6;
                            message.TokensReward = 22;
                            player.Home.Exp += 9;
                        }
                        if (message.Result == 4)
                        {
                            message.TrophiesReward = 5;
                            message.TokensReward = 16;
                            player.Home.Exp += 6;
                        }
                        if (message.Result == 5)
                        {
                            message.TrophiesReward = 3;
                            message.TokensReward = 12;
                            player.Home.Exp += 5;
                        }
                        if (message.Result == 6)
                        {
                            message.TrophiesReward = 1;
                            message.TokensReward = 8;
                            player.Home.Exp += 4;
                        }
                        if (message.Result == 7)
                        {
                            message.TrophiesReward = 0;
                            message.TokensReward = 6;
                            player.Home.Exp += 3;
                        }
                        if (message.Result == 8)
                        {
                            message.TrophiesReward = -2;
                            message.TokensReward = 4;
                            player.Home.Exp += 2;
                        }
                        if (message.Result == 9)
                        {
                            message.TrophiesReward = -3;
                            message.TokensReward = 2;
                            player.Home.Exp += 1;
                        }
                        if (message.Result == 10)
                        {
                            message.TrophiesReward = -3;
                            message.TokensReward = 0;
                            player.Home.Exp += 0;
                        }
                    }
                    if (message.OwnPlayer.Trophies >= 300 && message.OwnPlayer.Trophies <= 399)
                    {
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 10;
                            message.TokensReward = 35;
                            player.Home.Exp += 15;
                            player.Avatar.SoloWins += 1;
                        }
                        if (message.Result == 2)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 28;
                            player.Home.Exp += 12;
                        }
                        if (message.Result == 3)
                        {
                            message.TrophiesReward = 6;
                            message.TokensReward = 22;
                            player.Home.Exp += 9;
                        }
                        if (message.Result == 4)
                        {
                            message.TrophiesReward = 5;
                            message.TokensReward = 16;
                            player.Home.Exp += 6;
                        }
                        if (message.Result == 5)
                        {
                            message.TrophiesReward = 2;
                            message.TokensReward = 12;
                            player.Home.Exp += 5;
                        }
                        if (message.Result == 6)
                        {
                            message.TrophiesReward = 0;
                            message.TokensReward = 8;
                            player.Home.Exp += 4;
                        }
                        if (message.Result == 7)
                        {
                            message.TrophiesReward = 0;
                            message.TokensReward = 6;
                            player.Home.Exp += 3;
                        }
                        if (message.Result == 8)
                        {
                            message.TrophiesReward = -3;
                            message.TokensReward = 4;
                            player.Home.Exp += 2;
                        }
                        if (message.Result == 9)
                        {
                            message.TrophiesReward = -4;
                            message.TokensReward = 2;
                            player.Home.Exp += 1;
                        }
                        if (message.Result == 10)
                        {
                            message.TrophiesReward = -4;
                            message.TokensReward = 0;
                            player.Home.Exp += 0;
                        }
                    }
                    if (message.OwnPlayer.Trophies >= 400 && message.OwnPlayer.Trophies <= 499)
                    {
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 10;
                            message.TokensReward = 35;
                            player.Home.Exp += 15;
                            player.Avatar.SoloWins += 1;
                        }
                        if (message.Result == 2)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 28;
                            player.Home.Exp += 12;
                        }
                        if (message.Result == 3)
                        {
                            message.TrophiesReward = 6;
                            message.TokensReward = 22;
                            player.Home.Exp += 9;
                        }
                        if (message.Result == 4)
                        {
                            message.TrophiesReward = 5;
                            message.TokensReward = 16;
                            player.Home.Exp += 6;
                        }
                        if (message.Result == 5)
                        {
                            message.TrophiesReward = 2;
                            message.TokensReward = 12;
                            player.Home.Exp += 5;
                        }
                        if (message.Result == 6)
                        {
                            message.TrophiesReward = -1;
                            message.TokensReward = 8;
                            player.Home.Exp += 4;
                        }
                        if (message.Result == 7)
                        {
                            message.TrophiesReward = -2;
                            message.TokensReward = 6;
                            player.Home.Exp += 3;
                        }
                        if (message.Result == 8)
                        {
                            message.TrophiesReward = -3;
                            message.TokensReward = 4;
                            player.Home.Exp += 2;
                        }
                        if (message.Result == 9)
                        {
                            message.TrophiesReward = -5;
                            message.TokensReward = 2;
                            player.Home.Exp += 1;
                        }
                        if (message.Result == 10)
                        {
                            message.TrophiesReward = -5;
                            message.TokensReward = 0;
                            player.Home.Exp += 0;
                        }
                    }
                    if (message.OwnPlayer.Trophies >= 500 && message.OwnPlayer.Trophies <= 599)
                    {
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 10;
                            message.TokensReward = 35;
                            player.Home.Exp += 15;
                            player.Avatar.SoloWins += 1;
                        }
                        if (message.Result == 2)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 28;
                            player.Home.Exp += 12;
                        }
                        if (message.Result == 3)
                        {
                            message.TrophiesReward = 6;
                            message.TokensReward = 22;
                            player.Home.Exp += 9;
                        }
                        if (message.Result == 4)
                        {
                            message.TrophiesReward = 4;
                            message.TokensReward = 16;
                            player.Home.Exp += 6;
                        }
                        if (message.Result == 5)
                        {
                            message.TrophiesReward = 2;
                            message.TokensReward = 12;
                            player.Home.Exp += 5;
                        }
                        if (message.Result == 6)
                        {
                            message.TrophiesReward = -1;
                            message.TokensReward = 8;
                            player.Home.Exp += 4;
                        }
                        if (message.Result == 7)
                        {
                            message.TrophiesReward = -2;
                            message.TokensReward = 6;
                            player.Home.Exp += 3;
                        }
                        if (message.Result == 8)
                        {
                            message.TrophiesReward = -5;
                            message.TokensReward = 4;
                            player.Home.Exp += 2;
                        }
                        if (message.Result == 9)
                        {
                            message.TrophiesReward = -6;
                            message.TokensReward = 2;
                            player.Home.Exp += 1;
                        }
                        if (message.Result == 10)
                        {
                            message.TrophiesReward = -6;
                            message.TokensReward = 0;
                            player.Home.Exp += 0;
                        }
                    }
                    if (message.OwnPlayer.Trophies >= 600 && message.OwnPlayer.Trophies <= 699)
                    {
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 10;
                            message.TokensReward = 35;
                            player.Home.Exp += 15;
                            player.Avatar.SoloWins += 1;
                        }
                        if (message.Result == 2)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 28;
                            player.Home.Exp += 12;
                        }
                        if (message.Result == 3)
                        {
                            message.TrophiesReward = 6;
                            message.TokensReward = 22;
                            player.Home.Exp += 9;
                        }
                        if (message.Result == 4)
                        {
                            message.TrophiesReward = 4;
                            message.TokensReward = 16;
                            player.Home.Exp += 6;
                        }
                        if (message.Result == 5)
                        {
                            message.TrophiesReward = 1;
                            message.TokensReward = 12;
                            player.Home.Exp += 5;
                        }
                        if (message.Result == 6)
                        {
                            message.TrophiesReward = -2;
                            message.TokensReward = 8;
                            player.Home.Exp += 4;
                        }
                        if (message.Result == 7)
                        {
                            message.TrophiesReward = -2;
                            message.TokensReward = 6;
                            player.Home.Exp += 3;
                        }
                        if (message.Result == 8)
                        {
                            message.TrophiesReward = -5;
                            message.TokensReward = 4;
                            player.Home.Exp += 2;
                        }
                        if (message.Result == 9)
                        {
                            message.TrophiesReward = -7;
                            message.TokensReward = 2;
                            player.Home.Exp += 1;
                        }
                        if (message.Result == 10)
                        {
                            message.TrophiesReward = -8;
                            message.TokensReward = 0;
                            player.Home.Exp += 0;
                        }
                    }
                    if (message.OwnPlayer.Trophies >= 700 && message.OwnPlayer.Trophies <= 799)
                    {
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 10;
                            message.TokensReward = 35;
                            player.Home.Exp += 15;
                            player.Avatar.SoloWins += 1;
                        }
                        if (message.Result == 2)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 28;
                            player.Home.Exp += 12;
                        }
                        if (message.Result == 3)
                        {
                            message.TrophiesReward = 6;
                            message.TokensReward = 22;
                            player.Home.Exp += 9;
                        }
                        if (message.Result == 4)
                        {
                            message.TrophiesReward = 4;
                            message.TokensReward = 16;
                            player.Home.Exp += 6;
                        }
                        if (message.Result == 5)
                        {
                            message.TrophiesReward = 1;
                            message.TokensReward = 12;
                            player.Home.Exp += 5;
                        }
                        if (message.Result == 6)
                        {
                            message.TrophiesReward = -3;
                            message.TokensReward = 8;
                            player.Home.Exp += 4;
                        }
                        if (message.Result == 7)
                        {
                            message.TrophiesReward = -4;
                            message.TokensReward = 6;
                            player.Home.Exp += 3;
                        }
                        if (message.Result == 8)
                        {
                            message.TrophiesReward = -5;
                            message.TokensReward = 4;
                            player.Home.Exp += 2;
                        }
                        if (message.Result == 9)
                        {
                            message.TrophiesReward = -8;
                            message.TokensReward = 2;
                            player.Home.Exp += 1;
                        }
                        if (message.Result == 10)
                        {
                            message.TrophiesReward = -9;
                            message.TokensReward = 0;
                            player.Home.Exp += 0;
                        }
                    }
                    if (message.OwnPlayer.Trophies >= 800 && message.OwnPlayer.Trophies <= 899)
                    {
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 9;
                            message.TokensReward = 35;
                            player.Home.Exp += 15;
                            player.Avatar.SoloWins += 1;
                        }
                        if (message.Result == 2)
                        {
                            message.TrophiesReward = 7;
                            message.TokensReward = 28;
                            player.Home.Exp += 12;
                        }
                        if (message.Result == 3)
                        {
                            message.TrophiesReward = 5;
                            message.TokensReward = 22;
                            player.Home.Exp += 9;
                        }
                        if (message.Result == 4)
                        {
                            message.TrophiesReward = 2;
                            message.TokensReward = 16;
                            player.Home.Exp += 6;
                        }
                        if (message.Result == 5)
                        {
                            message.TrophiesReward = 0;
                            message.TokensReward = 12;
                            player.Home.Exp += 5;
                        }
                        if (message.Result == 6)
                        {
                            message.TrophiesReward = -3;
                            message.TokensReward = 8;
                            player.Home.Exp += 4;
                        }
                        if (message.Result == 7)
                        {
                            message.TrophiesReward = -4;
                            message.TokensReward = 6;
                            player.Home.Exp += 3;
                        }
                        if (message.Result == 8)
                        {
                            message.TrophiesReward = -7;
                            message.TokensReward = 4;
                            player.Home.Exp += 2;
                        }
                        if (message.Result == 9)
                        {
                            message.TrophiesReward = -9;
                            message.TokensReward = 2;
                            player.Home.Exp += 1;
                        }
                        if (message.Result == 10)
                        {
                            message.TrophiesReward = -10;
                            message.TokensReward = 0;
                            player.Home.Exp += 0;
                        }
                    }
                    if (message.OwnPlayer.Trophies >= 900 && message.OwnPlayer.Trophies <= 999)
                    {
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 35;
                            player.Home.Exp += 15;
                            player.Avatar.SoloWins += 1;
                        }
                        if (message.Result == 2)
                        {
                            message.TrophiesReward = 6;
                            message.TokensReward = 28;
                            player.Home.Exp += 12;
                        }
                        if (message.Result == 3)
                        {
                            message.TrophiesReward = 4;
                            message.TokensReward = 22;
                            player.Home.Exp += 9;
                        }
                        if (message.Result == 4)
                        {
                            message.TrophiesReward = 1;
                            message.TokensReward = 16;
                            player.Home.Exp += 6;
                        }
                        if (message.Result == 5)
                        {
                            message.TrophiesReward = -1;
                            message.TokensReward = 12;
                            player.Home.Exp += 5;
                        }
                        if (message.Result == 6)
                        {
                            message.TrophiesReward = -3;
                            message.TokensReward = 8;
                            player.Home.Exp += 4;
                        }
                        if (message.Result == 7)
                        {
                            message.TrophiesReward = -6;
                            message.TokensReward = 6;
                            player.Home.Exp += 3;
                        }
                        if (message.Result == 8)
                        {
                            message.TrophiesReward = -8;
                            message.TokensReward = 4;
                            player.Home.Exp += 2;
                        }
                        if (message.Result == 9)
                        {
                            message.TrophiesReward = -11;
                            message.TokensReward = 2;
                            player.Home.Exp += 1;
                        }
                        if (message.Result == 10)
                        {
                            message.TrophiesReward = -12;
                            message.TokensReward = 0;
                            player.Home.Exp += 0;
                        }
                    }
                    if (message.OwnPlayer.Trophies >= 1000 && message.OwnPlayer.Trophies <= 1099)
                    {
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 6;
                            message.TokensReward = 35;
                            player.Home.Exp += 15;
                            player.Avatar.SoloWins += 1;
                        }
                        if (message.Result == 2)
                        {
                            message.TrophiesReward = 5;
                            message.TokensReward = 28;
                            player.Home.Exp += 12;
                        }
                        if (message.Result == 3)
                        {
                            message.TrophiesReward = 3;
                            message.TokensReward = 22;
                            player.Home.Exp += 9;
                        }
                        if (message.Result == 4)
                        {
                            message.TrophiesReward = 1;
                            message.TokensReward = 16;
                            player.Home.Exp += 6;
                        }
                        if (message.Result == 5)
                        {
                            message.TrophiesReward = -2;
                            message.TokensReward = 12;
                            player.Home.Exp += 5;
                        }
                        if (message.Result == 6)
                        {
                            message.TrophiesReward = -5;
                            message.TokensReward = 8;
                            player.Home.Exp += 4;
                        }
                        if (message.Result == 7)
                        {
                            message.TrophiesReward = -6;
                            message.TokensReward = 6;
                            player.Home.Exp += 3;
                        }
                        if (message.Result == 8)
                        {
                            message.TrophiesReward = -9;
                            message.TokensReward = 4;
                            player.Home.Exp += 2;
                        }
                        if (message.Result == 9)
                        {
                            message.TrophiesReward = -11;
                            message.TokensReward = 2;
                            player.Home.Exp += 1;
                        }
                        if (message.Result == 10)
                        {
                            message.TrophiesReward = -12;
                            message.TokensReward = 0;
                            player.Home.Exp += 0;
                        }
                    }
                    if (message.OwnPlayer.Trophies >= 1100 && message.OwnPlayer.Trophies <= 1199)
                    {
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 5;
                            message.TokensReward = 35;
                            player.Home.Exp += 15;
                            player.Avatar.SoloWins += 1;
                        }
                        if (message.Result == 2)
                        {
                            message.TrophiesReward = 4;
                            message.TokensReward = 28;
                            player.Home.Exp += 12;
                        }
                        if (message.Result == 3)
                        {
                            message.TrophiesReward = 1;
                            message.TokensReward = 22;
                            player.Home.Exp += 9;
                        }
                        if (message.Result == 4)
                        {
                            message.TrophiesReward = 0;
                            message.TokensReward = 16;
                            player.Home.Exp += 6;
                        }
                        if (message.Result == 5)
                        {
                            message.TrophiesReward = -2;
                            message.TokensReward = 12;
                            player.Home.Exp += 5;
                        }
                        if (message.Result == 6)
                        {
                            message.TrophiesReward = -6;
                            message.TokensReward = 8;
                            player.Home.Exp += 4;
                        }
                        if (message.Result == 7)
                        {
                            message.TrophiesReward = -7;
                            message.TokensReward = 6;
                            player.Home.Exp += 3;
                        }
                        if (message.Result == 8)
                        {
                            message.TrophiesReward = -10;
                            message.TokensReward = 4;
                            player.Home.Exp += 2;
                        }
                        if (message.Result == 9)
                        {
                            message.TrophiesReward = -12;
                            message.TokensReward = 2;
                            player.Home.Exp += 1;
                        }
                        if (message.Result == 10)
                        {
                            message.TrophiesReward = -13;
                            message.TokensReward = 0;
                            player.Home.Exp += 0;
                        }
                    }
                    if (message.OwnPlayer.Trophies >= 1200)
                    {
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 5;
                            message.TokensReward = 35;
                            player.Home.Exp += 15;
                            player.Avatar.SoloWins += 1;
                        }
                        if (message.Result == 2)
                        {
                            message.TrophiesReward = 3;
                            message.TokensReward = 28;
                            player.Home.Exp += 12;
                        }
                        if (message.Result == 3)
                        {
                            message.TrophiesReward = 0;
                            message.TokensReward = 22;
                            player.Home.Exp += 9;
                        }
                        if (message.Result == 4)
                        {
                            message.TrophiesReward = -1;
                            message.TokensReward = 16;
                            player.Home.Exp += 6;
                        }
                        if (message.Result == 5)
                        {
                            message.TrophiesReward = -2;
                            message.TokensReward = 12;
                            player.Home.Exp += 5;
                        }
                        if (message.Result == 6)
                        {
                            message.TrophiesReward = -6;
                            message.TokensReward = 8;
                            player.Home.Exp += 4;
                        }
                        if (message.Result == 7)
                        {
                            message.TrophiesReward = -8;
                            message.TokensReward = 6;
                            player.Home.Exp += 3;
                        }
                        if (message.Result == 8)
                        {
                            message.TrophiesReward = -11;
                            message.TokensReward = 4;
                            player.Home.Exp += 2;
                        }
                        if (message.Result == 9)
                        {
                            message.TrophiesReward = -12;
                            message.TokensReward = 2;
                            player.Home.Exp += 1;
                        }
                        if (message.Result == 10)
                        {
                            message.TrophiesReward = -13;
                            message.TokensReward = 0;
                            player.Home.Exp += 0;
                        }
                    }
                    if (isBugStop == 999) { message.TrophiesReward = 0; }

                    player.Avatar.AddTokens(message.TokensReward);
                    hero.AddTrophies(message.TrophiesReward);
                }
                try
                {
                    HomeMode homeMode = LogicServerListener.Instance.GetHomeMode(player.AccountId);
                    //message.ProgressiveQuests = homeMode.Home.Quests.UpdateQuestsProgress(m_gameModeVariation, player.CharacterId, player.Kills, player.Damage, player.Heals, homeMode.Home);
                }
                catch
                {
                    Console.WriteLine("Yeah");
                }

                if (player.Avatar == null) continue;
                if (player.GameListener == null) continue;
                player.GameListener.SendTCPMessage(message);
            }
        }

        public int GetTeamScore(int team)
        {
            int score = 0;
            foreach (BattlePlayer player in m_players)
            {
                if (player.TeamIndex == team) score += player.GetScore();
            }
            return score;
        }



        private bool CalculateIsGameOver()
        {
            if (m_gameModeVariation == 3)
            {
                if (GetTicksGone() >= 20 * 120 + 120)
                {
                    if (GetTeamScore(0) > GetTeamScore(1))
                    {
                        m_winnerTeam = 0;
                    }
                    else if (GetTeamScore(0) < GetTeamScore(1))
                    {
                        m_winnerTeam = 1;
                    }
                    else
                    {
                        m_winnerTeam = -1;
                    }
                    return true;
                }
            }
            else if (m_gameModeVariation == 0)
            {
                if (GetTeamScore(0) > GetTeamScore(1) && GetTeamScore(0) >= 10)
                {
                    if (m_gemGrabCountdown == 0)
                    {
                        m_gemGrabCountdown = GetTicksGone() + 20 * 17;
                    }
                    else if (GetTicksGone() > m_gemGrabCountdown)
                    {
                        m_winnerTeam = 0;
                        return true;
                    }
                }
                else if (GetTeamScore(0) < GetTeamScore(1) && GetTeamScore(1) >= 10)
                {
                    if (m_gemGrabCountdown == 0)
                    {
                        m_gemGrabCountdown = GetTicksGone() + 20 * 17;
                    }
                    else if (GetTicksGone() > m_gemGrabCountdown)
                    {
                        m_winnerTeam = 1;
                        return true;
                    }
                }
                else
                {
                    m_gemGrabCountdown = 0;
                }
            }
            else if (m_gameModeVariation == 6)
            {
                if (m_playersAlive <= 1)
                {
                    return true;
                }
            }

            return false;
        }


        private void Tick()
        {
            TickSpawnEventStuffDelayed();
            m_gameObjectManager.Tick();
            TickSpawnHeroes();
        }

        private void SendVisionUpdateToPlayers()
        {
            Parallel.ForEach(m_players, player =>
            {
                try
                {
                    if (player.GameListener != null)
                    {
                        BitStream visionBitStream = new BitStream(64);
                        m_gameObjectManager.Encode(visionBitStream, m_tileMap, player.OwnObjectId, player.PlayerIndex, player.TeamIndex);

                        VisionUpdateMessage visionUpdate = new VisionUpdateMessage();
                        visionUpdate.Tick = GetTicksGone();
                        visionUpdate.HandledInputs = player.LastHandledInput;
                        visionUpdate.Viewers = m_spectators.Count;
                        visionUpdate.VisionBitStream = visionBitStream;

                        player.GameListener.SendMessage(visionUpdate);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending vision update to player {player.PlayerIndex}: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    // Consider additional actions for handling the error:
                    // - Log to a file
                    // - Notify other parts of the game
                    // - Retry sending the update (if appropriate)
                    foreach (BattlePlayer player1 in this.m_players)
                    {
                        var message = new ServerErrorMessage(43);
                        player1.GameListener.SendTCPMessage(message);
                    }
                }
            });

            BitStream spectateStream = new BitStream(64);
            m_gameObjectManager.Encode(spectateStream, m_tileMap, 0, -1, -1);

            Task.Run(() =>
            {
                foreach (LogicGameListener gameListener in m_spectators.Values.ToArray())
                {
                    try
                    {
                        VisionUpdateMessage visionUpdate = new VisionUpdateMessage();
                        visionUpdate.Tick = GetTicksGone();
                        visionUpdate.HandledInputs = gameListener.HandledInputs;
                        visionUpdate.Viewers = m_spectators.Count;
                        visionUpdate.VisionBitStream = spectateStream;

                        gameListener.SendMessage(visionUpdate);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending spectate vision update: {ex.Message}");
                        Console.WriteLine($"Stack trace: {ex.StackTrace}");
                        foreach (BattlePlayer player in this.m_players)
                        {
                            var message = new ServerErrorMessage(43);
                            player.GameListener.SendTCPMessage(message);
                        }
                    }
                }
            });
        }

        public BattlePlayer[] GetPlayers()
        {
            return m_players.ToArray();
        }

        public int GetRandomInt(int min, int max)
        {
            return m_random.Rand(max - min) + min;
        }

        public int GetRandomInt(int max)
        {
            return m_random.Rand(max);
        }

        public int GetTicksGone()
        {
            return m_time.GetTick();
        }

        public int GetTick()
        {
            int ticksGone = m_time.GetTick();

            return ticksGone < 20 ? 20 : ticksGone;
        }


        public int GetGameModeVariation()
        {
            return m_gameModeVariation;
        }

        public int GetPlayersCountWithGameModeVariation()
        {
            return m_playersCountWithGameModeVariation;
        }

        public int GetRandomSeed()
        {
            return m_randomSeed;
        }

        public LocationData Location
        {
            get
            {
                return DataTables.Get(DataType.Location).GetDataByGlobalId<LocationData>(m_locationId);
            }
        }

        public void EndBattleError(int ErrorID)
        {
            try
            {

                foreach (BattlePlayer player in GetPlayers())
                {
                    if (player != null) continue;
                    var message = new ServerErrorMessage(ErrorID);
                    player.GameListener.SendTCPMessage(message);
                }
                m_updateTimer.Dispose();
                GameOver();
                IsGameOver = true;
            }
            catch (Exception)
            {
                // ignore
            }
        }

        public long Id { get; set; } // set long player data
    }
}