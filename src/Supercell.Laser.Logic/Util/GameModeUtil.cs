namespace Supercell.Laser.Logic.Util
{
    public static class GameModeUtil
    {
        public static bool PlayersCollectPowerCubes(int variation)
        {
            int v1 = variation - 6;
            if (v1 <= 8)
                return ((0x119 >> v1) & 1) != 0;
            else
                return false;
        }

        public static int GetRespawnSeconds(int variation)
        {
            switch (variation)
            {
                case 0:
                case 2:
                    return 3;
                case 3:
                    return 1;
                default:
                    return 5;
            }
        }

        public static int GetRespwanSecondsForTask(int variation)
        {
            switch (variation)
            {
                case 0:
                case 2:
                    return 3000;
                case 3:
                    return 1000;
                default:
                    return 5000;
            }
        }

        public static bool PlayersCollectBountyStars(int variation)
        {
            return variation == 3 || variation == 15;
        }

        public static bool HasTwoTeams(int variation)
        {
            if (variation == 6 || variation == 7 || variation == 8 || variation == 9 ||
                variation == 10 || variation == 13 || variation == 15)
            {
                return false;
            }

            return true;
        }


        public static int GetGameModeVariation(string mode)
        {
            switch (mode)
            {
                case "CoinRush": // gem grab
                    return 0;
                case "AttackDefend": // heist
                    return 2;
                case "BossFight": // big game
                    return 7;
                case "BountyHunter": // bounty
                    return 3;
                case "Artifact": // deleted
                    return 4;
                case "LaserBall": // brawl ball
                    return 5;
                case "BattleRoyale": // solo shd
                    return 6;
                case "BattleRoyaleTeam": // duo shd
                    return 9;
                case "Survival": // roborumble
                    return 8;
                case "Raid": // boss fight
                    return 10;
                case "RoboWars": // siege
                    return 11;
                case "Tutorial":
                    return 12;
                case "Training":
                    return 13;
                case "CaptureTheFlag":
                    return 16;
                case "LoneStar":
                    return 15;
                default:
                    //Debugger.Error("Wrong game mode!");
                    return 0;
            }
        }
    }
}
