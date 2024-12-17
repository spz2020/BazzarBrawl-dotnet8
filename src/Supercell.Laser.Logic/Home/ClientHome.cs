namespace Supercell.Laser.Logic.Home
{
    using Newtonsoft.Json;
    using Supercell.Laser.Logic.Command.Home;
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Logic.Data.Helper;
    using Supercell.Laser.Logic.Helper;
    using Supercell.Laser.Logic.Home.Gatcha;
    using Supercell.Laser.Logic.Home.Items;
    using Supercell.Laser.Logic.Home.Quest;
    using Supercell.Laser.Logic.Home.Structures;
    using Supercell.Laser.Logic.Message.Home;
    using Supercell.Laser.Titan.DataStream;
    using System;


    [JsonObject(MemberSerialization.OptIn)]
    public class ClientHome
    {
        public const int DAILYOFFERS_COUNT = 6;

        public static readonly int[] GoldPacksPrice = new int[]
        {
            20, 50, 140, 280
        };

        public static readonly int[] GoldPacksAmount = new int[]
        {
            150, 400, 1200, 2600
        };

        [JsonProperty] public long HomeId;
        [JsonProperty] public int ThumbnailId;
        [JsonProperty] public int CharacterId;
        [JsonProperty] public int SkinId = 52;

        [JsonProperty] public List<OfferBundle> OfferBundles;

        [JsonProperty] public int TrophiesReward;
        [JsonProperty] public int TokenReward;
        [JsonProperty] public int StarTokenReward;

        [JsonProperty] public int BrawlPassProgress;
        [JsonProperty] public int PremiumPassProgress;
        [JsonProperty] public int BrawlPassTokens;
        [JsonProperty] public bool HasPremiumPass;
        [JsonProperty] public List<int> UnlockedEmotes;
        [JsonProperty] public List<int> UnlockedSkins;

        [JsonProperty] public int TrophyRoadProgress;
        [JsonProperty] public int Namecolor;
        [JsonProperty] public int Exp;
        [JsonProperty] public Quests Quests;
        [JsonProperty] public List<string> OffersClaimed;
        [JsonProperty] public string Day;
        [JsonProperty] public List<long> ReportsIds;

        [JsonIgnore] public EventData[] Events;

        public PlayerThumbnailData Thumbnail => DataTables.Get(DataType.PlayerThumbnail).GetDataByGlobalId<PlayerThumbnailData>(ThumbnailId);
        public CharacterData Character => DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(CharacterId);

        public HomeMode HomeMode;

        [JsonProperty] public DateTime LastVisitHomeTime;

        public ClientHome()
        {
            ThumbnailId = GlobalId.CreateGlobalId(28, 0);
            CharacterId = GlobalId.CreateGlobalId(16, 0);

            OfferBundles = new List<OfferBundle>();
            LastVisitHomeTime = DateTime.UnixEpoch;
            ReportsIds = new List<long>();
            OffersClaimed = new List<string>();

            TrophyRoadProgress = 1;

            BrawlPassProgress = 1;
            PremiumPassProgress = 1;

            UnlockedEmotes = new List<int>();
            UnlockedSkins = new List<int>();
        }

        public int TimerMath(DateTime timer_start, DateTime timer_end)
        {
            {
                DateTime timer_now = DateTime.Now;
                if (timer_now > timer_start)
                {
                    if (timer_now < timer_end)
                    {
                        int time_sec = (int)(timer_end - timer_now).TotalSeconds;
                        return time_sec;
                    }
                    else
                    {
                        return -1;
                    }
                }
                else
                {
                    return -1;
                }
            }
        }

        public void HomeVisited()
        {
            RotateShopContent(DateTime.UtcNow, OfferBundles.Count == 0);
            LastVisitHomeTime = DateTime.UtcNow;
            UpdateOfferBundles();

            if (Quests == null && TrophyRoadProgress >= 11)
            {
                Quests = new Quests();
                Quests.AddRandomQuests(HomeMode.Avatar.Heroes, 8);
            }
            else if (Quests != null)
            {
                if (Quests.QuestList.Count < 8) // New quests adds at 07:00 AM UTC
                {
                    Quests.AddRandomQuests(HomeMode.Avatar.Heroes, 8 - Quests.QuestList.Count);
                }
            }
        }

        public void Tick()
        {
            LastVisitHomeTime = DateTime.UtcNow;
            TokenReward = 0;
            TrophiesReward = 0;
            StarTokenReward = 0;
        }

        public void PurchaseOffer(int index)
        {
            if (index < 0 || index >= OfferBundles.Count) return;

            OfferBundle bundle = OfferBundles[index];
            if (bundle.Purchased) return;

            if (bundle.Currency == 0)
            {
                if (!HomeMode.Avatar.UseDiamonds(bundle.Cost)) return;
            }
            else if (bundle.Currency == 1)
            {
                if (!HomeMode.Avatar.UseGold(bundle.Cost)) return;
            }

            if (bundle.Claim == "debug")
            {
                ;
            }
            else
            {
                OffersClaimed.Add(bundle.Claim);
            }

            bundle.Purchased = true;

            LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
            Random rand = new Random();

            foreach (Offer offer in bundle.Items)
            {
                if (offer.Type == ShopItem.BrawlBox || offer.Type == ShopItem.FreeBox)
                {
                    DeliveryUnit unit = new DeliveryUnit(10);
                    HomeMode.SimulateGatcha(unit);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.HeroPower)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(6);
                    reward.DataGlobalId = offer.ItemDataId;
                    reward.Count = offer.Count;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.Gems)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(8);
                    reward.Count = offer.Count;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.Coin)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(7);
                    reward.Count = offer.Count;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.Skin)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(9);
                    reward.SkinGlobalId = GlobalId.CreateGlobalId(29, offer.SkinDataId);
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.BigBox)
                {
                    DeliveryUnit unit = new DeliveryUnit(12);
                    HomeMode.SimulateGatcha(unit);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.MegaBox)
                {
                    DeliveryUnit unit = new DeliveryUnit(11);
                    HomeMode.SimulateGatcha(unit);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.GuaranteedHero)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(1);
                    reward.DataGlobalId = offer.ItemDataId;
                    reward.Count = 1;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.EmoteBundle)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    List<int> Emotes_All = new List<int> { 0, 1, 2, 3, 4, 182, 232, 7, 8, 9, 10, 11, 183, 233, 12, 13, 14, 15, 16, 184, 234, 17, 18, 19, 20, 21, 185, 235, 23, 24, 25, 26, 27, 186, 236, 29, 30, 31, 32, 33, 187, 237, 34, 35, 36, 37, 38, 188, 238, 39, 40, 41, 42, 43, 189, 239, 270, 44, 45, 46, 47, 48, 190, 240, 271, 49, 50, 51, 52, 53, 191, 241, 54, 55, 56, 57, 58, 192, 242, 59, 60, 61, 62, 63, 193, 243, 65, 66, 67, 68, 69, 194, 244, 272, 70, 71, 72, 73, 74, 195, 245, 76, 77, 78, 79, 80, 196, 248, 83, 84, 85, 86, 87, 197, 249, 88, 89, 90, 91, 92, 198, 250, 94, 95, 96, 97, 98, 199, 251, 99, 100, 101, 102, 103, 200, 253, 104, 105, 106, 107, 108, 201, 256, 109, 110, 111, 112, 113, 202, 257, 114, 115, 116, 117, 118, 203, 258, 119, 120, 121, 122, 123, 203, 259, 124, 125, 126, 127, 128, 204, 260, 273, 129, 130, 131, 132, 133, 205, 261, 135, 136, 137, 138, 139, 206, 262, 140, 141, 142, 143, 144, 207, 263, 146, 147, 148, 149, 150, 208, 264, 274, 151, 152, 153, 154, 155, 209, 265, 159, 160, 161, 162, 163, 210, 266, 164, 165, 166, 167, 168, 211, 267, 170, 171, 172, 173, 174, 212, 268, 275, 177, 178, 179, 180, 181, 213, 269, 226, 227, 228, 229, 230, 231, 255, 357, 252, 276, 277, 278, 279, 280, 281 };
                    List<int> Emotes_Locked = Emotes_All.Except(UnlockedEmotes).OrderBy(x => Guid.NewGuid()).Take(3).ToList(); ;

                    foreach (int x in Emotes_Locked)
                    {
                        GatchaDrop reward = new GatchaDrop(11);
                        reward.Count = 1;
                        reward.PinGlobalId = 52000000 + x;
                        unit.AddDrop(reward);
                        UnlockedEmotes.Add(x);
                    }
                    command.DeliveryUnits.Add(unit);
                }

                else if (offer.Type == ShopItem.Emote)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(11);
                    reward.Count = 1;
                    reward.PinGlobalId = 52000221;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }

                else
                {
                    // todo...
                }

                UpdateOfferBundles();

                command.Execute(HomeMode);

                AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                message.Command = command;
                HomeMode.GameListener.SendMessage(message);
            }
        }

        private void RotateShopContent(DateTime time, bool isNewAcc)
        {
            if (OfferBundles.Select(bundle => bundle.IsDailyDeals).ToArray().Length > 6)
            {
                OfferBundles.RemoveAll(bundle => bundle.IsDailyDeals);
            }
            OfferBundles.RemoveAll(offer => offer.EndTime <= time);

            if (isNewAcc || DateTime.UtcNow >= DateTime.UtcNow.Date.AddHours(8)) // Daily deals refresh at 08:00 AM UTC
            {
                if (LastVisitHomeTime < DateTime.UtcNow.Date.AddHours(8))
                {
                    UpdateDailyOfferBundles();
                }
            }
        }

        public void DevColorCmd(string color)
        {
            if (color == "12")
            {
                Namecolor = 12;
            }
            if (color == "13")
            {
                Namecolor = 13;
            }
            if (color == "14")
            {
                Namecolor = 14;
            }
        }

        private void UpdateDailyOfferBundles()
        {
            // OfferBundles.Add(GenerateDailyGift());
            // OfferBundles.Add(GenerateGift());
            // OfferBundles.Add(GenerateGift1());
            // OfferBundles.Add(GenerateGift2());

            // bool shouldPowerPoints = false;
            // for (int i = 1; i < DAILYOFFERS_COUNT; i++)
            // {
            //     OfferBundle dailyOffer = GenerateDailyOffer(shouldPowerPoints);
            //     if (dailyOffer != null)
            //     {
            //         if (!shouldPowerPoints) shouldPowerPoints = dailyOffer.Items[0].Type != ShopItem.HeroPower;
            //         OfferBundles.Add(dailyOffer);
            //     }
            // }
            ;
        }

        public void UpdateOfferBundles()
        {
            OfferBundles.RemoveAll(bundle => bundle.IsTrue);

            GenerateOffer(
                new DateTime(2000, 4, 20, 12, 0, 0), new DateTime(2030, 4, 20, 12, 0, 0),
                1, 52, 52, ShopItem.EmoteBundle,
                0, 0, 0,
                "pinpackteuy1st", "PinPack!", "offer_finals"
            );
            GenerateOffer(
                new DateTime(2000, 4, 20, 12, 0, 0), new DateTime(2030, 4, 20, 12, 0, 0),
                1, 52, 52, ShopItem.Emote,
                0, 0, 0,
                "pinpackteuyst", "сру кроу Pin", "offer_finals"
            );
        }

        public void GenerateOffer(
            DateTime OfferStart,
            DateTime OfferEnd,
            int Count,
            int BrawlerID,
            int Extra,
            ShopItem Item,
            int Cost,
            int OldCost,
            int Currency,
            string Claim,
            string Title,
            string BGR
            )
        {

            OfferBundle bundle = new OfferBundle();
            bundle.IsDailyDeals = false;
            bundle.IsTrue = true;
            bundle.EndTime = OfferEnd;
            bundle.Cost = Cost;
            bundle.OldCost = OldCost;
            bundle.Currency = Currency;
            bundle.Claim = Claim;
            bundle.Title = Title;
            bundle.BackgroundExportName = BGR;

            if (OffersClaimed.Contains(bundle.Claim))
            {
                bundle.Purchased = true;
            }
            if (TimerMath(OfferStart, OfferEnd) == -1)
            {
                bundle.Purchased = true;
            }
            if (HomeMode.HasHeroUnlocked(16000000 + BrawlerID))
            {
                bundle.Purchased = true;
            }

            Offer offer = new Offer(Item, Count, (16000000 + BrawlerID), Extra);
            bundle.Items.Add(offer);

            OfferBundles.Add(bundle);
        }

        public void GenerateOffer2(
            DateTime OfferStart,
            DateTime OfferEnd,
            int Count,
            int BrawlerID,
            int Extra,
            ShopItem Item,
            int Count2,
            int BrawlerID2,
            int Extra2,
            ShopItem Item2,
            int Cost,
            int OldCost,
            int Currency,
            string Claim,
            string Title,
            string BGR
            )
        {

            OfferBundle bundle = new OfferBundle();
            bundle.IsDailyDeals = false;
            bundle.IsTrue = true;
            bundle.EndTime = OfferEnd;
            bundle.Cost = Cost;
            bundle.OldCost = OldCost;
            bundle.Currency = Currency;
            bundle.Claim = Claim;
            bundle.Title = Title;
            bundle.BackgroundExportName = BGR;

            if (OffersClaimed.Contains(bundle.Claim))
            {
                bundle.Purchased = true;
            }
            if (TimerMath(OfferStart, OfferEnd) == -1)
            {
                bundle.Purchased = true;
            }
            if (HomeMode.HasHeroUnlocked(16000000 + BrawlerID))
            {
                bundle.Purchased = true;
            }

            Offer offer = new Offer(Item, Count, (16000000 + BrawlerID), Extra);
            bundle.Items.Add(offer);
            Offer offer2 = new Offer(Item2, Count2, (16000000 + BrawlerID2), Extra2);
            bundle.Items.Add(offer2);

            OfferBundles.Add(bundle);
        }

        public void GenerateOffer3(
            DateTime OfferStart,
            DateTime OfferEnd,
            int Count,
            int BrawlerID,
            int Extra,
            ShopItem Item,
            int Count2,
            int BrawlerID2,
            int Extra2,
            ShopItem Item2,
            int Count3,
            int BrawlerID3,
            int Extra3,
            ShopItem Item3,
            int Cost,
            int OldCost,
            int Currency,
            string Claim,
            string Title,
            string BGR
            )
        {

            OfferBundle bundle = new OfferBundle();
            bundle.IsDailyDeals = false;
            bundle.IsTrue = true;
            bundle.EndTime = OfferEnd;
            bundle.Cost = Cost;
            bundle.OldCost = OldCost;
            bundle.Currency = Currency;
            bundle.Claim = Claim;
            bundle.Title = Title;
            bundle.BackgroundExportName = BGR;

            if (OffersClaimed.Contains(bundle.Claim))
            {
                bundle.Purchased = true;
            }
            if (TimerMath(OfferStart, OfferEnd) == -1)
            {
                bundle.Purchased = true;
            }
            if (HomeMode.HasHeroUnlocked(16000000 + BrawlerID))
            {
                bundle.Purchased = true;
            }

            Offer offer = new Offer(Item, Count, (16000000 + BrawlerID), Extra);
            bundle.Items.Add(offer);
            Offer offer2 = new Offer(Item2, Count2, (16000000 + BrawlerID2), Extra2);
            bundle.Items.Add(offer2);
            Offer offer3 = new Offer(Item3, Count3, (16000000 + BrawlerID3), Extra3);
            bundle.Items.Add(offer3);

            OfferBundles.Add(bundle);
        }

        private OfferBundle GenerateDailyGift()
        {
            OfferBundle bundle = new OfferBundle();
            bundle.IsDailyDeals = true;
            bundle.EndTime = DateTime.UtcNow.Date.AddDays(1).AddHours(8); // tomorrow at 8:00 utc (11:00 MSK)
            bundle.Cost = 0;

            Offer offer = new Offer(ShopItem.FreeBox, 1);
            bundle.Items.Add(offer);

            return bundle;
        }

        private OfferBundle GenerateGift()
        {
            OfferBundle bundle = new OfferBundle();
            bundle.IsDailyDeals = false;
            bundle.EndTime = DateTime.UtcNow.Date.AddDays(1).AddHours(8); // tomorrow at 8:00 utc (11:00 MSK)
            bundle.Cost = 0;
            bundle.Title = "Thanks for 600 subscribers";

            Offer offer = new Offer(ShopItem.Gems, 80);
            bundle.Items.Add(offer);

            return bundle;
        }

        private OfferBundle GenerateGift1()
        {
            OfferBundle bundle = new OfferBundle();
            bundle.IsDailyDeals = false;
            bundle.EndTime = DateTime.UtcNow.Date.AddDays(1).AddHours(8); // tomorrow at 8:00 utc (11:00 MSK)
            bundle.Cost = 0;
            bundle.Title = "<cff3200>G<cff6500>i<cff9800>f<cffcb00>t<cffff00> <cccff00>D<c99ff00>a<c66ff00>y<c33ff00> <c01ff00>2</c>";

            Offer offer = new Offer(ShopItem.Skin, 1, 29000000, 52);
            bundle.Items.Add(offer);

            return bundle;
        }

        private OfferBundle GenerateGift2()
        {
            OfferBundle bundle = new OfferBundle();
            bundle.IsDailyDeals = false;
            bundle.EndTime = DateTime.UtcNow.Date.AddDays(1).AddHours(8); // tomorrow at 8:00 utc (11:00 MSK)
            bundle.Cost = 0;
            bundle.Title = "<cff3200>G<cff6500>i<cff9800>f<cffcb00>t<cffff00> <cccff00>D<c99ff00>a<c66ff00>y<c33ff00> <c01ff00>3</c>";

            Offer offer = new Offer(ShopItem.Gems, 30);
            bundle.Items.Add(offer);

            return bundle;
        }

        private OfferBundle GenerateDailyOffer(bool shouldPowerPoints)
        {
            OfferBundle bundle = new OfferBundle();
            bundle.IsDailyDeals = true;
            bundle.EndTime = DateTime.UtcNow.Date.AddDays(1).AddHours(8); // tomorrow at 8:00 utc (11:00 MSK)

            Random random = new Random();
            int type = shouldPowerPoints ? 0 : random.Next(0, 2); // getting a type

            switch (type)
            {
                case 0: // Power points
                    List<Hero> unlockedHeroes = HomeMode.Avatar.Heroes;
                    bool heroValid = false;
                    int generateAttempts = 0;
                    int index = -1;
                    while (!heroValid && generateAttempts < 10)
                    {
                        generateAttempts++;
                        index = random.Next(unlockedHeroes.Count);
                        heroValid = unlockedHeroes[index].PowerPoints < 2300 + 1440;
                        if (heroValid)
                        {
                            foreach (OfferBundle b in OfferBundles)
                            {
                                if (!b.IsDailyDeals) continue;

                                if (b.Items.Count > 0)
                                {
                                    if (b.Items[0].Type == ShopItem.HeroPower)
                                    {
                                        if (b.Items[0].ItemDataId == unlockedHeroes[index].CharacterId)
                                        {
                                            heroValid = false;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (!heroValid) return null;

                    int count = random.Next(15, 100) + 1;
                    Offer offer = new Offer(ShopItem.HeroPower, count, unlockedHeroes[index].CharacterId);

                    bundle.Items.Add(offer);
                    bundle.Cost = count * 2;
                    bundle.Currency = 1;

                    break;
                case 1: // mega box
                    Offer megaBoxOffer = new Offer(ShopItem.MegaBox, 1);
                    bundle.Items.Add(megaBoxOffer);
                    bundle.Cost = 40;
                    bundle.OldCost = 80;
                    bundle.Currency = 0;
                    break;
            }

            return bundle;
        }

        public void Encode(ByteStream encoder)
        {
            DateTime utcNow = DateTime.UtcNow;


            //long maintenceTimerInSeconds = Config.Instance.GetUnixTimestamp(Config.Instance.MaintenceTimer);
            //long trophySeasonTimerInSeconds = Config.Instance.GetUnixTimestamp(Config.Instance.TrophySeasonTimer);
            //long brawlPassSeasonTimerInSeconds = Config.Instance.GetUnixTimestamp(Config.Instance.BrawlPassSeasonTimer);

            encoder.WriteVInt(utcNow.Year * 1000 + utcNow.DayOfYear); // 0x78d4b8
            encoder.WriteVInt(utcNow.Hour * 3600 + utcNow.Minute * 60 + utcNow.Second); // 0x78d4cc

            encoder.WriteVInt(HomeMode.Avatar.Trophies); // 0x78d4e0
            encoder.WriteVInt(HomeMode.Avatar.Trophies); // 0x78d4f4

            encoder.WriteVInt(HomeMode.Avatar.HighestTrophies); // highest trophy again?

            encoder.WriteVInt(200);// TrophyRoadProgress);

            encoder.WriteVInt(14888);// Exp + 100); // experience

            ByteStreamHelper.WriteDataReference(encoder, Thumbnail);

            // Name colors not implemented since I used game patch to allow color codes in names and everywhere.
            encoder.WriteVInt(43);
            encoder.WriteVInt(Namecolor);

            encoder.WriteVInt(18); // Played game modes
            for (int i = 0; i < 18; i++)
            {
                encoder.WriteVInt(i);
            }

            encoder.WriteVInt(1); // Selected Skins Dictionary
            {
                encoder.WriteVInt(29);
                encoder.WriteVInt(SkinId);
            }

            encoder.WriteVInt(UnlockedSkins.Count);
            for (int i = 0; i < UnlockedSkins.Count; i++)
            {
                encoder.WriteVInt(29);
                encoder.WriteVInt(UnlockedSkins[i]);
            }

            encoder.WriteVInt(0);

            encoder.WriteVInt(0);
            encoder.WriteVInt(HomeMode.Avatar.HighestTrophies); // Highest Trophies Reachd Icon
            encoder.WriteVInt(0);
            encoder.WriteVInt(1);

            encoder.WriteBoolean(true);
            encoder.WriteVInt(1);
            encoder.WriteVInt(1488); // trophy league timer
            encoder.WriteVInt(1488); // powerplay timer
            encoder.WriteVInt(1488); // brawl pass season timer

            encoder.WriteVInt(0);
            encoder.WriteVInt(0);
            encoder.WriteVInt(0);

            encoder.WriteBoolean(false);
            encoder.WriteBoolean(false);
            encoder.WriteBoolean(false);
            encoder.WriteVInt(2);
            encoder.WriteVInt(2);
            encoder.WriteVInt(2);

            encoder.WriteVInt(0); // name change cost in gems
            encoder.WriteVInt(0); // timer for next name change

            encoder.WriteVInt(OfferBundles.Count); // Shop offers at 0x78e0c4
            foreach (OfferBundle offerBundle in OfferBundles)
            {
                offerBundle.Encode(encoder);
            }

            encoder.WriteVInt(0);

            encoder.WriteVInt(10); // Battle Tokens
            encoder.WriteVInt(1488); // Timer for battle tokens
            encoder.WriteVInt(0); // 0x78e250
            encoder.WriteVInt(0); // 0x78e3a4
            encoder.WriteVInt(0); // 0x78e3a4

            ByteStreamHelper.WriteDataReference(encoder, Character);

            encoder.WriteString("RU"); // Z
            encoder.WriteString("meow :3"); // V

            encoder.WriteVInt(3);
            {
                encoder.WriteInt(3);
                encoder.WriteInt(TokenReward); // tokens

                encoder.WriteInt(4);
                encoder.WriteInt(TrophiesReward); // trophies

                encoder.WriteInt(10);
                encoder.WriteInt(1337); // power play trophies


            }
            TokenReward = 0;
            TrophiesReward = 0; // ну а где ещё. хотя....
            StarTokenReward = 0;

            encoder.WriteVInt(0); // array

            encoder.WriteVInt(1); // BrawlPassSeasonData
            {
                encoder.WriteVInt(0);
                encoder.WriteVInt(BrawlPassTokens);
                encoder.WriteVInt(PremiumPassProgress);
                encoder.WriteVInt(BrawlPassProgress);
                encoder.WriteBoolean(HasPremiumPass);
                encoder.WriteVInt(0);
            }

            encoder.WriteBoolean(true); // pro league season data
            {
                encoder.WriteVInt(0); // idk
                encoder.WriteVInt(1337); // trophies count?
            }

            if (Quests != null)
            {
                encoder.WriteBoolean(true);
                Quests.Encode(encoder);
            }
            else
            {
                encoder.WriteBoolean(true);
                encoder.WriteVInt(0);
            }

            encoder.WriteBoolean(true);
            encoder.WriteVInt(UnlockedEmotes.Count);
            foreach (int i in UnlockedEmotes)
            {
                encoder.WriteVInt(52);
                encoder.WriteVInt(i);
                encoder.WriteVInt(1);
                encoder.WriteVInt(1);
                encoder.WriteVInt(1);
            }

            encoder.WriteVInt(utcNow.Year * 1000 + utcNow.DayOfYear);
            encoder.WriteVInt(100);
            encoder.WriteVInt(10);
            encoder.WriteVInt(30);
            encoder.WriteVInt(3);
            encoder.WriteVInt(80);
            encoder.WriteVInt(10);
            encoder.WriteVInt(40);
            encoder.WriteVInt(1000);
            encoder.WriteVInt(550);
            encoder.WriteVInt(0);
            encoder.WriteVInt(999900);

            encoder.WriteVInt(0); // Array

            encoder.WriteVInt(11);
            for (int i = 1; i <= 11; i++)
                encoder.WriteVInt(i);

            encoder.WriteVInt(Events.Length + 1);
            foreach (EventData data in Events)
            {
                data.Encode(encoder);
            }


            encoder.WriteVInt(0);
            encoder.WriteVInt(9); // slot
            encoder.WriteVInt(0);
            encoder.WriteVInt(1588);
            encoder.WriteVInt(10);

            ByteStreamHelper.WriteDataReference(encoder, 15000000);

            encoder.WriteVInt(2); // 0xacec7c
            encoder.WriteString(null); // 0xacecac
            encoder.WriteVInt(0); // 0xacecc0
            encoder.WriteVInt(0); // - пытки
            encoder.WriteVInt(3); // всего пыток 

            encoder.WriteVInt(0); // 0xacecfc

            encoder.WriteVInt(1); // 0xacee58
            encoder.WriteVInt(1); // 0xacee6c




            encoder.WriteVInt(0);

            encoder.WriteVInt(8);
            {
                encoder.WriteVInt(20);
                encoder.WriteVInt(35);
                encoder.WriteVInt(75);
                encoder.WriteVInt(140);
                encoder.WriteVInt(290);
                encoder.WriteVInt(480);
                encoder.WriteVInt(800);
                encoder.WriteVInt(1250);
            }

            encoder.WriteVInt(8);
            {
                encoder.WriteVInt(1);
                encoder.WriteVInt(2);
                encoder.WriteVInt(3);
                encoder.WriteVInt(4);
                encoder.WriteVInt(5);
                encoder.WriteVInt(10);
                encoder.WriteVInt(15);
                encoder.WriteVInt(20);
            }

            encoder.WriteVInt(3);
            {
                encoder.WriteVInt(10);
                encoder.WriteVInt(30);
                encoder.WriteVInt(80);
            }

            encoder.WriteVInt(3);
            {
                encoder.WriteVInt(6);
                encoder.WriteVInt(20);
                encoder.WriteVInt(60);
            }

            ByteStreamHelper.WriteIntList(encoder, GoldPacksPrice);
            ByteStreamHelper.WriteIntList(encoder, GoldPacksAmount);

            encoder.WriteVInt(2);
            encoder.WriteVInt(200); // tokens
            encoder.WriteVInt(20); // tokens been added

            encoder.WriteVInt(8640);
            encoder.WriteVInt(10);
            encoder.WriteVInt(5);

            encoder.WriteBoolean(false);
            encoder.WriteBoolean(false);
            encoder.WriteBoolean(false);

            encoder.WriteVInt(50);
            encoder.WriteVInt(604800);

            encoder.WriteBoolean(true);

            encoder.WriteVInt(1); // Array
            {
                encoder.WriteVInt(16);
                encoder.WriteVInt(36);

                encoder.WriteInt(10000);
                encoder.WriteInt(10000);
            }

            encoder.WriteVInt(2); // IntValueEntries
            {
                encoder.WriteInt(1);
                encoder.WriteInt(41000000); // theme 41000013

                encoder.WriteInt(46);
                encoder.WriteInt(1);
            }

            encoder.WriteVInt(0);

            encoder.WriteLong(HomeId);
            encoder.WriteVInt(1); // notif massive
            {
                encoder.WriteVInt(73);
                encoder.WriteInt(1);
                encoder.WriteBoolean(false);
                encoder.WriteInt(1337); // timer
                encoder.WriteString("");
                encoder.WriteVInt(0);
            }
            encoder.WriteVInt(0);
            encoder.WriteBoolean(false);
            encoder.WriteVInt(0);
            encoder.WriteVInt(0);
        }
    }
}
