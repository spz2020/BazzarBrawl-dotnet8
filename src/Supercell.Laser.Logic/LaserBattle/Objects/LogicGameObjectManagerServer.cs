namespace Supercell.Laser.Logic.Battle.Objects
{
    using Supercell.Laser.Logic.Battle.Level;
    using Supercell.Laser.Logic.Battle.Structures;
    using Supercell.Laser.Logic.Data.Helper;
    using Supercell.Laser.Logic.Helper;
    using Supercell.Laser.Logic.Util;
    using Supercell.Laser.Titan.DataStream;

    public class LogicGameObjectManagerServer
    {
        public static int EMOTE_COOLDOWN_TICKS = 200;
        private Queue<LogicGameObjectServer> AddObjects;
        private Queue<LogicGameObjectServer> RemoveObjects;

        public int BallX = 3150;
        public int BallY = 4950;

        public int BallHP = 1;

        public int DestinationX;
        public int DestinationY;

        public int BallFlyTicks = 88888;

        public int BallAngle;
        public bool BallFlying => BallFlyTicks < 17;

        private LogicBattleModeServer Battle;
        private List<LogicGameObjectServer> GameObjects;

        private int ObjectCounter;
        private int EndType;

        public LogicGameObjectManagerServer(LogicBattleModeServer battle)
        {
            Battle = battle;
            EndType = -1;
            GameObjects = new List<LogicGameObjectServer>();

            AddObjects = new Queue<LogicGameObjectServer>();
            RemoveObjects = new Queue<LogicGameObjectServer>();
        }

        public LogicGameObjectServer[] GetGameObjects()
        {
            return GameObjects.ToArray();
        }

        public LogicBattleModeServer GetBattle()
        {
            return Battle;
        }

        public void PreTick()
        {
            foreach (LogicGameObjectServer gameObject in GameObjects)
            {
                if (gameObject.ShouldDestruct())
                {
                    gameObject.OnDestruct();
                    RemoveGameObject(gameObject);
                }
                else
                {
                    gameObject.PreTick();
                }

            }

            while (AddObjects.Count > 0)
            {
                GameObjects.Add(AddObjects.Dequeue());
            }

            while (RemoveObjects.Count > 0)
            {
                GameObjects.Remove(RemoveObjects.Dequeue());
            }
        }

        public void Tick()
        {
            foreach (LogicGameObjectServer gameObject in GameObjects)
            {
                gameObject.Tick();
            }
        }

        public void AddGameObject(LogicGameObjectServer gameObject)
        {
            gameObject.AttachGameObjectManager(this, GlobalId.CreateGlobalId(gameObject.GetObjectType(), ObjectCounter++));
            AddObjects.Enqueue(gameObject);
        }

        public void RemoveGameObject(LogicGameObjectServer gameObject)
        {
            RemoveObjects.Enqueue(gameObject);
        }

        public LogicGameObjectServer GetGameObjectByID(int globalId)
        {
            return GameObjects.Find(obj => obj.GetGlobalID() == globalId);
        }

        public List<LogicGameObjectServer> GetVisibleGameObjects(int teamIndex)
        {
            List<LogicGameObjectServer> objects = new List<LogicGameObjectServer>();

            foreach (LogicGameObjectServer obj in GameObjects)
            {
                if (obj.GetFadeCounter() > 0 || obj.GetIndex() / 16 == teamIndex)
                {
                    objects.Add(obj);
                }
            }

            return objects;
        }



        public void SetBattleEnd(int Type)
        {
            EndType = Type;
        }


        public void Encode(BitStream bitStream, TileMap tileMap, int ownObjectGlobalId, int playerIndex, int teamIndex)
        {
            BattlePlayer[] players = Battle.GetPlayers();
            List<LogicGameObjectServer> visibleGameObjects = GetVisibleGameObjects(teamIndex);


            int GameModeVariation = Battle.GetGameModeVariation();
            bitStream.WritePositiveInt(ownObjectGlobalId, 21);

            if (GameModeVariation == 0)
            {
                bitStream.WritePositiveVInt(Battle.GetGemGrabCountdown(), 4);
            }

            bitStream.WriteBoolean(teamIndex > 0 && GameModeUtil.HasTwoTeams(GameModeVariation));
            bitStream.WriteInt(EndType, 4); // -1 battle 0 - win 1 - defeat (ANIM)

            bitStream.WriteBoolean(true);
            bitStream.WriteBoolean(true);
            bitStream.WriteBoolean(true);
            bitStream.WriteBoolean(false);

            if (tileMap.Width < 22)
            {
                bitStream.WritePositiveInt(0, 5); // 0xa820a8
                bitStream.WritePositiveInt(0, 6); // 0xa820b4
                bitStream.WritePositiveInt(tileMap.Width - 1, 5); // 0xa820c0
            }
            else
            {
                bitStream.WritePositiveInt(0, 6); // 0xa820a8
                bitStream.WritePositiveInt(0, 6); // 0xa820b4
                bitStream.WritePositiveInt(tileMap.Width - 1, 6); // 0xa820c0
            }
            bitStream.WritePositiveInt(tileMap.Height - 1, 6); // 0xa820d0

            for (int i = 0; i < tileMap.Width; i++)
            {
                for (int j = 0; j < tileMap.Height; j++)
                {
                    var tile = tileMap.GetTile(i, j, true);
                    //if (GetBattle().GetTicksGone() > 100) tile.Destruct();
                    if (tile.Data.RespawnSeconds > 0 || tile.Data.IsDestructible)
                    {
                        bitStream.WriteBoolean(tile.IsDestructed());
                    }
                }
            }


            bitStream.WritePositiveInt(1, 1);


            for (int i = 0; i < players.Length; i++)
            {
                bitStream.WritePositiveInt(0, 1);
                bitStream.WriteBoolean(players[i].HasUlti());
                if (GameModeVariation == 6)
                {
                    bitStream.WritePositiveInt(0, 4);
                }
                LogicCharacterServer logicCharacterServer = (LogicCharacterServer)GetGameObjectByID(players[i].OwnObjectId);



                if (logicCharacterServer != null) logicCharacterServer.LogicAccessory?.Encode(bitStream, i == playerIndex);

                if (i == playerIndex)
                {
                    bitStream.WritePositiveInt(players[i].GetUltiCharge(), 12);
                    bitStream.WritePositiveInt(0, 1);
                    bitStream.WritePositiveInt(0, 1);
                }
            }

            bitStream.WritePositiveInt(1, 1);

            switch (GameModeVariation)
            {
                case 6:
                    bitStream.WritePositiveInt(Battle.GetPlayersAliveCountForBattleRoyale(), 4);
                    break;
                case 16:

                    break;
                case 9:
                    bitStream.WritePositiveIntMax7(0); // duo shd 
                    break;
                case 19:
                    bitStream.WritePositiveIntMax127(0);
                    bitStream.WritePositiveIntMax127(0);
                    break;
                case 18:
                    bitStream.WritePositiveIntMax127(0);
                    break;
                case 14:
                    bitStream.WritePositiveIntMax127(0);
                    bitStream.WritePositiveIntMax16383(0);
                    break;
                case 13:
                    bitStream.WritePositiveIntMax131071(0);
                    break;
                case 10:
                    bitStream.WritePositiveIntMax127(0);
                    break;
                case 7:
                    bitStream.WritePositiveIntMax127(0);
                    break;
            }


            for (int i = 0; i < players.Length; i++)
            {
                if (GameModeVariation == 8)
                {
                    bitStream.WriteBoolean(true);
                    bitStream.WritePositiveVIntMax255(100);
                }
                else if (GameModeVariation != 6 && GameModeVariation != 15 && GameModeVariation != 17)
                {
                    bitStream.WriteBoolean(true);
                    bitStream.WritePositiveVIntMax255(players[i].GetScore());
                }
                else if (GameModeVariation == 15)
                {
                    bitStream.WritePositiveIntMax134217727(1);
                }
                else if (GameModeVariation == 17)
                {
                    bitStream.WritePositiveIntMax8191(98); // zone 0 index
                    bitStream.WritePositiveIntMax8191(99); // zone 1 index
                }

                else
                {
                    bitStream.WriteBoolean(false);
                }
                if (bitStream.WriteBoolean(players[i].KillList.Count > 0))
                {
                    bitStream.WritePositiveIntMax15(players[i].KillList.Count);
                    for (int j = 0; j < players[i].KillList.Count; j++)
                    {
                        bitStream.WritePositiveIntMax15(players[i].KillList[j].PlayerIndex);
                        bitStream.WriteIntMax7(players[i].KillList[j].BountyStarsEarned);
                    }
                }
            }

            /*

            */



            bitStream.WritePositiveInt(visibleGameObjects.Count, 8);

            foreach (LogicGameObjectServer gameObject in visibleGameObjects)
            {
                ByteStreamHelper.WriteDataReference(bitStream, gameObject.GetDataId());
            }

            foreach (LogicGameObjectServer gameObject in visibleGameObjects)
            {
                bitStream.WritePositiveInt(GlobalId.GetInstanceId(gameObject.GetGlobalID()), 14); // 0x2381b4
            }

            foreach (LogicGameObjectServer gameObject in visibleGameObjects)
            {
                gameObject.Encode(bitStream, gameObject.GetGlobalID() == ownObjectGlobalId, teamIndex);
            }

            bitStream.WritePositiveInt(0, 8);
        }
    }
}
