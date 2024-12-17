namespace Supercell.Laser.Server.Handler
{
    using Supercell.Laser.Logic.Avatar;
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Logic.Home;
    using Supercell.Laser.Logic.Message.Account.Auth;
    using Supercell.Laser.Logic.Message.Home;
    using Supercell.Laser.Logic.Util;
    using Supercell.Laser.Server.Database;
    using Supercell.Laser.Server.Database.Cache;
    using Supercell.Laser.Server.Database.Models;
    using Supercell.Laser.Server.Networking.Session;
    using System.Reflection;

    public static class CmdHandler
    {

        public static void Start()
        {
            while (true)
            {
                try
                {
                    string cmd = Console.ReadLine();
                    if (cmd == null) continue;
                    if (!cmd.StartsWith("/")) continue;
                    cmd = cmd.Substring(1);
                    string[] args = cmd.Split(" ");
                    if (args.Length < 1) continue;
                    switch (args[0])
                    {
                        case "premium":
                            ExecuteGivePremiumToAccount(args);
                            break;
                        case "ban":
                            ExecuteBanAccount(args);
                            break;
                        case "unban":
                            ExecuteUnbanAccount(args);
                            break;
                        case "dev":
                            DevColor(args);
                            break;
                        case "changename":
                            ExecuteChangeNameForAccount(args);
                            break;
                        case "gems":
                            AddGemsAccount(args);
                            break;
                        case "getvalue":
                            ExecuteGetFieldValue(args);
                            break;
                        case "changevalue":
                            ExecuteChangeValueForAccount(args);
                            break;
                        case "unlockall":
                            ExecuteUnlockAllForAccount(args);
                            break;
                        case "kick":
                            ExecuteKickPlayer(args);
                            break;
                        case "maintenance":
                            Console.WriteLine("Starting maintenance...");
                            ExecuteShutdown();
                            Console.WriteLine("Maintenance started!");
                            break;
                    }
                }
                catch (Exception) { }
            }
        }

        private static void ExecuteUnlockAllForAccount(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /unlockall [TAG]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            for (int i = 0; i < HomeMode.UNLOCKABLE_HEROES_COUNT; i++)
            {
                if (!account.Avatar.HasHero(16000000 + i))
                {
                    CharacterData character = DataTables.Get(16).GetDataWithId<CharacterData>(i);
                    CardData card = DataTables.Get(23).GetData<CardData>(character.Name + "_unlock");

                    account.Avatar.UnlockHero(character.GetGlobalId(), card.GetGlobalId());
                }
            }

            Logger.Print($"Successfully unlocked all brawlers for account {account.AccountId.GetHigherInt()}-{account.AccountId.GetLowerInt()} ({args[1]})");

            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }

        private static void ExecuteGivePremiumToAccount(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /premium [TAG]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            account.Avatar.IsPremium = true;
            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }

        private static void ExecuteUnbanAccount(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /unban [TAG]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            account.Avatar.Banned = false;
            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }

        private static void ExecuteBanAccount(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /ban [TAG]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            account.Avatar.Banned = true;
            account.Avatar.ResetTrophies();
            account.Avatar.Name = "Brawler";
            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }
        private static void ExecuteKickPlayer(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /kick [TAG]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            account.Avatar.Banned = true;
            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new OutOfSyncMessage());
            }
        }

        private static void AddGemsAccount(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: /gems [TAG] [Count]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            account.Avatar.AddDiamondsCmd(args[2]);
            if (Sessions.IsSessionActive(id))
            {
                //if (args[2] == "30" && args[2] == "80" && args[2] == "170" && args[2] == "360" && args[2] == "950" && args[2] == "2000" {
                //    var session = Sessions.GetSession(id);
                //    session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                //    {
                //        Message = "Your account updated!"
                //    });
                //    Sessions.Remove(id);
                //}
                //else
                //{
                //    Console.WriteLine("You can add only 30, 80, 170, 360, 950 or 2000 gems!");
                //    return;
                //}
            }
        }

        private static void DevColor(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: /dev [TAG] [Color]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            account.Home.DevColorCmd(args[2]);
            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }

        private static void ExecuteChangeNameForAccount(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: /changevalue [TAG] [NewName]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            account.Avatar.Name = args[2];
            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }

        private static void ExecuteGetFieldValue(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: /changevalue [TAG] [FieldName]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            Type type = typeof(ClientAvatar);
            FieldInfo field = type.GetField(args[2]);
            if (field == null)
            {
                Console.WriteLine($"Fail: LogicClientAvatar::{args[2]} not found!");
                return;
            }

            int value = (int)field.GetValue(account.Avatar);
            Console.WriteLine($"LogicClientAvatar::{args[2]} = {value}");
        }

        private static void ExecuteChangeValueForAccount(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Usage: /changevalue [TAG] [FieldName] [Value]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            Type type = typeof(ClientAvatar);
            FieldInfo field = type.GetField(args[2]);
            if (field == null)
            {
                Console.WriteLine($"Fail: LogicClientAvatar::{args[2]} not found!");
                return;
            }

            field.SetValue(account.Avatar, int.Parse(args[3]));
            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }

        private static void ExecuteShutdown()
        {
            Sessions.StartShutdown();
            AccountCache.SaveAll();
            AllianceCache.SaveAll();

            AccountCache.Started = false;
            AllianceCache.Started = false;
        }
    }
}
