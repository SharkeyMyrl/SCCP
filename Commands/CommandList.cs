using LandSharks.Database;
using Rocket.API;
using Rocket.Core;
using Rocket.Unturned.Chat;
using System.Collections.Generic;
using System.Linq;

namespace LandSharks.Commands
{
    public class CommandList : IRocketCommand
    {
        public string Name => "list";

        public string Help => "Display top players or get user by rank";

        public string Syntax => "[<rank>]";

        public List<string> Aliases => new List<string>();

        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public List<string> Permissions =>
            new List<string>
            {
                "list",
                "list.other"
            };

        public void Execute(IRocketPlayer caller, params string[] command)
        {
            switch (command.Length)
            {
                case 0:
                    {
                        var topRanks = SharkTank.Instance.RankDatabase.GetTopRanks(3).ToList();
                        UnturnedChat.Say(caller,
                            SharkTank.Instance.Translate("list_1", SharkTank.Instance.configNotificationColor));

                        if (topRanks.Count == 0)
                        {
                            UnturnedChat.Say(caller, SharkTank.Instance.Translate("ranking_empty"),
                                SharkTank.Instance.configNotificationColor);
                            return;
                        }

                        for (var i = 0; i < topRanks.Count; i++)
                        {
                            var ranking = topRanks[i];
                            UnturnedChat.Say(caller,
                                SharkTank.Instance.Translate($"list_{i + 2}", ranking.Points, ranking.CurrentRank,
                                    SharkTank.Instance.GetLevel(int.Parse(ranking.Points)).Name, ranking.LastDisplayName),
                                SharkTank.Instance.configNotificationColor);
                        }

                        break;
                    }

                case 1:
                    {
                        if (!caller.HasPermission("list.other"))
                        {
                            UnturnedChat.Say(caller, R.Translate("command_no_permission"),
                                SharkTank.Instance.configNotificationColor);
                            return;
                        }

                        if (!int.TryParse(command[0], out var rank))
                        {
                            UnturnedChat.Say(caller, SharkTank.Instance.Translate("nan", command[0]),
                                SharkTank.Instance.configNotificationColor);
                            return;
                        }

                        var ranking = SharkTank.Instance.RankDatabase.GetAccountByRank(rank);

                        if (ranking == null)
                        {
                            UnturnedChat.Say(caller,
                                SharkTank.Instance.Translate("list_search_not_found"),
                                SharkTank.Instance.configNotificationColor);
                            return;
                        }

                        UnturnedChat.Say(caller,
                            SharkTank.Instance.Translate("list_search", ranking.Points,
                                ranking.CurrentRank, SharkTank.Instance.GetLevel(int.Parse(ranking.Points)).Name,
                                ranking.LastDisplayName), SharkTank.Instance.configNotificationColor);
                        break;
                    }

                default:
                    UnturnedChat.Say(caller,
                        SharkTank.Instance.Translate("general_invalid_parameter"),
                        SharkTank.Instance.configNotificationColor);
                    break;
            }
        }
    }
}