using Supercell.Laser.Titan.Math;

namespace Supercell.Laser.Logic.Util
{
    public static class GamePlayUtil
    {
        public static bool IsJumpCharge(int chargeType)
        {
            uint v1 = (uint)(chargeType - 2);
            if (v1 <= 9)
                return ((0x293u >> (int)v1) & 1) != 0;
            else
                return false;
        }

        public static int GetDistanceBetween(int a1, int a2, int a3, int a4)
        {
            return LogicMath.Sqrt((a3 - a1) * (a3 - a1) + (a4 - a2) * (a4 - a2));
        }

        public static int GetDistanceSquaredBetween(int a1, int a2, int a3, int a4)
        {
            return (a3 - a1) * (a3 - a1) + (a4 - a2) * (a4 - a2);
        }

        public static int GetPlayerCountWithGameModeVariation(int gameMode)
        {
            switch (gameMode)
            {
                case 0:
                    return 6;
                case 2:
                    return 6;
                case 3:
                    return 6;
                case 5:
                    return 6;
                case 6:
                    return 10;
                case 7:
                    return 6;
                case 8:
                    return 3;
                case 9:
                    return 10;
                case 10:
                    return 3;
                case 11:
                    return 6;
                case 12:
                    return 1;
                case 13:
                    return 1;
                case 14:
                case 15:
                    return 10;
                case 16:
                    return 6;
                case 17:
                    return 6;
                case 18:
                    return 3;
                case 19:
                case 20:
                case 21:
                case 22:
                case 23:
                    return 6;
                case 24:
                    return 2;
                case 25:
                case 26:
                case 27:
                    return 6;
            }
            return -1;
        }
    }
}
