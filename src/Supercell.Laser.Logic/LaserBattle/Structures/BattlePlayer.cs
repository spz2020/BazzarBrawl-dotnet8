namespace Supercell.Laser.Logic.Battle.Structures
{
    using Newtonsoft.Json;
    using Supercell.Laser.Logic.Avatar;
    using Supercell.Laser.Logic.Avatar.Structures;
    using Supercell.Laser.Logic.Battle.Objects;
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Logic.Data.Helper;
    using Supercell.Laser.Logic.Helper;
    using Supercell.Laser.Logic.Home;
    using Supercell.Laser.Logic.Home.Structures;
    using Supercell.Laser.Logic.Listener;
    using Supercell.Laser.Titan.DataStream;
    using Supercell.Laser.Titan.Math;

    public class BattlePlayer
    {
        public long AccountId;
        public int PlayerIndex;
        public int TeamIndex;

        public long TeamId = -1;

        public PlayerDisplayData DisplayData;
        public int CharacterId;
        public int SkinId;

        public long SessionId;
        public LogicGameListener GameListener;

        public int OwnObjectId;
        public int LastHandledInput;

        public int Trophies, HighestTrophies;

        public int HeroPowerLevel;

        private int Score;
        private LogicVector2 SpawnPoint;

        private int StartUsingPinTicks;
        private int PinIndex;

        private bool Bot;

        private int UltiCharge;

        public bool IsAlive;
        public int BattleRoyaleRank;

        public List<PlayerKillEntry> KillList;
        public List<PinData> PinList;

        public int Kills;
        public int Damage;
        public int Heals;


        public BattlePlayer()
        {
            DisplayData = new PlayerDisplayData();
            SpawnPoint = new LogicVector2();
            TeamIndex = 0;
            //Console.WriteLine("Display Checkout: " + DisplayData);
            //Console.WriteLine("Spawn Checkout: " + SpawnPoint);



            StartUsingPinTicks = -9999;

            HeroPowerLevel = 0;
            BattleRoyaleRank = -1;
            IsAlive = true;
            PinList = new List<PinData>();
            KillList = new List<PlayerKillEntry>();
        }

        public void Healed(int heals)
        {
            Heals += heals;
        }

        public void DamageDealed(int damage)
        {
            Damage += damage;
        }

        public void KilledPlayer(int index, int bountyStars)
        {
            KillList.Add(new PlayerKillEntry()
            {
                PlayerIndex = index,
                BountyStarsEarned = bountyStars
            });

            Kills++;
        }

        public bool HasUlti()
        {
            return UltiCharge >= 4000;
        }

        public int GetUltiCharge()
        {
            return UltiCharge;
        }

        public void AddUltiCharge(int amount)
        {
            UltiCharge = LogicMath.Min(4000, UltiCharge + amount);
        }

        public void UseUlti()
        {
            UltiCharge = 0;
        }

        public bool IsBot()
        {
            return Bot;
        }

        public BattlePlayer(ClientHome home, ClientAvatar avatar) : this()
        {
            Home = home;
            Avatar = avatar;
        }

        public void AddScore(int a)
        {
            Score += a;
        }

        public void ResetScore()
        {
            Score = 0;
        }

        public void UsePin(int index, int ticks)
        {
            StartUsingPinTicks = ticks;
            PinIndex = index;
        }

        public bool IsUsingPin(int ticks)
        {
            return ticks - StartUsingPinTicks < 80;
        }

        public int GetPinIndex()
        {
            return PinIndex;
        }

        public int GetHeroPowerLVL()
        {
            //if (HeroPowerLevel < 0 || HeroPowerLevel > 10) return -1;
            return 0;
        }

        public int GetTeamIndex()
        {
            return 0;
        }

        public int GetPinUseCooldown(int ticks)
        {
            return StartUsingPinTicks + 100;
        }

        public int GetScore()
        {
            return Score;
        }

        public void SetSpawnPoint(int x, int y)
        {
            SpawnPoint.Set(x, y);
        }

        public LogicVector2 GetSpawnPoint()
        {
            return SpawnPoint.Clone();
        }
        public bool HeroData;
        public int HeroDataAccessory;
        public int HeroDataStarpower;
        public int HeroDataCount;
        public void Encode(ByteStream stream)
        {

            stream.WriteLong(AccountId);
            stream.WriteVInt(PlayerIndex);
            stream.WriteVInt(TeamIndex);

            stream.WriteVInt(0);
            stream.WriteInt(0);

            ByteStreamHelper.WriteDataReference(stream, CharacterId);
            LogicCharacterServer character = new LogicCharacterServer(null, 0, 0);
            if (TeamIndex == 1) character.SetPos(90);
            if (TeamIndex == 0) character.SetPos(270);
            ByteStreamHelper.WriteDataReference(stream, -1);// SkinId > 0 ? 29000000 + SkinId : 0);
            stream.WriteBoolean(HeroData); // CardInfo
            if (HeroData == true)
            {
                stream.WriteVInt(2);
                ByteStreamHelper.WriteDataReference(stream, 23000000 + 76); // starpower
                ByteStreamHelper.WriteDataReference(stream, 23000000 + 255); // accessory
            }

            stream.WriteBoolean(false);
            //new LogicBattleEmotes()?.Encode(stream); // pins bool
            DisplayData.Encode(stream);
        }

        [JsonIgnore] public readonly ClientHome Home;
        [JsonIgnore] public readonly ClientAvatar Avatar;

        public static BattlePlayer Create(ClientHome home, ClientAvatar avatar, int playerIndex, int teamIndex)
        {
            BattlePlayer player = new BattlePlayer(home, avatar);

            player.DisplayData.Name = avatar.Name;
            player.DisplayData.ThumbnailId = home.ThumbnailId;
            player.AccountId = avatar.AccountId;
            player.CharacterId = home.CharacterId;
            player.SkinId = home.SkinId;
            player.PlayerIndex = playerIndex;
            player.TeamIndex = teamIndex;
            player.HeroData = true;
            player.HeroDataCount = 2;
            // player.HeroDataAccessory = 

            Hero hero = avatar.GetHero(home.CharacterId);
            player.Trophies = hero.Trophies;
            player.HighestTrophies = hero.HighestTrophies;
            player.HeroPowerLevel = hero.PowerLevel;

            return player;
        }

        public static BattlePlayer CreateBotInfo(string name, int playerIndex, int teamIndex, int character = 16000000)
        {
            BattlePlayer player = new BattlePlayer();
            player.DisplayData.Name = "жирная шалава";
            player.DisplayData.ThumbnailId = GlobalId.CreateGlobalId(28, 0);
            player.AccountId = 100000 + playerIndex;
            player.CharacterId = character;
            player.PlayerIndex = playerIndex;
            player.TeamIndex = teamIndex;
            player.SessionId = -1;
            player.Bot = true;
            player.HeroData = true;

            return player;
        }

        public int ReturnTe()
        {
            return TeamIndex;
        }
        public int GetOnlySkinID(int? skinid)
        {
            if (skinid == null) return -1;

            string numrstr = skinid.Value.ToString();
            int zero = numrstr.LastIndexOf('0') + 1;
            if (zero >= numrstr.Length)
            {
                return skinid.Value;
            }

            string str = numrstr.Substring(zero);
            return int.TryParse(str, out int result) ? result : skinid.Value;
        }


    }
}
