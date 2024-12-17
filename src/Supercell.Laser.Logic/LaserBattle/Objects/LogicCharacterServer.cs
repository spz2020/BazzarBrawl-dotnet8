namespace Supercell.Laser.Logic.Battle.Objects
{
    using Supercell.Laser.Logic.Battle.Component;
    using Supercell.Laser.Logic.Battle.Level;
    using Supercell.Laser.Logic.Battle.LogicPlayerModifications;
    using Supercell.Laser.Logic.Battle.Structures;
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Logic.Message.Account;
    using Supercell.Laser.Logic.UHelper;
    using Supercell.Laser.Logic.Util;
    using Supercell.Laser.Titan.DataStream;
    using Supercell.Laser.Titan.Debug;
    using Supercell.Laser.Titan.Math;

    public class LogicCharacterServer : LogicGameObjectServer
    {
        public SkillData SkillData => DataTables.Get(DataType.Skill).GetDataByGlobalId<SkillData>(DataId);
        public const int MAX_SKILL_HOLD_TICKS = 15;

        public const int INTRO_TICKS = 120;

        private LogicVector2 _requiredPosition = new();
        private LogicVector2 m_movementDestination;
        private LogicVector2 m_closestEnemyPosition;
        private LogicVector2 _nowPosition = new();
        private LogicVector2 _startPosition = new();

        private int _nowPushbackTicker;
        private int _receivedPushbackAngle;
        private int _receivedPushbackEarlyTicks;
        private float _receivedPushbackStrength;
        private bool _reversedPushback;



        private int m_hitpoints;
        private int m_maxHitpoints;



        private int m_state;
        public int m_angle;
        private int afkticks;

        private bool brawlbol = false;

        private int _paramCounter;

        private bool IsSlowed;


        private bool m_isMoving;
        private bool m_usingUltiCurrently;


        private int m_tickWhenHealthRegenBlocked;
        private int m_lastSelfHealTick;

        private bool m_holdingSkill;
        private int m_skillHoldTicksGone;

        private List<LogicSkillServer> m_skills;

        private int m_itemCount;

        private int m_heroLevel;

        private int _interruptSkillsTick;

        private bool m_isBot;
        private int m_ticksSinceBotEnemyCheck = 100;
        private int m_lastAIAttackTick;

        private LogicCharacterServer m_closestEnemy;


        private int m_activeChargeType;

        private bool m_isStunned;
        private int m_ticksGoneSinceStunned;

        private int m_damageMultiplier;
        private int m_lastTileDamageTick;

        private List<int> m_damageIndicator;
        private LogicImmuntyServer m_immunity;

        private int m_attackingTicks;
        public bool m_invis;
        public bool m_slowdown;
        public int m_slowpower;
        private int _stunnedTick;

        private LogicPoisonServer m_poison;
        private int m_teamIndex;
        public LogicAccessory? LogicAccessory;

        bool buffed;

        private LogicBattleModeServer PBattle;
        private FlyPractice _flyPractice;
        private int skinid;
        private int _poisonDamageCounter;


        public LogicCharacterServer(LogicBattleModeServer Battle, int classId, int instanceId) : base(classId, instanceId)
        {
            BattlePlayer player1 = new BattlePlayer();
            buffed = false;
            PBattle = Battle;

            m_damageIndicator = new List<int>();
            m_skills = new List<LogicSkillServer>();

            m_slowdown = false;
            m_slowpower = 0;


            _poisonDamageCounter = 0;

            m_maxHitpoints = CharacterData.Hitpoints;
            m_hitpoints = m_maxHitpoints;
            m_attackingTicks = 63;

            _interruptSkillsTick = 0;

            m_state = 4;
            _stunnedTick = -1;

            m_invis = false;

            if (WeaponSkillData != null)
                m_skills.Add(new LogicSkillServer(Battle, WeaponSkillData.GetGlobalId(), false));
            if (UltimateSkillData != null)
                m_skills.Add(new LogicSkillServer(Battle, UltimateSkillData.GetGlobalId(), true));

            m_activeChargeType = -1;

            afkticks = 0;

            _paramCounter = 0;


            LogicAccessory = new LogicAccessory(PBattle, this);

            if (this.GetIndex() == 0) m_angle = 90;
            else m_angle = 90 * 3;
        }


        public void AddPoison(LogicCharacterServer pSource, int pDamage, int pTickCount, int pType, bool pSlowDown)
        {
            if (m_poison != null)
            {
                m_poison.RefreshPoison(pSource, pDamage, pTickCount);

                return;
            }

            m_poison = new LogicPoisonServer(pSource, pSlowDown, pDamage, pTickCount, pType);
        }

        public void SetImmunity(int time, int percentage)
        {
            m_immunity = new LogicImmuntyServer(time, percentage);
        }

        public void SetBot(bool isbot)
        {
            m_isBot = isbot;
        }

        public override void PreTick()
        {
            m_damageIndicator.Clear();
        }

        public CharacterData CharacterData => DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(DataId);

        public SkillData WeaponSkillData => DataTables.Get(DataType.Skill).GetData<SkillData>(CharacterData.WeaponSkill);
        public SkillData UltimateSkillData => DataTables.Get(DataType.Skill).GetData<SkillData>(CharacterData.UltimateSkill);

        public void ApplyItem(LogicItemServer logicItem)
        {
            if (logicItem.ItemData.Name == "BattleRoyaleBuff")
            {
                if (GameObjectManager.GetBattle().GetGameModeVariation() == 6)
                {
                    m_itemCount++;
                }

                int delta = ((int)(((float)10 / 100) * (float)CharacterData.Hitpoints));
                m_maxHitpoints += delta;
                m_hitpoints = LogicMath.Min(m_hitpoints + delta, m_maxHitpoints);
                m_damageMultiplier++;
            }

            if (logicItem.ItemData.Name == "Money")
            {
                BattlePlayer player = GameObjectManager.GetBattle().GetPlayer(GetGlobalID());
                if (player != null)
                {
                    player.AddScore(1);
                }
            }

            if (logicItem.ItemData.Name == "Point" && GameObjectManager.GetBattle().GetGameModeVariation() == 0)
            {
                BattlePlayer player = GameObjectManager.GetBattle().GetPlayer(GetGlobalID());
                if (player != null)
                {
                    m_itemCount++;
                    player.AddScore(1);
                }
            }
        }

        public override void Tick()
        {
            if (this.m_isBot)
            { TickBot(); }
            else
            {
                HandleMoveAndAttack();
            }


            if (m_holdingSkill) m_skillHoldTicksGone++;

            foreach (LogicSkillServer skill in m_skills)
            {
                skill.Tick();
            }
            if (GameObjectManager.GetBattle().GetTicksGone() > LogicCharacterServer.INTRO_TICKS)
            {
                TickTimers();
            }
            TickTile();
            if (CharacterData.IsHero()) TickHeals();

            LogicAccessory?.UpdateAccessory();


            if (m_attackingTicks < 63) m_attackingTicks++;

            if (GameObjectManager.GetBattle().GetTicksGone() > INTRO_TICKS) TickAI();
        }


        private void TickTimers()
        {
            LogicBattleModeServer battle = GameObjectManager.GetBattle();

            afkticks++;

            if (m_immunity != null)
            {
                if (m_immunity.Tick(1))
                {
                    m_immunity.Destruct();
                    m_immunity = null;
                }
            }

            if (m_poison != null)
            {
                if (m_poison.Tick(this))
                {
                    m_poison.Destruct();
                    m_poison = null;
                }
            }



            if (afkticks > 31222200)// battle.IsPlayerAfk(afkticks, this))
            {
                foreach (BattlePlayer player in battle.m_players)
                {
                    var message = new ServerErrorMessage(43); // вообще надо disconnect но ок
                    player.GameListener.SendTCPMessage(message);
                    this.ResetAFKTicks();
                    Console.WriteLine("Отправили а хуй знает че отправили");
                }
            }





            if (_receivedPushbackStrength != 0 &&
    _receivedPushbackStrength / 20 > _nowPushbackTicker + _receivedPushbackEarlyTicks)
            {
                var deltaX = GetX() + (int)((float)LogicMath.Cos(_receivedPushbackAngle) /
                                            (battle.GetTick() * 1000)
                                            * (_receivedPushbackStrength * (CharacterData.Speed /
                                                                            battle.GetTick())));
                var deltaY = GetY() + (int)((float)LogicMath.Sin(_receivedPushbackAngle) /
                                            (battle.GetTick() * 1000)
                                            * (_receivedPushbackStrength * (CharacterData.Speed /
                                                                            battle.GetTick())));

                /*
                if (!battle.GetTileMap().LogicRect.IsInside(deltaX, deltaY))
                {
                    SetPosition(GetX(), GetY(), 0);

                    _reversedPushback = false;
                    _nowPushbackTicker = 0;
                    _receivedPushbackStrength = 0;
                    _receivedPushbackAngle = 0;
                    _receivedPushbackEarlyTicks = 0;
                    m_state = 4;

                    return;
                }
                */

                // тоже todo потому что щас лень

                m_angle = LogicMath.GetAngleBetween(m_angle, LogicMath.GetAngle(deltaX, deltaY));
                {
                    m_state = 3;
                }

                SetPosition(deltaX, deltaY, 200 + _nowPushbackTicker * 5);
                {
                    _nowPushbackTicker++;
                }

                m_angle = 360 - _receivedPushbackAngle;
            }

            if (!_reversedPushback && _receivedPushbackStrength != 0 &&
                _nowPushbackTicker + _receivedPushbackEarlyTicks >= _receivedPushbackStrength / 20)
            {
                _reversedPushback = true;
                _nowPushbackTicker = 0;
            }

            if (_reversedPushback) // and isInAirFromPushback
            {
                if (GetZ() > 20)
                {
                    SetPosition(GetX(), GetY(), -20 * 2 + 200 - _nowPushbackTicker * 20 - 2);
                    {
                        _nowPushbackTicker++;
                    }
                }
                else
                {
                    SetPosition(GetX(), GetY(), 0);
                    {
                        _reversedPushback = false;
                        _nowPushbackTicker = 0;
                        _receivedPushbackStrength = 0;
                        _receivedPushbackAngle = 0;
                        _receivedPushbackEarlyTicks = 0;

                        m_state = 4;
                    }
                }
            }
            if (m_attackingTicks < 63 - 1 + -5) m_attackingTicks += 5;


        }

        public void ResetAFKTicks()
        {
            afkticks = 0;
        }

        private void TickTile()
        {
            TileMap tileMap = GameObjectManager.GetBattle().GetTileMap();

            Tile tile = tileMap.GetTile(GetX(), GetY());
            if (tile.Data.HidesHero && !tile.IsDestructed())
            {
                DecrementFadeCounter();
            }
            else
            {
                IncrementFadeCounter();
            }

            int x = TileMap.LogicToTile(GetX());
            int y = TileMap.LogicToTile(GetY());
            if (GameObjectManager.GetBattle().GetTicksGone() - m_lastTileDamageTick > 20)
            {
                if (GameObjectManager.GetBattle().IsTileOnPoisonArea(x, y))
                {
                    _poisonDamageCounter++;
                    m_lastTileDamageTick = GameObjectManager.GetBattle().GetTicksGone();
                    CauseDamage(this, 1000 + 300 * _poisonDamageCounter);
                }
            }
            else
            {
                _poisonDamageCounter = 1;
            }


        }

        private void StopMovement()
        {
            this.m_isMoving = false;
        }

        private int m_meleeAttackEndTick = -1;
        private LogicCharacterServer m_meleeAttackTarget;
        private int m_meleeAttackDamage;

        private void StartMeleeAttack(LogicCharacterServer target, int ticks, int damage)
        {
            this.m_meleeAttackTarget = target;
            this.m_attackingTicks = 0;
            this.m_meleeAttackEndTick = GameObjectManager.GetBattle().GetTicksGone() + ticks;
            this.m_meleeAttackDamage = damage;
            this.m_state = 3;
        }


        private LogicCharacterServer ShamanPetTarget;


        private void TickAI()
        {
            if (m_isBot)
            {
                TickBot();
                return;
            }

            if (CharacterData.IsHero()) return;

            if (CharacterData.Name == "ShamanPet")
            {
                m_ticksSinceBotEnemyCheck++;

                if (m_ticksSinceBotEnemyCheck > 20)
                {
                    this.ShamanPetTarget = GetClosestEnemy();
                }

                if (this.ShamanPetTarget == null) return;

                if (this.ShamanPetTarget.GetPosition().GetDistance(this.Position) <= 300)
                {
                    this.StopMovement();
                    if (this.m_meleeAttackEndTick < this.GameObjectManager.GetBattle().GetTicksGone())
                    {
                        this.StartMeleeAttack(this.ShamanPetTarget, 10, GetAbsoluteDamage(CharacterData.AutoAttackDamage));
                    }
                }
                else
                {
                    this.MoveTo(this.ShamanPetTarget.GetX(), this.ShamanPetTarget.GetY());
                }
            }

            if (CharacterData.Name == "MechanicTurret")
            {
                m_ticksSinceBotEnemyCheck++;

                if (m_ticksSinceBotEnemyCheck > 20)
                {
                    ShamanPetTarget = GetClosestEnemy();
                }

                if (ShamanPetTarget == null) return;

                if (ShamanPetTarget.GetPosition().GetDistance(Position) <= 300)
                {
                    StopMovement();
                    if (m_meleeAttackEndTick < this.GameObjectManager.GetBattle().GetTicksGone())
                    {
                        StartMeleeAttack(ShamanPetTarget, 10, GetAbsoluteDamage(CharacterData.AutoAttackDamage));
                    }
                }
                else
                {
                    this.MoveTo(this.ShamanPetTarget.GetX(), this.ShamanPetTarget.GetY());
                }
            }

            if (CharacterData.AutoAttackProjectile != null && CharacterData.AutoAttackSpeedMs > 0 && CharacterData.AutoAttackDamage > 0)
            {
                if (GameObjectManager.GetBattle().GetTicksGone() - m_lastAIAttackTick < CharacterData.AutoAttackSpeedMs / 50) return;
                foreach (LogicGameObjectServer gameObject in GameObjectManager.GetGameObjects())
                {
                    if (gameObject.GetObjectType() != 1) continue;
                    if (gameObject.GetIndex() / 16 == GetIndex() / 16) continue;
                    if (Position.GetDistance(gameObject.GetPosition()) > 100 * CharacterData.AutoAttackRange) continue;

                    ProjectileData projectileData = DataTables.Get(DataType.Projectile).GetData<ProjectileData>(CharacterData.AutoAttackProjectile);
                    int angle = LogicMath.GetAngle(gameObject.GetX() - GetX(), gameObject.GetY() - GetY());
                    m_lastAIAttackTick = GameObjectManager.GetBattle().GetTicksGone();

                    m_state = 3;
                    m_attackingTicks = 0;
                    m_angle = angle;
                    break;
                }
            }
        }

        private void TickBot()
        {
            m_ticksSinceBotEnemyCheck++;

            if (m_ticksSinceBotEnemyCheck > 60 || m_closestEnemy == null)
            {
                m_ticksSinceBotEnemyCheck = 0;
                LogicCharacterServer closestEnemy = GetClosestEnemy();

                if (closestEnemy == null) return;

                m_closestEnemy = closestEnemy;
                m_closestEnemyPosition = closestEnemy.GetPosition();
            }

            if (m_closestEnemy == null) return;

            if (m_ticksSinceBotEnemyCheck % 40 == 0) MoveTo(m_closestEnemyPosition.X, m_closestEnemyPosition.Y);

            if (GameObjectManager.GetBattle().GetTicksGone() - m_lastAIAttackTick <= 20) return;
            LogicSkillServer weapon = GetWeaponSkill();
            LogicVector2 enemyPosition = m_closestEnemy.GetPosition();
            if (Position.GetDistance(enemyPosition) >= WeaponSkillData.CastingRange * 80) return;
            if (!weapon.HasEnoughCharge()) return;
            m_lastAIAttackTick = GameObjectManager.GetBattle().GetTicksGone();

            int deltaX = enemyPosition.X - Position.X;
            int deltaY = enemyPosition.Y - Position.Y;

            ActivateSkill(false, deltaX, deltaY);

        }

        public bool IsTargetVisibleToAttack(LogicCharacterServer checker)
        {
            return !checker.CharacterData.IsHero() || GetFadeCounter() > 0;

        }

        public LogicCharacterServer GetClosestEnemy()
        {
            LogicCharacterServer closestEnemy = null;
            int distance = 99999999;

            foreach (LogicGameObjectServer gameObject in GameObjectManager.GetGameObjects())
            {
                if (gameObject.GetObjectType() != 1) continue;

                LogicCharacterServer enemy = (LogicCharacterServer)gameObject;
                if (enemy == null) continue;
                if (enemy.GetIndex() / 16 == GetIndex() / 16) continue;
                if (!enemy.IsTargetVisibleToAttack(this)) continue;

                int distanceToEnemy = Position.GetDistance(enemy.GetPosition());
                if (distanceToEnemy < distance)
                {
                    closestEnemy = enemy;
                    distance = distanceToEnemy;
                }
            }

            return closestEnemy;
        }


        private void TickHeals()
        {
            if (m_hitpoints >= m_maxHitpoints) return;

            int ticksGone = GameObjectManager.GetBattle().GetTicksGone();
            if (ticksGone - m_tickWhenHealthRegenBlocked < 60) // 3 seconds
                return;
            if (ticksGone - m_lastSelfHealTick < 20) // 1 second
                return;

            m_lastSelfHealTick = ticksGone;

            int heal = 13 * m_maxHitpoints / 100;
            CauseDamage(this, -heal, false);

            BattlePlayer player = GameObjectManager.GetBattle().GetPlayerWithObject(GetGlobalID());
            if (player != null)
            {
                if (heal > 0)
                {
                    player.Healed(heal);
                }
            }
        }

        public void CauseDamage(LogicCharacterServer damageDealer, int damage, bool shouldShow = true)
        {
            try
            {
                if (m_hitpoints <= 0) return;
                if (GetZ() >= 10) return;

                if (m_immunity != null)
                {
                    int damageDiff = ((int)(((float)m_immunity.GetImmunityPercentage() / 100) * (float)damage));
                    damage -= damageDiff;
                }

                m_hitpoints -= damage;
                m_hitpoints = LogicMath.Max(m_hitpoints, 0);
                m_hitpoints = LogicMath.Min(m_hitpoints, m_maxHitpoints);


                if (damage > 0) BlockHealthRegen();

                LogicBattleModeServer battle = GameObjectManager.GetBattle();
                if (shouldShow) m_damageIndicator.Add(damage);

                if (CharacterData.IsHero())
                {
                    if (damageDealer != null && damageDealer != this)
                    {
                        BattlePlayer enemy = battle.GetPlayerWithObject(damageDealer.GetGlobalID());
                        if (enemy != null)
                        {
                            if (damage > 0)
                            {
                                enemy.AddUltiCharge(damageDealer.CharacterData.UltiChargeMul * (damage / 80));
                                enemy.DamageDealed(damage);
                            }
                        }
                    }

                    if (m_hitpoints <= 0)
                    {
                        BattlePlayer player = battle.GetPlayerWithObject(GetGlobalID());

                        if (player != null)
                        {
                            battle.PlayerDied(player);
                        }

                        if (damageDealer != null)
                        {
                            BattlePlayer enemy = battle.GetPlayerWithObject(damageDealer.GetGlobalID());
                            if (GameObjectManager.GetBattle().GetGameModeVariation() == 3)
                            {
                                if (enemy != null)
                                {
                                    damageDealer.AddItemsCollected(1);
                                    enemy.AddScore(m_itemCount + 1);
                                }
                            }

                            if (enemy != null)
                            {
                                int bountyStars = GameObjectManager.GetBattle().GetGameModeVariation() == 3 ? m_itemCount + 1 : 0;
                                enemy.KilledPlayer(GetIndex() % 16, bountyStars);
                            }
                        }

                        if (GameObjectManager.GetBattle().GetGameModeVariation() == 6)
                        {
                            ItemData data = DataTables.Get(18).GetData<ItemData>("BattleRoyaleBuff");
                            LogicItemServer item = new LogicItemServer(18, data.GetInstanceId());
                            item.SetPosition(GetX(), GetY(), 0);
                            item.SetAngle(GameObjectManager.GetBattle().GetRandomInt(0, 360));
                            GameObjectManager.AddGameObject(item);
                        }

                        if (GameObjectManager.GetBattle().GetGameModeVariation() == 0)
                        {
                            ItemData data = DataTables.Get(18).GetData<ItemData>("Point");
                            for (int i = 0; i < m_itemCount; i++)
                            {
                                LogicItemServer item = new LogicItemServer(18, data.GetInstanceId());
                                item.SetPosition(GetX(), 1000, 0);
                                item.SetAngle(GameObjectManager.GetBattle().GetRandomInt(0, 360));
                                GameObjectManager.AddGameObject(item);
                            }

                        }

                        if (GameObjectManager.GetBattle().GetGameModeVariation() == 1488) // футбольнiй мячiк
                        {
                            ItemData data = DataTables.Get(18).GetData<ItemData>("Point");
                            LogicItemServer item = new LogicItemServer(18, data.GetInstanceId());
                            item.SetPosition(GetX(), 1000, 0);
                            item.SetAngle(GameObjectManager.GetBattle().GetRandomInt(0, 360));
                            GameObjectManager.AddGameObject(item);

                        }

                    }
                }

                if (m_hitpoints <= 0)
                {
                    if (CharacterData.Name == "LootBox")
                    {
                        ItemData data = DataTables.Get(18).GetData<ItemData>("BattleRoyaleBuff");
                        LogicItemServer item = new LogicItemServer(18, data.GetInstanceId());
                        item.SetPosition(GetX() + 100, GetY() + 1000, 0);
                        item.SetAngle(GameObjectManager.GetBattle().GetRandomInt(0, 360));
                        GameObjectManager.AddGameObject(item);
                    }
                }
            }
            catch (Exception) { }
        }



        public void SetAllVisibleByGadget()
        {
            // to do
        }

        public void AddItemsCollected(int a)
        {
            m_itemCount += a;
            if (GameObjectManager.GetBattle().GetGameModeVariation() == 3)
            {
                m_itemCount = LogicMath.Min(6, m_itemCount);
            }
        }
        public int CalculateDamageBuffsAndNerfs()
        {
            return 0;
        }
        public void ResetItemsCollected()
        {
            m_itemCount = 0;
        }

        public void HoldSkillStarted()
        {
            if (!m_holdingSkill)
            {
                m_holdingSkill = true;
                m_skillHoldTicksGone = 0;
            }
        }

        public void SkillReleased()
        {
            m_holdingSkill = false;
        }

        public LogicSkillServer GetWeaponSkill()
        {
            return m_skills.Count > 0 ? m_skills[0] : null;
        }

        public LogicSkillServer GetUltimateSkill()
        {
            return m_skills.Count > 1 ? m_skills[1] : null;
        }

        public void InterruptAllSkills()
        {
            foreach (LogicSkillServer skill in m_skills)
            {
                skill.Interrupt();
            }
        }

        public void BlockHealthRegen()
        {
            m_tickWhenHealthRegenBlocked = GameObjectManager.GetBattle().GetTicksGone();
        }


        public void ActivateSkill(bool isUlti, int x, int y)
        {
            m_state = 3;

            LogicSkillServer skill = isUlti ? GetUltimateSkill() : GetWeaponSkill();
            if (skill == null) return;
            if (skill.IsActive) return;
            if (skill.SkillData.BehaviorType == "Charge1123") return;

            TileMap tileMap = GameObjectManager.GetBattle().GetTileMap();
            m_angle = LogicMath.GetAngle(x, y);
            skill.Activate(this, x, y, tileMap);
            m_attackingTicks = 0;
            m_state = 0;


            switch ((skill.SkillData.ChargeType))
            {
                case 1:
                    // bull ulti
                    MoveTo(x, y);
                    break;
                case 2:
                    // piper and crow ulti
                    break;
                case 3:
                    LogicVector2 Destination = new LogicVector2(GetX() + x, GetY() + y);
                    JumpChargeDestination = Destination;
                    int distance = Position.GetDistance(Destination);
                    ChargeTime = distance / 50;
                    break;
                case 4:
                    MoveTo(x, y);
                    // mortis main attack
                    break;
                case 5:
                    break;
                case 6:
                    // piper main attack
                    break;
                case 7:
                    // darryl ulti
                    break;
                default:
                    break;
            }

            if (!string.IsNullOrEmpty(skill.SkillData.AreaEffectObject))
            {
                AreaEffectData effectData = DataTables.Get(17).GetData<AreaEffectData>(skill.SkillData.AreaEffectObject);
                LogicAreaEffectServer effect = new LogicAreaEffectServer(PBattle, 17, effectData.GetInstanceId());
                effect.SetPosition(GetX(), GetY(), 0);
                effect.SetSource(this);
                effect.SetIndex(GetIndex());
                effect.SetDamage(skill.SkillData.Damage);
                GameObjectManager.AddGameObject(effect);
            }

            if (!string.IsNullOrEmpty(skill.SkillData.SpawnedItem))
            {

                ItemData data = DataTables.Get(18).GetData<ItemData>("BoxOfMines");
                LogicItemServer item = new LogicItemServer(18, data.GetInstanceId());
                item.SetPosition(GetX(), GetY(), 0);
                item.SetIndex(GetIndex());
                item.SetAngle(90);
                //GameObjectManager.AddGameObject(item);
            }

        }


        private LogicVector2 JumpChargeDestination;
        private int ChargeTime;


        // TODO: refactor
        private int GetBulletAngle(int n, int spread, int numBullets)
        {
            if (spread != 0)
            {
                int d = (-spread / 2) / 2;
                for (int i = 0; i < n; i++)
                {
                    d += (spread / 2) / numBullets;
                }
                return d;
            }
            else
            {
                int d = (-spread / 2) / 2;
                for (int i = 0; i < n; i++)
                {
                    d += 4;
                }
                return d;
            }
        }

        private int SpreadIndex = 0;


        private void Attack(int x, int y, int range, ProjectileData projectileData, int damage, int spread, int bulletsPerShot, LogicSkillServer skill)
        {
            int originAngle = LogicMath.GetAngle(x, y);
            SetForcedVisible();

            if (m_holdingSkill)
            {
                if (m_skillHoldTicksGone > 14) bulletsPerShot = 1;
                else if (m_skillHoldTicksGone > 5) bulletsPerShot = 3;
            }

            for (int i = 0; i < bulletsPerShot; i++)
            {
                LogicProjectileServer projectile = new LogicProjectileServer(6, projectileData.GetInstanceId());
                projectile.MaxRange = skill.SkillData.CastingRange;

                int newRange = range / 2;

                if (m_holdingSkill)
                    newRange += skill.GetSkillRangeAddFromHold(m_skillHoldTicksGone);

                int a = LogicMath.Min(m_skillHoldTicksGone, MAX_SKILL_HOLD_TICKS) / 3;

                projectile.SetTargetPosition(GetX() + x, GetY() + y);

                if (!skill.IsRapidSpreadPattern)
                {
                    projectile.ShootProjectile(originAngle + GetBulletAngle(i, spread, bulletsPerShot) / (a != 0 ? a : 1), this, GetAbsoluteDamage(damage), newRange + 1, skill == GetUltimateSkill());
                }
                else
                {
                    projectile.ShootProjectile(originAngle + skill.ATTACK_PATTERN_TABLE[SpreadIndex] / (a != 0 ? a : 1), this, GetAbsoluteDamage(damage), newRange + 1, skill == GetUltimateSkill());
                    SpreadIndex++;
                    if (SpreadIndex >= skill.ATTACK_PATTERN_TABLE.Length) SpreadIndex = 0;
                }

                if (skill.SkillData.SummonedCharacter != null)
                {
                    CharacterData summonedCharacter = DataTables.Get(DataType.Character).GetData<CharacterData>(skill.SkillData.SummonedCharacter);
                    if (summonedCharacter != null)
                    {
                        projectile.SetSummonedCharacter(summonedCharacter);
                    }
                }

                GameObjectManager.AddGameObject(projectile);
            }
            m_holdingSkill = false;
        }


        public override bool ShouldDestruct()
        {
            return m_hitpoints <= 0;
        }

        public bool IsChargeActive()
        {
            return m_activeChargeType >= 0;
        }

        public int GetHitpointPercentage()
        {
            return (int)(((float)this.m_hitpoints / (float)this.m_maxHitpoints) * 100f);
        }

        public bool HasActiveSkill()
        {
            if (m_skills.Count == 0) return false;
            if (m_skills.Count == 1) return m_skills[0].IsActive;
            else return m_skills[0].IsActive || m_skills[1].IsActive;
        }

        public void SetInvisibility(bool invisibility)
        {
            m_invis = invisibility;
        }

        public void SetSlowDown(bool slowdown, int slowpower)
        {
            m_slowdown = true;
            m_slowpower = slowpower;
        }

        public void ResetSlowDown()
        {
            m_slowdown = false;
            m_slowpower = 0;
        }

        public void Buff(int power)
        {
            buffed = true;
            m_damageMultiplier++;
        }

        public void DeBuff()
        {
            buffed = false;
            m_damageMultiplier--;
        }

        public void SeSlowDownDefault(bool slowdown)
        {
            m_slowdown = true;
        }

        public int GetMsBetweenAttacks(string skillname)
        {
            return SkillData.MsBetweenAttacks;
        }



        public void HandleBotMoveAndAttack(int botLvl)
        {

        }

        private async void HandleMoveAndAttack()
        {
            if (!HasActiveSkill())
                m_attackingTicks = 63;

            if (m_isStunned)
            {
                m_ticksGoneSinceStunned++;
                if (m_ticksGoneSinceStunned > 40)
                {
                    m_isStunned = false;
                }
                return;
            }

            foreach (LogicGameObjectServer obj in GameObjectManager.GetGameObjects())
            {
                try
                {
                    if (Position.GetDistance(obj.GetPosition()) <= 200 && obj.GetIndex() / 16 != GetIndex() / 16)
                    {
                        obj.SetForcedVisible();
                    }

                    if (CharacterData.IsHero())
                    {
                        if (obj.GetObjectType() == 4)
                        {
                            LogicItemServer item = (LogicItemServer)obj;
                            if (Position.GetDistance(item.GetPosition()) < 350 && item.CanBePickedUp())
                            {
                                item.PickUp(this);
                            }
                        }
                    }

                }
                catch (Exception error)
                {
                    foreach (BattlePlayer player in PBattle.m_players)
                    {
                        var message = new ServerErrorMessage(43);
                        try
                        {
                            player.GameListener.SendTCPMessage(message);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                        }
                    }
                    Console.WriteLine("Error in Character.cs:" + error);
                }
            }

            if (this.m_meleeAttackEndTick == this.GameObjectManager.GetBattle().GetTicksGone())
            {
                if (this.m_meleeAttackTarget != null)
                    this.m_meleeAttackTarget.CauseDamage(null, this.m_meleeAttackDamage);
            }

            // Handle Attack
            foreach (LogicSkillServer skill in m_skills)
            {
                if (!skill.IsActive) continue;

                if (skill.SkillData.BehaviorType == "Attack")
                {
                    if (!skill.ShouldAttackThisTick()) continue;

                    ProjectileData projectileData = DataTables.Get(DataType.Projectile).GetData<ProjectileData>(skill.SkillData.Projectile);

                    int damage = skill.SkillData.Damage;
                    int spread = skill.SkillData.Spread;
                    int bulletsPerShot = skill.SkillData.NumBulletsInOneAttack;

                    this.Attack(skill.X, skill.Y, skill.SkillData.CastingRange, projectileData, damage, spread, bulletsPerShot, skill);
                    if (m_attackingTicks < 63)
                    {
                        m_state = 0;
                    }
                }
                else if (skill.SkillData.BehaviorType == "Charge")
                {
                    if (GamePlayUtil.IsJumpCharge(skill.SkillData.ChargeType))
                    {
                        // не придумали
                        return;
                    }

                    m_activeChargeType = skill.SkillData.ChargeType;

                    int dx = LogicMath.Cos(m_angle) / 100;
                    int dy = LogicMath.Sin(m_angle) / 100;

                    dx *= skill.SkillData.ChargeSpeed / 80;
                    dy *= skill.SkillData.ChargeSpeed / 80;

                    if (!GameObjectManager.GetBattle().IsInPlayArea(Position.X + dx, Position.Y + dy))
                    {
                        skill.Interrupt();
                        m_activeChargeType = -1;
                        return;
                    }

                    Tile tile = GameObjectManager.GetBattle().GetTileMap().GetTile(Position.X + dx, Position.Y + dy);

                    if (tile == null)
                    {
                        skill.Interrupt();
                        m_activeChargeType = -1;
                        return;
                    }

                    if (tile.Data.BlocksMovement && !tile.IsDestructed())
                    {
                        if (tile.Data.IsDestructible)
                        {
                            tile.Destruct();
                        }
                        else
                        {
                            skill.Interrupt();
                            m_activeChargeType = -1;
                            return;
                        }
                    }

                    Position.X += dx;
                    Position.Y += dy;


                    if (skill.ShouldEndThisTick) m_activeChargeType = -1;

                }
                else
                {
                    Debugger.Warning("Unknown skill type: " + skill.SkillData.BehaviorType);
                }
            }


            // Handle Move
            if (m_isMoving && !IsChargeActive())
            {
                if (Position.GetDistance(m_movementDestination) != 0)
                {

                    int angle = m_angle;
                    int initialDestX = m_movementDestination.X;
                    int initialDestY = m_movementDestination.Y;
                    bool isBot = this.m_isBot || !CharacterData.IsHero();
                    if (isBot)
                    {
                        while (CheckObstacle(15))
                        {
                            m_movementDestination.X = initialDestX;
                            m_movementDestination.Y = initialDestY;

                            m_movementDestination.X = Position.X;
                            m_movementDestination.Y = Position.Y;

                            angle += 2;

                            m_movementDestination.X += LogicMath.Cos(angle);
                            m_movementDestination.Y += LogicMath.Sin(angle);
                        }
                    }
                    else
                    {
                        if (CheckObstacle(1))
                            this.StopMovement();
                    }

                    var deltaXproto = _requiredPosition.Clone().GetX() - GetPosition().Clone().GetX() > 0
                            ? LogicMath.Min(CharacterData.Speed,
                            _requiredPosition.Clone().GetX() - GetPosition().Clone().GetX())
                            : LogicMath.Max(-CharacterData.Speed,
                            _requiredPosition.Clone().GetX() - GetPosition().Clone().GetX());

                    var deltaYproto = _requiredPosition.Clone().GetY() - GetPosition().Clone().GetY() > 0
                            ? LogicMath.Min(CharacterData.Speed,
                            _requiredPosition.Clone().GetY() - GetPosition().Clone().GetY())
                            : LogicMath.Max(-CharacterData.Speed,
                            _requiredPosition.Clone().GetY() - GetPosition().Clone().GetY());

                    //m_angle = LogicMath.NormalizeAngle360(angle);

                    int movingSpeed = 0;
                    if (!m_slowdown && m_slowpower <= 0) movingSpeed = CharacterData.Speed / 20;
                    else if (m_slowdown && m_slowpower > 0) movingSpeed = CharacterData.Speed / 20 - m_slowpower;

                    int deltaX = 0;
                    int deltaY = 0;


                    if (m_movementDestination.X - Position.X != 0)
                    {
                        if (m_movementDestination.X - Position.X > 0) deltaX = LogicMath.Min(movingSpeed, m_movementDestination.X - Position.X);
                        else deltaX = m_movementDestination.Clone().GetX() - GetPosition().Clone().GetX() > 0
                        ? LogicMath.Min(movingSpeed, m_movementDestination.Clone().GetX() - GetPosition().Clone().GetX())
                        : LogicMath.Max(-movingSpeed, m_movementDestination.Clone().GetX() - GetPosition().Clone().GetX());

                        Position.X += deltaX;
                    }
                    if (m_movementDestination.Y - Position.Y != 0)
                    {
                        if (m_movementDestination.Y - Position.Y > 0) deltaY = LogicMath.Min(movingSpeed, m_movementDestination.Y - Position.Y);
                        else deltaY = m_movementDestination.Clone().GetY() - GetPosition().Clone().GetY() > 0
                        ? LogicMath.Min(movingSpeed, m_movementDestination.Clone().GetY() - GetPosition().Clone().GetY())
                        : LogicMath.Max(-movingSpeed, m_movementDestination.Clone().GetY() - GetPosition().Clone().GetY());

                        Position.Y += deltaY;
                    }
                    int _oldDa = m_angle;
                    if (m_attackingTicks > 60)
                    {
                        if (LogicMath.Abs(m_angle -
                                          LogicMath.NormalizeAngle360(LogicMath.GetAngle(deltaX, deltaY))) > 1)
                            if (LogicMath.Abs(_oldDa -
                                              LogicMath.NormalizeAngle360(LogicMath.GetAngle(deltaX, deltaY))) >
                                1)
                                if ((LogicMath.Cos(deltaXproto) + LogicMath.Sin(deltaYproto)) / 360 <= 360)
                                {
                                    m_angle =
                                        LogicMath.NormalizeAngle360(LogicMath.GetAngle(deltaX, deltaY));
                                }
                        m_state = 0;
                        m_attackingTicks = 0;


                    }

                }



                m_isMoving = Position.GetDistance(m_movementDestination) != 0;
                if (!m_isMoving)
                {
                    m_state = 4;
                }
            }
        }

        public int GetMoveAngle()
        {
            return m_angle;
        }

        public void SetFinalPosition(int x, int y)
        {
            GetPosition().Set(x, y);

            var v1 = LogicMath.GetRotatedX(x, y, GetMoveAngle());
            var v2 = LogicMath.GetRotatedY(x, y, GetMoveAngle());
            var v3 = LogicMath.SqrtApproximate(x - GetX(), y - GetY());

            var a1 = (v3 - 20) * (x - GetX()) / v3;
            var a2 = (v3 - 20) * (y - GetY()) / v3;

            var c2 = a1 + GetX();
            var c3 = a2 + GetY();

            GetPosition().Set(Convert.ToInt32(c2), Convert.ToInt32(c3));
        }
        public void TriggerStun(int ticks)
        {
            _stunnedTick = this.GameObjectManager.GetBattle().GetTicksGone() - this.GameObjectManager.GetBattle().GetTicksGone() / 2 < ticks
                ? ticks
                : this.GameObjectManager.GetBattle().GetTicksGone() + ticks;
        }

        public int TransformPushBackLengthToStrength(int a1)
        {
            return a1 / 6;
        }

        public bool IsImmuneToPushbackFromCharge(int a1)
        {
            return m_immunity != null! || a1 <= 0 ||
                   (!CharacterData.IsHero() && CharacterData.IsBoss());
        }

        public void TriggerPushback(int pushbackStrength, int pushbackAngle, int pushbackEarlyTicks, bool reversepushback)
        {
            _receivedPushbackStrength = pushbackStrength * 1.11f;
            _receivedPushbackAngle = pushbackAngle;
            _receivedPushbackEarlyTicks = pushbackEarlyTicks;
            if (reversepushback) m_angle = 90;
            _nowPushbackTicker = 0;

            InterruptAllSkills();
        }

        private bool CheckObstacle(int nextTiles)
        {
            int movingSpeed = CharacterData.Speed / 20;
            int deltaX;
            int deltaY;

            int newX = this.Position.X;
            int newY = this.Position.Y;

            for (int i = 0; i < nextTiles; i++)
            {
                if (m_movementDestination.X - Position.X > 0) deltaX = LogicMath.Min(movingSpeed, m_movementDestination.X - Position.X);
                else deltaX = LogicMath.Max(-movingSpeed, m_movementDestination.X - Position.X);

                if (m_movementDestination.Y - Position.Y > 0) deltaY = LogicMath.Min(movingSpeed, m_movementDestination.Y - Position.Y);
                else deltaY = LogicMath.Max(-movingSpeed, m_movementDestination.Y - Position.Y);

                newX += deltaX;
                newY += deltaY;

                if (!GameObjectManager.GetBattle().IsInPlayArea(newX, newY)) return true;

                Tile nextTile = GameObjectManager.GetBattle().GetTileMap().GetTile(newX, newY);
                if (nextTile == null) return true;
                if (nextTile.Data.BlocksMovement && !nextTile.IsDestructed()) return true;
            }

            return false;
        }


        public void IncreaseSize(int buffStack)
        {
            //if (buffStack == -1) _itemPickTick = 0;
        }

        public void UltiEnabled()
        {
            m_usingUltiCurrently = true;
        }

        public void UltiDisabled()
        {
            m_usingUltiCurrently = false;
        }

        public override bool IsAlive()
        {
            return m_hitpoints > 0;
        }

        public override int GetRadius()
        {
            return CharacterData.CollisionRadius;
        }

        public void SetHeroLevel(int level)
        {
            m_heroLevel = level;
            m_maxHitpoints = CharacterData.Hitpoints + ((int)(((float)5 / 100) * (float)CharacterData.Hitpoints)) * level;
            m_hitpoints = m_maxHitpoints;
            m_damageMultiplier = level;
        }

        public int GetHeroLevel()
        {
            return m_heroLevel;
        }

        public void InterruptAllSkillsAgain()
        {
            //_interruptSkillsTick = _battleMode.GetTicksGone() + 10;
        }

        public int GetNormalWeaponDamage()
        {
            return WeaponSkillData.Damage + ((int)(((float)5 / 100) * (float)WeaponSkillData.Damage)) * (m_heroLevel + m_damageMultiplier);
        }

        public int GetAbsoluteDamage(int damage)
        {
            return damage + ((int)(((float)5 / 100) * (float)damage)) * (m_heroLevel + m_damageMultiplier);
        }

        public void SetPos(int pos)
        {
            m_angle = pos;
            Console.WriteLine(m_angle);
        }
        private bool roblox = false;
        public void ReloadFullAmmo()
        {
            roblox = true;
        }

        public void PizdaGleba()
        {
            roblox = false;
        }

        public void changeAngle(int angle)
        {
            m_angle = angle;
        }

        public void MoveTo(int x, int y)
        {
            if (!GameObjectManager.GetBattle().IsInPlayArea(x, y)) return;
            if (IsChargeActive()) return;

            m_isMoving = true;
            if (m_attackingTicks >= 63) m_state = 1;
            m_movementDestination = new LogicVector2(x, y);

            LogicVector2 delta = m_movementDestination.Clone();
            delta.Substract(Position);

            if (!((delta.X < 150 && delta.X > -150) && (delta.Y < 150 && delta.Y > -150)))
            {
                m_angle = LogicMath.GetAngle(delta.X, delta.Y);
            }
        }


        public override void Encode(BitStream bitStream, bool isOwnObject, int visionTeam)
        {

            var damageEntryList = Helper.SumRepeatedElements(m_damageIndicator);



            isOwnObject = isOwnObject && CharacterData.IsHero();
            base.Encode(bitStream, isOwnObject, visionTeam);
            bitStream.WritePositiveInt(visionTeam == this.GetIndex() / 16 ? 10 : GetFadeCounter(), 4);

            if (CharacterData.HasAutoAttack() || CharacterData.Speed != 0 || CharacterData.Type == "Minion_Building_charges_ulti")
            {
                if (isOwnObject)
                {
                    bitStream.WriteBoolean(false); // 0xa1aff8
                    bitStream.WriteBoolean(false);
                }
                else
                {
                    bitStream.WritePositiveIntMax511(m_angle);// m_angle);
                    bitStream.WritePositiveIntMax511(m_angle);// m_angle);
                }
                bitStream.WritePositiveIntMax7(m_state); // State
                bitStream.WriteBoolean(buffed); // Coctail used
                bitStream.WriteInt(LogicMath.Clamp(m_attackingTicks, 0, 63), 6); // Animation Playing

                bitStream.WriteBoolean(false); // дёргает и не rotate
                bitStream.WriteBoolean(_stunnedTick > 0); // Stun
                bitStream.WriteBoolean(false); // is gadget 
                bitStream.WriteBoolean(false); // is starpower
            }
            else
            {
                bitStream.WritePositiveIntMax7(m_state);
                if (CharacterData.Type == "Train")
                {
                    bitStream.WritePositiveIntMax511(m_angle);
                    bitStream.WritePositiveIntMax511(m_angle);
                }
                else if (CharacterData.AreaEffect != null)
                {
                    bitStream.WritePositiveIntMax511(m_angle);// m_angle);
                }
            }

            bitStream.WritePositiveVIntMax255OftenZero(0); // 0xa1b0d8
            bitStream.WritePositiveVIntMax255OftenZero(0); // bibi(home run charge)
            bitStream.WriteBoolean(false); // Speed up (Arrows up) false
            bitStream.WriteBoolean(m_slowpower > 0); // Slow down

            if (m_poison != null)
            {
                bitStream.WritePositiveInt(m_poison.GetPoisonType(), 2);
                {
                    bitStream.WriteBoolean(m_poison.HasSlowDownEffect());
                }
            }
            else
            {
                bitStream.WritePositiveInt(0, 2);
            }

            if (CharacterData.HasVeryMuchHitPoints() || GameObjectManager.GetBattle().GetGameModeVariation() == 6)
            {
                bitStream.WritePositiveVIntMax65535(m_hitpoints);
                bitStream.WritePositiveVIntMax65535(m_maxHitpoints);
            }
            else
            {
                bitStream.WritePositiveInt(m_hitpoints, 13);
                bitStream.WritePositiveInt(m_maxHitpoints, 13);
            }



            if (CharacterData.IsHero())
            {
                bitStream.WritePositiveVIntMax255OftenZero(m_itemCount);
                bitStream.WritePositiveVIntMax255OftenZero(0);

                bitStream.WriteBoolean(false); // big brawler
                bitStream.WriteBoolean(true); // poison and down arrows effect(maybe crow gadget)
                {
                    bitStream.WriteBoolean(false); // bugging visior
                    bitStream.WriteBoolean(m_immunity != null); // immunity
                    bitStream.WriteBoolean(false); // будто ебашит героин
                    bitStream.WriteBoolean(false); // bull rage
                    bitStream.WriteBoolean(m_usingUltiCurrently); // using ulti
                    bitStream.WriteBoolean(false); // ulti activated???? 2
                    bitStream.WriteBoolean(false); // gold shield (кста возможно shield для персов, я хз) 
                    bitStream.WriteBoolean(false); // unknown?
                }

                if (isOwnObject) bitStream.WriteBoolean(false); // leon clone have ulti

                if (isOwnObject) bitStream.WritePositiveInt(0, 4);

                if (IsChargeActive())
                {
                    bitStream.WritePositiveInt(0, 8);
                    bitStream.WritePositiveInt(0, 4);
                }
            }


            bitStream.WritePositiveInt(0, 2); // global effect
            bitStream.WriteBoolean(m_invis); // maybe in bushes?
            bitStream.WritePositiveInt(0, 9);


            if (isOwnObject)
            {
                bool IsSpeedBuff = false;
                bitStream.WriteBoolean(IsSpeedBuff); // speed buff percentage
                if (IsSpeedBuff) bitStream.WriteInt(99, 10);
                bitStream.WriteBoolean(roblox); // шоколадный глаз тары
            }

            bitStream.WritePositiveIntMax31(damageEntryList.Count);
            if (damageEntryList.Count > 0)
                foreach (var damage in damageEntryList.ToList())
                    bitStream.WriteInt(damage, 15);



            if (m_skills == null)
            {
                Console.WriteLine("Warning: m_skills is null.");
            }
            else
            {
                for (int i = 0; i < m_skills.Count; i++)
                {
                    if (m_skills[i] == null)
                    {
                        Console.WriteLine($"Warning: m_skills[{i}] is null.");
                    }
                    else
                    {
                        try
                        {
                            m_skills[i].Encode(bitStream);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error encoding skill at index {i}: {ex.Message}");
                            throw;
                        }
                    }
                }
            }
        }


        public override int GetObjectType()
        {
            return 1;
        }
    }
}
