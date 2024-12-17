namespace Supercell.Laser.Logic.Battle.Objects
{
    using Supercell.Laser.Logic.Battle.Level;
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Titan.DataStream;

    public class LogicAreaEffectServer : LogicGameObjectServer
    {
        private LogicCharacterServer m_source;

        private int m_ticksElapsed;
        private LogicBattleModeServer BattleMode;
        private int m_damage;
        private int moveto_tick;
        private List<LogicCharacterServer> m_alreadyDamagedList;
        private List<Tile> TileInRadius;

        public LogicAreaEffectServer(LogicBattleModeServer battle, int classId, int instanceId) : base(classId, instanceId)
        {
            BattleMode = battle;
            m_alreadyDamagedList = new List<LogicCharacterServer>();
            TileInRadius = new List<Tile>();
        }

        public AreaEffectData EffectData => DataTables.Get(DataType.AreaEffect).GetDataByGlobalId<AreaEffectData>(DataId);

        public override void Tick()
        {
            m_ticksElapsed++;
            moveto_tick++;

            if (EffectData.Type == "Damage")
            {
                if (m_damage == 0) m_damage = EffectData.Damage;

                LogicGameObjectServer[] objects = GameObjectManager.GetGameObjects();
                foreach (LogicGameObjectServer gameObject in objects)
                {
                    if (gameObject.GetObjectType() != 1) continue;

                    LogicCharacterServer character = (LogicCharacterServer)gameObject;
                    if (character == null) continue;

                    if (m_alreadyDamagedList.Contains(character)) continue;
                    if (character.GetIndex() / 16 == m_source.GetIndex() / 16) continue;
                    if (character.GetPosition().GetDistance(Position) > GetRadius()) continue;

                    character.CauseDamage(m_source, m_damage);
                    m_alreadyDamagedList.Add(character);
                }
            }
            else if (EffectData.Type == "BulletExplosion")
            {
                if (m_ticksElapsed == 1)
                {
                    ProjectileData projectileData = DataTables.Get(6).GetData<ProjectileData>(EffectData.BulletExplosionBullet);
                    int a = 0;
                    for (int i = 0; i < EffectData.CustomValue; i++)
                    {
                        LogicProjectileServer projectile = new LogicProjectileServer(6, projectileData.GetInstanceId());
                        projectile.ShootProjectile(a, m_source, 400, EffectData.BulletExplosionBulletDistance / 2, false);
                        projectile.SetPosition(GetX(), GetY(), 400);
                        a += 360 / EffectData.CustomValue;
                        GameObjectManager.AddGameObject(projectile);
                    }
                }
            }
            else if (EffectData.Type == "PushBack")
            {

            }
            else if (EffectData.Type == "DelayedDamage")
            {
                if (m_damage == 0) m_damage = EffectData.Damage;

                LogicGameObjectServer[] objects = GameObjectManager.GetGameObjects();
                foreach (LogicGameObjectServer gameObject in objects)
                {
                    if (gameObject.GetObjectType() != 1) continue;

                    LogicCharacterServer character = (LogicCharacterServer)gameObject;
                    if (character == null) continue;

                    if (m_alreadyDamagedList.Contains(character)) continue;
                    if (character.GetIndex() / 16 == m_source.GetIndex() / 16) continue;
                    if (character.GetPosition().GetDistance(Position) > GetRadius()) continue;

                    character.CauseDamage(m_source, m_damage);
                    m_alreadyDamagedList.Add(character);
                }
            }


            else if (EffectData.Type == "DamageBoost") // 8-bit ulti area effect
            {
                //if (m_ticksElapsed % 20 == 0) m_alreadyDamagedList.Clear();

                LogicGameObjectServer[] objects = GameObjectManager.GetGameObjects();
                foreach (LogicGameObjectServer gameObject in objects)
                {
                    if (gameObject.GetObjectType() != 1) continue;

                    LogicCharacterServer character = (LogicCharacterServer)gameObject;
                    if (character.GetIndex() != GetIndex()) return;

                    if (character == null) continue;
                    if (character.GetIndex() != GetIndex()) return;

                    if (m_alreadyDamagedList.Contains(character)) continue;
                    if (character.GetIndex() / 16 == m_source.GetIndex() / 16) continue;
                    if (character.GetPosition().GetDistance(Position) > GetRadius()) continue;

                    character.Buff(0);
                    //if (!m_alreadyDamagedList.Contains(character)) character.DeBuff();


                    m_alreadyDamagedList.Add(character);
                }
            }
            else if (EffectData.Type == "Slow") // bea accsessory
            {

            }
            else if (EffectData.Type == "HealRegen") // idk
            {

            }
            else if (EffectData.Type == "SmokeScreen") // sandy ulti
            {

            }

            else if (EffectData.Type == "ShieldBuff") // jacky ulti
            {

            }

            else if (EffectData.Type == "Effect")
            {
                if (m_ticksElapsed == 1)
                {
                    ProjectileData projectileData = DataTables.Get(6).GetData<ProjectileData>(EffectData.BulletExplosionBullet);
                    int a = 0;
                    for (int i = 0; i < EffectData.CustomValue; i++)
                    {
                        LogicProjectileServer projectile = new LogicProjectileServer(6, projectileData.GetInstanceId());
                        projectile.ShootProjectile(a, m_source, 400, EffectData.BulletExplosionBulletDistance / 2, false);
                        projectile.SetPosition(GetX(), GetY(), 400);
                        a += 360 / EffectData.CustomValue;
                        GameObjectManager.AddGameObject(projectile);
                    }
                }
            }
            else if (EffectData.Type == "ChargeSuper") // bo accsessory
            {

            }
            else if (EffectData.Type == "SpeedBuff") // max ulti
            {

            }
            else if (EffectData.Type == "Heal") // pam ulti
            {

            }
            else if (EffectData.Type == "Dot")
            {

                Console.WriteLine(m_ticksElapsed);
                if (m_ticksElapsed % 20 == 0) m_alreadyDamagedList.Clear();

                LogicGameObjectServer[] objects = GameObjectManager.GetGameObjects();
                foreach (LogicGameObjectServer gameObject in objects)
                {
                    if (gameObject.GetObjectType() != 1) continue;

                    LogicCharacterServer character = (LogicCharacterServer)gameObject;
                    if (character == null) continue;

                    if (m_alreadyDamagedList.Contains(character)) continue;
                    if (character.GetIndex() / 16 == m_source.GetIndex() / 16) continue;
                    if (character.GetPosition().GetDistance(Position) > GetRadius()) continue;

                    character.CauseDamage(m_source, m_damage);
                    switch ((EffectData.Name))
                    {
                        case "MummyUltiArea":
                            character.SetSlowDown(true, 30);

                            if (GameObjectManager.GetBattle().GetTicksGone() > 0)
                            {
                                this.SetPosition(m_source.GetX(), m_source.GetY(), 0);
                            }
                            if (m_ticksElapsed >= 80) character.ResetSlowDown();
                            break;
                        case "CactusUltiExplosion":
                            character.SetSlowDown(true, 30);
                            if (m_ticksElapsed >= 80) character.ResetSlowDown();

                            break;
                        case "WhirlwindTrail": // carl accsessory
                            break;
                        default:
                            break;
                    }
                    Console.WriteLine(m_ticksElapsed);
                    m_alreadyDamagedList.Add(character);
                }

            }
        }

        public override void Encode(BitStream bitStream, bool isOwnObject, int visionTeam)
        {
            base.Encode(bitStream, isOwnObject, visionTeam);

            bitStream.WritePositiveInt(GetFadeCounter(), 4);
            bitStream.WritePositiveIntMax127(0);
        }

        public void SetSource(LogicCharacterServer source)
        {
            m_source = source;
        }

        public void SetDamage(int damage)
        {
            m_damage = damage;
        }

        public override int GetRadius()
        {
            return EffectData.Radius;
        }


        public override bool ShouldDestruct()
        {
            return m_ticksElapsed >= EffectData.TimeMs / 50;
        }

        public override int GetObjectType()
        {
            return 3;
        }
    }
}
