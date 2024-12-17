using Supercell.Laser.Logic.Battle.Objects;
using Supercell.Laser.Logic.Data;
using Supercell.Laser.Titan.DataStream;

namespace Supercell.Laser.Logic.Battle.LogicPlayerModifications
{
    public class LogicAccessory
    {
        private const bool V2AvailableCheck = true;
        public int писюн;
        private readonly LogicCharacterServer _logicCharacterServer;
        private int penis;

        private readonly bool _exclusiveAccessory;
        private bool _exclusiveAccessoryDoubleTrigger;

        private int _cooldownTick = 100;
        private int _deltaTick;
        private bool _firstUse;

        private int _nowCount = 1488;
        private bool NowUse;
        private bool PastUse;

        public LogicAccessory(LogicBattleModeServer battleMode, LogicCharacterServer logicCharacterServer) //data accessory todo
        {
            _logicCharacterServer = logicCharacterServer;


            _exclusiveAccessory = false;
        }

        public void UpdateAccessory()
        {
            if (!_exclusiveAccessory)
            {
                NowUse = _cooldownTick != 0;
            }
            else
            {
                if (_exclusiveAccessoryDoubleTrigger && _firstUse && !PastUse)
                {
                    _cooldownTick = 3;
                    _deltaTick = _cooldownTick - 1;
                    PastUse = true;
                }
                else
                {
                    NowUse = !_exclusiveAccessoryDoubleTrigger;
                    _cooldownTick = NowUse ? 1 : PastUse ? 3 : 0;
                    _deltaTick = NowUse ? 0 : PastUse ? 3 : 0;
                }
            }

            if (!_exclusiveAccessory)
            {
                if (_nowCount > 0)
                {
                    if (_cooldownTick > 0) _deltaTick--;
                    if (_deltaTick >= 1) return;

                    _cooldownTick = 0;
                    _deltaTick = 0;
                }
                else
                {
                    _nowCount = 0;
                    _cooldownTick = 3;
                    _deltaTick = 0;
                }
            }
            else
            {
                if (_nowCount <= 0) return;
                if (_cooldownTick <= 0 || _deltaTick <= 0 || !PastUse) return;

                _deltaTick--;
                if (_deltaTick != 0) return;

                _cooldownTick = 0;
                _deltaTick = 0;

                _firstUse = false;
                PastUse = false;
                NowUse = false;
            }
        }

        public void CreateAreaEffect(string name, int x, int y, int z, int index, int damage, LogicCharacterServer source, LogicGameObjectManagerServer gameObjectManager)
        {
            AreaEffectData data = DataTables.Get(DataType.AreaEffect).GetData<AreaEffectData>(name);

            LogicAreaEffectServer effect = new LogicAreaEffectServer(null, 17, data.GetInstanceId());
            effect.SetPosition(x, y, z);
            effect.SetIndex(index);
            effect.SetDamage(damage);
            effect.SetSource(source);

            gameObjectManager.AddGameObject(effect);
        }

        public void TriggerAccessory(bool exclusiveAccessoryDoubleTrigger = false)
        {

            _exclusiveAccessoryDoubleTrigger = exclusiveAccessoryDoubleTrigger;
            if (_exclusiveAccessoryDoubleTrigger) return;

            if (_nowCount < 1) return;
            if (_deltaTick > 0) return;
            if (V2AvailableCheck && CheckCurrentAccessoryAvailability(true)) return;

            _firstUse = true;
            _cooldownTick = 3;
            _deltaTick = _cooldownTick;

            _nowCount--;
            NowUse = true;
            string _gadgetType = "dash";
            if (_deltaTick > 0)
            {
                switch (_gadgetType)
                {
                    case "dash":
                        _logicCharacterServer.MoveTo(_logicCharacterServer.GetX() + 900, _logicCharacterServer.GetY() + 2000); // testim
                        break;
                    case "consume_bush": // custiki nyam nyam
                        break;
                    case "jump":
                        break;
                    case "repeat_area":
                        break;
                    case "heal":
                        // мне лень
                        break;
                    case "speed":
                        break;
                    case "spin_shoot":
                        break;
                    case "teleport_to_pet":
                        break;
                    case "cc_immunity":
                        break;
                    case "trail":
                        break;
                    case "spawn":
                        break;
                    case "throw_opponent":
                        break;
                    case "vision":
                        _logicCharacterServer.SetAllVisibleByGadget();
                        break;
                    case "repeat_shot":
                        break;
                    case "promote_minion":
                        break;
                    case "reload":
                        break;
                    case "kill_projectile":
                        break;

                }
            }
        }

        public bool CheckCurrentAccessoryAvailability(bool v2Check)
        {
            if (v2Check) return _cooldownTick != 0 || _deltaTick != 0;
            return _exclusiveAccessory && NowUse;
        }

        public void Encode(BitStream bitStream, bool isOwn)
        {
            bitStream.WritePositiveIntMax7(_nowCount);
            if (isOwn)
            {
                bitStream.WritePositiveVIntMax255OftenZero(_deltaTick);
                if (bitStream.WritePositiveVIntMax255OftenZero(_cooldownTick) != 1) return;

                bitStream.WritePositiveIntMax16383(0);
                bitStream.WritePositiveIntMax511(0);
            }
            else
            {
                if (!bitStream.WriteBoolean(NowUse && _exclusiveAccessory)) return;

                bitStream.WritePositiveIntMax16383(0);
                bitStream.WritePositiveIntMax511(0);
            }
        }
    }
}
