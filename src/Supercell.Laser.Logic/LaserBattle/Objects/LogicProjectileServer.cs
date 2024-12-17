namespace Supercell.Laser.Logic.Battle.Objects
{
    using Supercell.Laser.Logic.Battle.Level;
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Titan.DataStream;
    using Supercell.Laser.Titan.Math;

    public class LogicProjectileServer : LogicGameObjectServer
    {
        public ProjectileData ProjectileData => DataTables.Get(DataType.Projectile).GetDataByGlobalId<ProjectileData>(DataId);
        private CharacterData characterData => DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(DataId);
        private List<int> AlreadyDamagedObjectsGlobalIds;

        private LogicGameObjectServer Source;
        private int Angle;
        private int Damage;
        private int CastingTime;

        private int TicksActive;
        private bool ShouldDestructImmediately;

        private bool IsUltiWeapon;

        private LogicVector2 TargetPosition;

        private int m_destroyedTicks;
        private int _blockShakeIndex;

        private CharacterData SummonedCharacter;

        public int MaxRange;
        private bool _destroyByBlock;

        public LogicProjectileServer(int classId, int instanceId) : base(classId, instanceId)
        {
            TicksActive = -1;
            FullTravelTicks = -1;
            Z = 500;

            TargetPosition = new LogicVector2();
            AlreadyDamagedObjectsGlobalIds = new List<int>();
        }

        private int m_totalDelta;

        public override void Tick()
        {
            if (IsDestroyed())
            {
                if (m_destroyedTicks < 1)
                {
                    TargetReached();
                }

                m_destroyedTicks++;
                return;
            }

            if (!ProjectileData.Indirect)
            {
                if (m_totalDelta > CastingTime * 180) return;
            }

            int deltaX = (int)(((float)LogicMath.Cos(Angle) / 20000) * ProjectileData.Speed);
            int deltaY = (int)(((float)LogicMath.Sin(Angle) / 20000) * ProjectileData.Speed);

            m_totalDelta += ProjectileData.Speed / 20;

            Position.X += deltaX;
            Position.Y += deltaY;

            TileMap tileMap = GameObjectManager.GetBattle().GetTileMap();

            Tile tile = tileMap.GetTile(TileMap.LogicToTile(Position.X), TileMap.LogicToTile(Position.Y), true);
            if (tile == null)
            {
                ShouldDestructImmediately = true;
                return;
            }

            if (!ProjectileData.Indirect)
            {
                if (!tile.IsDestructed() && tile.Data.BlocksProjectiles && !(tile.Data.IsDestructibleNormalWeapon || (tile.Data.IsDestructible && IsUltiWeapon)))
                {
                    ShouldDestructImmediately = true;
                    _destroyByBlock = true;
                    _blockShakeIndex = 0;
                }
                else if (tile.Data.IsDestructibleNormalWeapon)
                {
                    tile.Destruct();
                }
                else if (tile.Data.IsDestructible && IsUltiWeapon)
                {
                    tile.Destruct();
                }
            }

            if (Position.X <= 250 || Position.Y <= 250) ShouldDestructImmediately = true;

            if (!ProjectileData.Indirect)
            {
                HandleCollisions();
            }

            if (ProjectileData.Indirect)
            {
                if (FullTravelTicks < 0)
                {
                    int distance = Position.GetDistance(TargetPosition);
                    FullTravelTicks = distance / (ProjectileData.Speed / 20);
                    //Console.WriteLine(MaxRange);
                    FullTravelTicks = LogicMath.Min(FullTravelTicks, MaxRange);
                }

                if (TicksActive < (FullTravelTicks / 2))
                {
                    Z += (ProjectileData.Gravity / 20) * (FullTravelTicks - TicksActive);
                    // Console.WriteLine("DELTA X: " + (FullTravelTicks - TicksActive));
                }
                else
                {
                    int tmp = (FullTravelTicks) - TicksActive;
                    int deltaZ = (ProjectileData.Gravity / 20) * (TicksActive - tmp);
                    //Console.WriteLine("DELTA Z: " + deltaZ);
                    if (deltaZ > 0) Z -= deltaZ;
                }

                if (TicksActive >= FullTravelTicks) ShouldDestructImmediately = true;
            }

            if (!GameObjectManager.GetBattle().IsInPlayArea(Position.X, Position.Y))
            {
                ShouldDestructImmediately = true;
                SetForcedInvisible();

            }

            TicksActive++;
        }

        private int FullTravelTicks;

        public void SetTargetPosition(int x, int y)
        {
            TargetPosition.Set(x, y);
        }

        public void SetSummonedCharacter(CharacterData data)
        {
            SummonedCharacter = data;
        }

        private void HandleCollisions()
        {
            foreach (LogicGameObjectServer gameObject in GameObjectManager.GetGameObjects())
            {
                if (gameObject == null) continue;

                if (gameObject.GetObjectType() != 1) continue;
                if (!gameObject.IsAlive()) continue;
                if (AlreadyDamagedObjectsGlobalIds.Contains(gameObject.GetGlobalID()))
                {
                    LogicCharacterServer character1 = (LogicCharacterServer)gameObject;
                    character1.TriggerPushback(ProjectileData.PushbackStrength, Angle, ProjectileData.EarlyTicks, true);
                    continue;

                }


                int teamIndex = gameObject.GetIndex() / 16;
                if (teamIndex == this.GetIndex() / 16) continue;

                int radius1 = gameObject.GetRadius();
                int radius2 = this.GetRadius();
                LogicCharacterServer character2 = (LogicCharacterServer)gameObject;
                //CreateAreaEffect("");

                if (character2.GetZ() >= 10) return;

                if (Position.GetDistance(gameObject.GetPosition()) <= radius1 + radius2)
                {
                    // Collision!
                    if (!ProjectileData.PiercesCharacters) ShouldDestructImmediately = true;

                    AlreadyDamagedObjectsGlobalIds.Add(gameObject.GetGlobalID());


                    LogicCharacterServer character = (LogicCharacterServer)gameObject;
                    if (ProjectileData.Name != "RocketGirlProjectile") character.CauseDamage((LogicCharacterServer)Source, Damage);
                    if (ProjectileData.PoisonType != 0)
                    {
                        character.AddPoison((LogicCharacterServer)Source, ((int)(((float)ProjectileData.PoisonDamagePercent / 100) * (float)Damage)), 4, ProjectileData.PoisonType, false);
                    }

                    return;
                }
            }
        }

        private void TargetReached()
        {
            if (ProjectileData.SpawnAreaEffectObject != null)
            {
                CreateAreaEffect(ProjectileData.SpawnAreaEffectObject);
                if (ProjectileData.SpawnAreaEffectObject == "BlackHoleUltiSuck" && ShouldDestructImmediately == true) CreateAreaEffect("BlackHoleUltiExplosion");
            }
            if (ProjectileData.SpawnAreaEffectObject2 != null)
            {
                CreateAreaEffect(ProjectileData.SpawnAreaEffectObject2);
            }
            if (SummonedCharacter != null)
            {
                LogicCharacterServer character = new LogicCharacterServer(null, 16, SummonedCharacter.GetInstanceId());
                character.SetPosition(GetX(), GetY(), 0);
                character.SetIndex(Source.GetIndex());
                GameObjectManager.AddGameObject(character);

                switch (SummonedCharacter.Name)
                {
                    case "DamageBooster":
                        CreateAreaEffect("DamageBoost");
                        break;
                    case "HealingStation":
                        CreateAreaEffect("HealingStationHeal");
                        break;
                }
            }

        }

        public override void OnDestruct()
        {
            ;
        }

        public void CreateAreaEffect(string name)
        {
            AreaEffectData data = DataTables.Get(DataType.AreaEffect).GetData<AreaEffectData>(name);

            LogicAreaEffectServer effect = new LogicAreaEffectServer(null, 17, data.GetInstanceId());
            effect.SetPosition(GetX(), GetY(), 0);
            effect.SetIndex(GetIndex());
            effect.SetDamage(Damage);
            effect.SetSource((LogicCharacterServer)Source);

            GameObjectManager.AddGameObject(effect);
        }

        private bool IsDestroyed()
        {
            return ((m_totalDelta > CastingTime * 180 && !ProjectileData.Indirect) || ShouldDestructImmediately);
        }

        public override bool ShouldDestruct()
        {
            return ((m_totalDelta > CastingTime * 180 && !ProjectileData.Indirect) || ShouldDestructImmediately) && m_destroyedTicks > 2;
        }

        public override void Encode(BitStream bitStream, bool isOwnObject, int visionTeam)
        {
            base.Encode(bitStream, isOwnObject, visionTeam);

            int effect = 0;
            if (m_totalDelta > CastingTime * 180 && !ProjectileData.Indirect) effect = 1;
            if (ShouldDestructImmediately) effect = 3;
            if (_destroyByBlock) effect = 4;

            bitStream.WritePositiveIntMax7(effect); // next effect
            switch (_destroyByBlock)
            {
                case false:
                    bitStream.WriteBoolean(effect != 1 && effect != 0);
                    break;
                case true:
                    bitStream.WritePositiveIntMax1023(0);
                    bitStream.WriteBoolean(false);
                    break;
            }

            if (ProjectileData.TriggerWithDelayMs != 0 || ProjectileData.PreExplosionTimeMs != 0)
                bitStream.WritePositiveVIntMax65535(0);

            if (ProjectileData.PreExplosionTimeMs != 0)
            {
                bitStream.WritePositiveVIntMax65535(200);
                bitStream.WritePositiveVIntMax65535(2000);
            }

            bitStream.WritePositiveIntMax1023(0); // Total path

            if (ProjectileData.Rendering != "DoNotRotateClip")
                bitStream.WritePositiveIntMax511(Angle);

            bitStream.WriteBoolean(false);
        }

        public void ShootProjectile(int angle, LogicGameObjectServer owner, int damage, int castingTime, bool isUlti)
        {
            TicksActive = 0;
            Angle = angle;
            Damage = damage;
            CastingTime = castingTime;

            Source = owner;
            SetIndex(owner.GetIndex());

            Position.X = owner.GetX();
            Position.Y = owner.GetY();

            IsUltiWeapon = isUlti;
        }

        public override int GetObjectType()
        {
            return 2;
        }
    }
}
