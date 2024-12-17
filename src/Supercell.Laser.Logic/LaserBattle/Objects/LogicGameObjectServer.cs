namespace Supercell.Laser.Logic.Battle.Objects
{
    using Supercell.Laser.Logic.Battle.Structures;
    using Supercell.Laser.Logic.Data.Helper;
    using Supercell.Laser.Titan.DataStream;
    using Supercell.Laser.Titan.Math;

    public class LogicGameObjectServer
    {
        protected int DataId;
        protected LogicGameObjectManagerServer GameObjectManager;
        private readonly LogicBattleModeServer _logicBattleModeServer;

        private int Index;
        private int FadeCounter;
        private int ObjectGlobalId;

        protected LogicVector2 Position;
        protected int Z;

        public LogicGameObjectServer(int classId, int instanceId)
        {
            DataId = GlobalId.CreateGlobalId(classId, instanceId);

            Position = new LogicVector2();
            Z = 0;

            FadeCounter = 10;
        }

        public virtual void Tick()
        {
            ;
        }

        public virtual void Encode(BitStream bitStream, bool isOwnObject, int visionTeam)
        {
            var v1 = LogicMath.Clamp(Position.X, 0, 65535);
            var v2 = LogicMath.Clamp(Position.Y, 0, 65535);

            bitStream.WritePositiveVIntMax65535(v1);
            bitStream.WritePositiveVIntMax65535(v2);
            bitStream.WritePositiveVIntMax255(Index);
            bitStream.WritePositiveVIntMax65535(Z);
        }

        public virtual void PreTick()
        {
            ;
        }

        public void SetForcedVisible()
        {
            FadeCounter = 10;
        }

        public void SetForcedInvisible()
        {
            FadeCounter = 0;
        }

        public void IncrementFadeCounter()
        {
            if (FadeCounter < 10) FadeCounter++;
        }

        public void DecrementFadeCounter()
        {
            if (FadeCounter > 0) FadeCounter--;
        }

        public LogicBattleModeServer GetLogicBattleModeServer()
        {
            return _logicBattleModeServer;
        }

        public int GetFadeCounter()
        {
            return FadeCounter;
        }
        public void SetPosition(int x, int y, int z)
        {
            Position.Set(x, y);
            Z = z;
        }

        public void SetPosition(LogicVector2 logicVector2)
        {
            Position.Set(logicVector2.GetX(), logicVector2.GetY());
        }

        public LogicVector2 GetPosition()
        {
            return Position.Clone();
        }

        public BattlePlayer GetPlayer()
        {
            LogicBattleModeServer battle = GameObjectManager.GetBattle();
            return battle.GetPlayer(ObjectGlobalId);
        }

        public LogicBattleModeServer GetBattleMode()
        {
            LogicBattleModeServer battle = GameObjectManager.GetBattle();
            return battle;
        }

        public int GetGlobalID()
        {
            return ObjectGlobalId;
        }

        public int GetDataId()
        {
            return DataId;
        }

        public void AttachGameObjectManager(LogicGameObjectManagerServer gameObjectManager, int globalId)
        {
            GameObjectManager = gameObjectManager;
            ObjectGlobalId = globalId;
        }

        public virtual bool ShouldDestruct()
        {
            return false;
        }

        public virtual void OnDestruct()
        {
            ;
        }

        public int GetX()
        {
            return Position.GetX();
        }

        public int GetY()
        {
            return Position.GetY();
        }

        public int GetZ()
        {
            return Z;
        }

        public int GetTileX()
        {
            return Position.GetX() / 300;
        }

        public int GetTileY()
        {
            return Position.GetY() / 300;
        }

        public void SetIndex(int i)
        {
            Index = i;
        }

        public int GetIndex()
        {
            return Index;
        }

        public virtual bool IsAlive()
        {
            return true;
        }

        public virtual int GetRadius()
        {
            return 100;
        }

        public virtual int GetSize()
        {
            return 100;
        }

        public virtual int GetObjectType()
        {
            return -1;
        }
    }
}
