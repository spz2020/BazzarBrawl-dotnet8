namespace Supercell.Laser.Server.Message
{
    using Supercell.Laser.Logic.Command;
    using Supercell.Laser.Logic.Command.Home;
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Logic.Data.Helper;
    using Supercell.Laser.Logic.Home;
    using Supercell.Laser.Logic.Team;
    using Supercell.Laser.Server.Logic.Game;
    using Supercell.Laser.Server.Networking;
    using System;

    public class CommandManager
    {
        public Connection Connection { get; }

        public HomeMode HomeMode;


        public CommandManager(HomeMode homeMode, Connection connection)
        {
            HomeMode = homeMode;
            Connection = connection;
        }


        public bool ReceiveCommand(Command command)
        {
            try
            {
                switch (command.GetCommandType())
                {
                    case 506:
                        return LogicSelectSkinReceived((LogicSelectSkinCommand)command);
                    default:
                        Logger.Print($"CommandManager::ReceiveCommand - no case for {command.GetType().Name} ({command.GetCommandType()})");
                        return false;
                }
            }
            catch (Exception e)
            {
                return false;
            }
        }


        private bool LogicSelectSkinReceived(LogicSelectSkinCommand command)
        {
            int skin = GlobalId.CreateGlobalId(29, command.SkinId);
            int skind = GlobalId.CreateGlobalId(44, command.SkinId);
            SkinData skinData = DataTables.Get(DataType.Skin).GetDataByGlobalId<SkinData>(skin);
            if (command.SkinId != 63)
                if (!skinData.Name.EndsWith("Default") && !HomeMode.Home.UnlockedSkins.Contains(skin)) return false;
            Console.WriteLine(skinData.Conf);
            SkinConfData sk = DataTables.Get(DataType.SkinConf).GetData<SkinConfData>(skinData.Conf);
            CharacterData c = DataTables.Get(DataType.Character).GetData<CharacterData>(sk.Character);
            Console.WriteLine(c.GetGlobalId());
            //HomeMode.Home.SelectedSkins[c.GetInstanceId()] = command.SkinId;
            if (HomeMode.Avatar.TeamId > 0)
            {
                TeamEntry team = Teams.Get(HomeMode.Avatar.TeamId);
                if (team == null)
                {
                    return true;
                }
                TeamMember m = team.GetMember(HomeMode.Avatar.AccountId);
                if (m == null)
                {
                    return true;
                }
                m.SkinId = skin;
                team.TeamUpdated();
            }
            return true;
        }

    }
}
