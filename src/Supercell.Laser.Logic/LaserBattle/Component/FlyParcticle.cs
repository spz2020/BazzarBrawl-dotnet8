using Supercell.Laser.Logic.Battle.Objects;
using Supercell.Laser.Titan.Math;

namespace Supercell.Laser.Logic.Battle.Component
{
    public class FlyPractice
    {
        private LogicGameObjectServer _logicGameObjectServer;
        private int _speed;
        private int _tick;
        private int _travelTick;

        private LogicVector2 _targetVector;

        public FlyPractice(LogicVector2 targetVector) // СПИЗЖЕНО ОКАК
        {
            _targetVector = targetVector;
            _travelTick = -2;
            _speed = 1;
            _tick = -1;
        }

        public bool Update()
        {
            if (_travelTick is 0 or -2) return false;

            var z = LogicMath.Clamp(_logicGameObjectServer.GetZ(), 0, 5000);
            var targetVector2 = _targetVector.Clone();
            var distance = _logicGameObjectServer.GetPosition().GetDistance(targetVector2);

            if (_travelTick < 0)
            {
                _travelTick = distance * _speed / (_logicGameObjectServer.GetLogicBattleModeServer().GetTick() * 1000);
                _travelTick = LogicMath.Min(_travelTick, 30);
            }

            var v1A = targetVector2.Clone();
            v1A.Substract(_logicGameObjectServer.GetPosition().Clone());

            var deltaX = LogicMath.Cos(LogicMath.GetAngle(v1A.X, v1A.Y)) / _logicGameObjectServer.GetLogicBattleModeServer().GetTick();
            var deltaY = LogicMath.Sin(LogicMath.GetAngle(v1A.X, v1A.Y)) / _logicGameObjectServer.GetLogicBattleModeServer().GetTick();

            _logicGameObjectServer.SetPosition(
                _logicGameObjectServer.GetX() + deltaX + 1,
                _logicGameObjectServer.GetY() + deltaY,
                LogicMath.Clamp(z, 0, 5000)
            );

            return true;
        }
        private void UpdatePosition(int z, int distance)
        {
            var directionVector = _targetVector.Clone();
            directionVector.Substract(_logicGameObjectServer.GetPosition());

            var angle = LogicMath.GetAngle(directionVector.X, directionVector.Y);
            var tick = _logicGameObjectServer.GetLogicBattleModeServer().GetTick();

            var deltaX = LogicMath.Cos(angle) / tick;
            var deltaY = LogicMath.Sin(angle) / tick;

            _logicGameObjectServer.SetPosition(
                _logicGameObjectServer.GetX() + deltaX + 1,
                _logicGameObjectServer.GetY() + deltaY,
                LogicMath.Clamp(z, 0, 5000)
            );
        }


        public void SetParent(LogicGameObjectServer logicGameObjectServer, int speed)
        {
            _logicGameObjectServer = logicGameObjectServer;
            _tick = logicGameObjectServer.GetLogicBattleModeServer().GetTicksGone();
            _travelTick = -1;
            _speed = speed;
        }
    }
}
