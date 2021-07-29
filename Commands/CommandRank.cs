using LandSharks.Database;
using Rocket.API;
using Rocket.Core;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using Steamworks;
using System.Collections.Generic;

namespace LandSharks.Commands
{
    public class CommandRank : IRocketCommand
    {
        public string Name => "rank";

        public string Help => "Display current rank or get user by name";

        public string Syntax => "[<player>]";

        public List<string> Aliases => new List<string>();

        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public List<string> Permissions =>
            new List<string>
            {
                "rank",
                "rank.other"
            };

        public void Execute(IRocketPlayer caller, params string[] command)
        {
            switch (command.Length)
            {
                case 0:
                    {
                        if (SharkTank.DicPoints.TryGetValue(new CSteamID(ulong.Parse(caller.Id)), out var playerPoints))
                            UnturnedChat.Say(caller,
                                SharkTank.Instance.Translations.Instance.Translate("rank_self", playerPoints,
                                    SharkTank.Instance.RankDatabase.GetRankBySteamId(caller.Id),
                                    SharkTank.Instance.GetLevel(playerPoints).Name),
                                SharkTank.Instance.configNotificationColor);
                        break;
                    }

                case 1:
                    {
                        if (!caller.HasPermission("rank.other"))
                        {
                            UnturnedChat.Say(caller, R.Translate("command_no_permission"),
                                SharkTank.Instance.configNotificationColor);
                            return;
                        }

                        var otherPlayer = UnturnedPlayer.FromName(command[0]);
                        if (otherPlayer == null)
                        {
                            UnturnedChat.Say(caller, SharkTank.Instance.Translate("general_not_found"),
                                SharkTank.Instance.configNotificationColor);
                        }
                        else
                        {
                            if (SharkTank.DicPoints.TryGetValue(otherPlayer.CSteamID, out var playerPoints))
                                UnturnedChat.Say(caller,
                                    SharkTank.Instance.Translate("rank_other", playerPoints,
                                        SharkTank.Instance.RankDatabase.GetRankBySteamId(
                                            otherPlayer.CSteamID.ToString()),
                                        SharkTank.Instance.GetLevel(playerPoints).Name, otherPlayer.DisplayName),
                                    SharkTank.Instance.configNotificationColor);
                        }

                        break;
                    }

                default:
                    {
                        UnturnedChat.Say(caller, SharkTank.Instance.Translate("general_invalid_parameter"),
                            SharkTank.Instance.configNotificationColor);
                        break;
                    }
            }
        }
    }
}