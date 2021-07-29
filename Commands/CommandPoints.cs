using LandSharks.Database;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;

namespace LandSharks.Commands
{
    public class CommandPoints : IRocketCommand
    {
        public string Name => "points";

        public string Help => "Reset, set, add or remove points";

        public string Syntax => "[reset/set/add/remove] [<player>] [<points>]";

        public List<string> Aliases => new List<string>();

        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public List<string> Permissions =>
            new List<string>
            {
                "points",
                "points.reset",
                "points.set",
                "points.add",
                "points.remove"
            };

        public void Execute(IRocketPlayer caller, params string[] command)
        {
            if (command.Length == 2 && caller.HasPermission("points.reset") && command[0] == "reset")
            {
                var otherPlayer = UnturnedPlayer.FromName(command[1]);
                if (otherPlayer == null)
                {
                    UnturnedChat.Say(caller, SharkTank.Instance.Translate("general_not_found"),
                        SharkTank.Instance.configNotificationColor);
                }
                else
                {
                    SharkTank.Instance.RankDatabase.SetPoints(otherPlayer.CSteamID.ToString(), 0);
                    SharkTank.DicPoints[otherPlayer.CSteamID] = 0;
                    UnturnedChat.Say(otherPlayer, SharkTank.Instance.Translate("points_reset_player"),
                        SharkTank.Instance.configNotificationColor);
                    UnturnedChat.Say(caller,
                        SharkTank.Instance.Translate("points_reset_caller", otherPlayer.DisplayName),
                        SharkTank.Instance.configNotificationColor);
                }
            }
            else if (command.Length == 3 && caller.HasPermission("points.set") && command[0] == "set")
            {
                var otherPlayer = UnturnedPlayer.FromName(command[1]);
                if (otherPlayer == null)
                {
                    UnturnedChat.Say(caller, SharkTank.Instance.Translate("general_not_found"),
                        SharkTank.Instance.configNotificationColor);
                }
                else
                {
                    var isNumeric = int.TryParse(command[2], out var points);
                    if (!isNumeric)
                    {
                        UnturnedChat.Say(caller, SharkTank.Instance.Translate("general_invalid_parameter"),
                            SharkTank.Instance.configNotificationColor);
                        return;
                    }

                    SharkTank.Instance.RankDatabase.SetPoints(otherPlayer.CSteamID.ToString(), points);
                    SharkTank.DicPoints[otherPlayer.CSteamID] = points;
                    UnturnedChat.Say(otherPlayer,
                        SharkTank.Instance.Translate("points_set_player", points.ToString()),
                        SharkTank.Instance.configNotificationColor);
                    UnturnedChat.Say(caller,
                        SharkTank.Instance.Translate("points_set_caller", points.ToString(), otherPlayer.DisplayName),
                        SharkTank.Instance.configNotificationColor);
                }
            }
            else if (command.Length == 3 && caller.HasPermission("points.add") && command[0] == "add")
            {
                var otherPlayer = UnturnedPlayer.FromName(command[1]);
                if (otherPlayer == null)
                {
                    UnturnedChat.Say(caller, SharkTank.Instance.Translate("general_not_found"),
                        SharkTank.Instance.configNotificationColor);
                }
                else
                {
                    var isNumeric = int.TryParse(command[2], out var points);
                    if (!isNumeric)
                    {
                        UnturnedChat.Say(caller, SharkTank.Instance.Translate("general_invalid_parameter"),
                            SharkTank.Instance.configNotificationColor);
                        return;
                    }

                    SharkTank.Instance.UpdatePoints(otherPlayer, points);
                    UnturnedChat.Say(otherPlayer,
                        SharkTank.Instance.Translate("points_add_player", points.ToString()),
                        SharkTank.Instance.configNotificationColor);
                    UnturnedChat.Say(caller,
                        SharkTank.Instance.Translate("points_add_caller", points.ToString(), otherPlayer.DisplayName),
                        SharkTank.Instance.configNotificationColor);
                }
            }
            else if (command.Length == 3 && caller.HasPermission("points.remove") && command[0] == "remove")
            {
                var otherPlayer = UnturnedPlayer.FromName(command[1]);
                if (otherPlayer == null)
                {
                    UnturnedChat.Say(caller, SharkTank.Instance.Translate("general_not_found"),
                        SharkTank.Instance.configNotificationColor);
                }
                else
                {
                    var isNumeric = int.TryParse(command[2], out var points);
                    if (!isNumeric)
                    {
                        UnturnedChat.Say(caller, SharkTank.Instance.Translate("general_invalid_parameter"),
                            SharkTank.Instance.configNotificationColor);
                        return;
                    }

                    var playerExists = SharkTank.DicPoints.TryGetValue(otherPlayer.CSteamID, out _);
                    if (playerExists)
                    {
                        if (SharkTank.DicPoints[otherPlayer.CSteamID] - points >= 0)
                        {
                            SharkTank.Instance.RankDatabase.AddPoints(otherPlayer.CSteamID.ToString(), -points);
                            SharkTank.DicPoints[otherPlayer.CSteamID] -= points;
                        }
                        else
                        {
                            SharkTank.Instance.RankDatabase.SetPoints(otherPlayer.CSteamID.ToString(), 0);
                            points = SharkTank.DicPoints[otherPlayer.CSteamID];
                            SharkTank.DicPoints[otherPlayer.CSteamID] = 0;
                        }
                    }

                    UnturnedChat.Say(otherPlayer,
                        SharkTank.Instance.Translate("points_remove_player", points.ToString()),
                        SharkTank.Instance.configNotificationColor);
                    UnturnedChat.Say(caller,
                        SharkTank.Instance.Translate("points_remove_caller", points.ToString(),
                            otherPlayer.DisplayName), SharkTank.Instance.configNotificationColor);
                }
            }
            else
            {
                UnturnedChat.Say(caller, SharkTank.Instance.Translate("general_invalid_parameter"),
                    SharkTank.Instance.configNotificationColor);
            }
        }
    }
}