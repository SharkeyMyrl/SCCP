using fr34kyn01535.Kits;
using fr34kyn01535.Uconomy;
using LandSharks.Config;
using LandSharks.Database;
using Rocket.API;
using Rocket.API.Collections;
using Rocket.API.Serialisation;
using Rocket.Core;
using Rocket.Core.Permissions;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Level = LandSharks.Config.Objects.Level;
using Logger = Rocket.Core.Logging.Logger;

namespace LandSharks
{
    public class SharkTank : RocketPlugin<Configuration>
    {

        public static SharkTank Instance;
        public static Configuration Config;
        public const string Version = "1.0.0";

        public RankDatabase RankDatabase;

        public static Dictionary<CSteamID, int> DicPoints = new Dictionary<CSteamID, int>();
        public Color configNotificationColor;
        public Color configNotificationColorGlobal;
        public Color configNotificationColorJoinLeaveGlobal;

        protected override void Load()
        {
            SharkTank.Instance = this;

            RankDatabase = new RankDatabase();

            Config = Configuration.Instance;

            Logger.Log($"SCCP vault database Initializing...");
            VaultDatabase.CheckSchema();

            Logger.Log($"SCCP rank database Initializing...");
            RankDatabase.CheckSchema();

            Provider.onServerConnected += OnServerConnected;

            Provider.onServerDisconnected += OnServerDisconnected;

            Instance.Configuration.Instance.Level = Instance.Configuration.Instance.Level.OrderByDescending(x => x.Points).ToList();
            configNotificationColor = UnturnedChat.GetColorFromName(Instance.Configuration.Instance.NotificationColor, Color.green);
            configNotificationColorGlobal = UnturnedChat.GetColorFromName(Instance.Configuration.Instance.NotificationColorGlobal, Color.green);
            configNotificationColorJoinLeaveGlobal = UnturnedChat.GetColorFromName(Instance.Configuration.Instance.NotificationColorJoinLeaveGlobal, Color.green);

            U.Events.OnPlayerConnected += Events_OnPlayerConnected;
            U.Events.OnPlayerDisconnected += Events_OnPlayerDisconnected;
            UnturnedPlayerEvents.OnPlayerUpdateStat += UnturnedPlayerEvents_OnPlayerUpdateStat;

            Logger.Log($"SCCP by SharkeyMyrl, version: {Version}");
        }

        protected override void Unload()
        {
            DicPoints.Clear();

            U.Events.OnPlayerConnected -= Events_OnPlayerConnected;
            U.Events.OnPlayerDisconnected -= Events_OnPlayerDisconnected;
            UnturnedPlayerEvents.OnPlayerUpdateStat -= UnturnedPlayerEvents_OnPlayerUpdateStat;

            Logger.Log("SCCP has been unloaded!");
        }

        private async void OnServerConnected(Steamworks.CSteamID steamId)
        {
            if (!SharkTank.Config.SyncRanks)
                return;
            UnturnedPlayer untPlayer = UnturnedPlayer.FromCSteamID(steamId);
            await UpdateRanks(untPlayer);
        }

        private async void OnServerDisconnected(Steamworks.CSteamID steamId)
        {
            if (!SharkTank.Config.SyncRanks)
                return;
            UnturnedPlayer untPlayer = UnturnedPlayer.FromCSteamID(steamId);
            await UpdateRanks(untPlayer);
        }

        private void Events_OnPlayerConnected(UnturnedPlayer player)
        {
            RankDatabase.AddUpdatePlayer(player.CSteamID.ToString(), player.DisplayName);
            var rankInfo = RankDatabase.GetAccountBySteamId(player.CSteamID.ToString());
            DicPoints.Add(player.CSteamID, int.Parse(rankInfo.Points));

            if (Instance.Configuration.Instance.EnableRankNotificationOnJoin)
                UnturnedChat.Say(player,
                    Instance.Translations.Instance.Translate("rank_self", rankInfo.Points, rankInfo.CurrentRank,
                        Instance.GetLevel(int.Parse(rankInfo.Points)).Name), configNotificationColor);
            if (Instance.Configuration.Instance.EnableRankNotificationOnJoinGlobal)
                UnturnedChat.Say(
                    Instance.Translations.Instance.Translate("general_onjoin", rankInfo.Points, rankInfo.CurrentRank,
                        Instance.GetLevel(int.Parse(rankInfo.Points)).Name, player.DisplayName),
                    configNotificationColorJoinLeaveGlobal);
        }

        private void Events_OnPlayerDisconnected(UnturnedPlayer player)
        {
            var playerExists = DicPoints.TryGetValue(player.CSteamID, out var playerPoints);
            if (playerExists) DicPoints.Remove(player.CSteamID);

            if (Instance.Configuration.Instance.EnableRankNotificationOnLeaveGlobal)
                UnturnedChat.Say(
                    Instance.Translations.Instance.Translate("general_onleave", playerPoints,
                        RankDatabase.GetRankBySteamId(player.CSteamID.ToString()),
                        Instance.GetLevel(playerPoints).Name, player.DisplayName),
                    configNotificationColorJoinLeaveGlobal);
        }

        private void UnturnedPlayerEvents_OnPlayerUpdateStat(UnturnedPlayer player, EPlayerStat stat)
        {
            var configEvent = Instance.Configuration.Instance.Events.Find(x => x.EventName == stat.ToString());
            if (configEvent == null) return;

            if (configEvent.Notify)
            {
                var playerExists = DicPoints.TryGetValue(player.CSteamID, out var oldPoints);
                if (playerExists)
                    UnturnedChat.Say(player,
                        Translate("event_" + configEvent.EventName, configEvent.Points,
                            oldPoints + configEvent.Points), configNotificationColor);
            }

            UpdatePoints(player, configEvent.Points);
        }


        public async Task UpdateRanks(UnturnedPlayer untPlayer)
        {
            List<RocketPermissionsGroup> localRanks = R.Permissions.GetGroups(untPlayer, false);
            List<string> syncedRanks = await RankDatabase.GetRanks(untPlayer.Id);

            // Update local ranks
            foreach (string synced in syncedRanks)
            {
                // They already have rank
                if (localRanks.Any(pg => pg.Id.Equals(synced)))
                    break;

                AddRank(untPlayer, synced);
            }

            // Update global ranks
            foreach (var local in localRanks)
            {
                // Ignore these ranks.
                if (SharkTank.Config.Ignorelist.Contains(local.Id) && SharkTank.Config.UseIgnorelist)
                    break;

                // They already have rank
                if (syncedRanks.Contains(local.Id))
                    break;

                // Removed and Ignore whitelisted ranks.
                if (SharkTank.Config.Whitelist.Contains(local.Id) && SharkTank.Config.UseWhitelist)
                {
                    RemoveRank(untPlayer, local.Id);
                    break;
                }

                await AddRank(untPlayer, local);
            }
        }

        public bool GroupExists(string group)
        {
            RocketPermissionsManager pm = R.Instance.GetComponent<RocketPermissionsManager>();
            RocketPermissionsGroup pg = pm.GetGroup(group);

            if (pg == null)
                return false;
            return true;
        }

        public void AddRank(UnturnedPlayer untPlayer, string pg)
        {
            RocketPermissionsManager rocketPerms = R.Instance.GetComponent<RocketPermissionsManager>();

            if (GroupExists(pg))
            {
                rocketPerms.AddPlayerToGroup(pg, untPlayer);

                if (Config.LogChanges)
                    Logger.Log(Translate("log_rank_added_to", untPlayer.DisplayName, untPlayer.Id, pg));
            }
        }
        public async Task AddRank(string steam64, string name, string pg)
        {
            List<string> oldRanks = await RankDatabase.GetRanks(steam64);

            oldRanks.Add(pg.Trim());

            // Join and remove the annoying leading ,
            string newRanks = string.Join(",", oldRanks).TrimStart(',');

            await RankDatabase.UpdateRanks(steam64, newRanks);
        }
        public async Task AddRank(UnturnedPlayer untPlayer, RocketPermissionsGroup pg)
        {
            List<string> oldRanks = await RankDatabase.GetRanks(untPlayer.CSteamID.ToString());

            oldRanks.Add(pg.Id.Trim());

            // Join and remove the annoying leading ,
            string newRanks = string.Join(",", oldRanks).TrimStart(',');

            await RankDatabase.UpdateRanks(untPlayer.CSteamID.ToString(), newRanks);
        }

        public void RemoveRank(UnturnedPlayer untPlayer, string pg)
        {
            RocketPermissionsManager rocketPerms = R.Instance.GetComponent<RocketPermissionsManager>();

            if (GroupExists(pg))
            {
                rocketPerms.RemovePlayerFromGroup(pg, untPlayer);

                if (Config.LogChanges)
                    Logger.Log(Translate("log_rank_removed_from", untPlayer.DisplayName, untPlayer.Id, pg));
            }
        }
        public async Task RemoveRank(string steam64, string pg)
        {
            List<string> oldRanks = await RankDatabase.GetRanks(steam64);

            oldRanks.Remove(pg.Trim());

            // Join and remove the annoying leading ,
            string newRanks = string.Join(",", oldRanks).TrimStart(',');

            await RankDatabase.UpdateRanks(steam64, newRanks);
        }
        public async Task RemoveRank(UnturnedPlayer untPlayer, RocketPermissionsGroup pg)
        {
            List<string> oldRanks = await RankDatabase.GetRanks(untPlayer.CSteamID.ToString());

            oldRanks.Remove(pg.Id.Trim());

            // Join and remove the annoying leading ,
            string newRanks = string.Join(",", oldRanks).TrimStart(',');

            await RankDatabase.UpdateRanks(untPlayer.CSteamID.ToString(), newRanks);
        }

        public void UpdatePoints(UnturnedPlayer player, int points)
        {
            var playerExists = DicPoints.TryGetValue(player.CSteamID, out var oldPoints);

            if (!playerExists) return;

            RankDatabase.AddPoints(player.CSteamID.ToString(), points);
            DicPoints[player.CSteamID] += points;

            var newPoints = oldPoints + points;
            var configLevelOld = GetLevel(oldPoints);
            var configLevelNew = GetLevel(newPoints);

            if (configLevelOld.Name == configLevelNew.Name) return;

            if (Instance.Configuration.Instance.EnableLevelUpNotification)
                UnturnedChat.Say(player, Translate("level_up", newPoints, configLevelNew.Name),
                    configNotificationColor);
            if (Instance.Configuration.Instance.EnableLevelUpNotificationGlobal)
                UnturnedChat.Say(
                    Translate("level_up_global", newPoints, configLevelNew.Name, player.DisplayName),
                    configNotificationColorGlobal);

            if (configLevelNew.KitReward)
                try
                {
                    KitReward(configLevelNew, player);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Issue occured while giving a kit reward: {ex.Message}");
                }

            if (configLevelNew.PermissionGroupReward)
                try
                {
                    PermissionGroupReward(configLevelNew, player);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Issue occured while giving a permission group reward: {ex.Message}");
                }

            if (!configLevelNew.UconomyReward) return;

            try
            {
                UconomyReward(configLevelNew, player);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Issue occured while giving a uconomy reward: {ex.Message}");
            }
        }

        public Level GetLevel(int points)
        {
            foreach (var configLevel in Instance.Configuration.Instance.Level)
                if (points >= configLevel.Points)
                    return configLevel;

            return null;
        }

        private void KitReward(Level level, UnturnedPlayer player)
        {
            var rewardKit = Kits.Instance.Configuration.Instance.Kits.FirstOrDefault(k =>
                string.Equals(k.Name, level.KitName, StringComparison.CurrentCultureIgnoreCase));
            if (rewardKit == null)
            {
                Logger.LogWarning("Kit " + level.KitName + " not found.");
                return;
            }

            foreach (var item in rewardKit.Items)
                if (!player.GiveItem(item.ItemId, item.Amount))
                    Logger.Log($"Failed giving a item to {player.CharacterName} ({item.ItemId}, {item.Amount})");
            player.Experience += rewardKit.XP.Value;

            if (level.KitNotify)
                UnturnedChat.Say(player, Translate("level_up_kit", level.KitName), configNotificationColor);
        }

        private void PermissionGroupReward(Level level, IRocketPlayer player)
        {
            var result = R.Permissions.AddPlayerToGroup(level.PermissionGroupName, player);

            switch (result)
            {
                case RocketPermissionsProviderResult.GroupNotFound:
                    Logger.LogWarning(
                        $"Group {level.PermissionGroupName} does not exist. Group was not given to player.");
                    break;
                case RocketPermissionsProviderResult.Success:
                    if (level.PermissionGroupNotify)
                        UnturnedChat.Say(player, Translate("level_up_rank", level.PermissionGroupName),
                            configNotificationColor);
                    break;
            }
        }

        private void UconomyReward(Level level, IRocketPlayer player)
        {
            Uconomy.Instance.Database.IncreaseBalance(player.Id, level.UconomyAmount);
            if (level.UconomyNotify)
                UnturnedChat.Say(player,
                    Translate("level_up_uconomy",
                        level.UconomyAmount +
                        Uconomy.Instance.Configuration.Instance.MoneyName),
                    configNotificationColor);
        }

        public override TranslationList DefaultTranslations => new TranslationList()
        {
            {"general_onjoin", "[{2}] {3} ({0} points, rank {1}) connected to the server."},
            {"general_onleave", "[{2}] {3} ({0} points, rank {1}) disconnected from the server."},
            {"general_not_found", "Player not found."},
            {"general_invalid_parameter", "Invalid parameter."},
            {"rank_self", "Your current rank: {1} with {0} points [{2}]"},
            {"rank_other", "{3}'s current rank: {1} with {0} points [{2}]"},
            {"ranking_empty", "Ranking is empty."},
            {"nan", "{0} is not a number."},
            {"list_1", "The top 3 players:"},
            {"list_2", "{1}st: [{2}] {3} ({0} points)"},
            {"list_3", "{1}nd: [{2}] {3} ({0} points)"},
            {"list_4", "{1}rd: [{2}] {3} ({0} points)"},
            {"list_search", "Rank {1}: [{2}] {3} ({0} points)"},
            {"list_search_not_found", "Rank not found."},
            {"points_reset_player", "Your points have been reset."},
            {"points_reset_caller", "{0}'s points have been reset."},
            {"points_set_player", "Your points have been set to {0}."},
            {"points_set_caller", "{1}'s points have been set to {0}."},
            {"points_add_player", "You received {0} points."},
            {"points_add_caller", "You sent {0} points to {1}."},
            {"points_remove_player", "You lost {0} points."},
            {"points_remove_caller", "You removed {0} points from {1}."},
            {"level_up", "You went up: {1} with {0} points."},
            {"level_up_kit", "You went up and received the kit {0}."},
            {"level_up_rank", "You went up and recieved the permission rank {0}."},
            {"level_up_uconomy", "You went up and received {0}."},
            {"level_up_global", "{2} went up: {1} with {0} points."},
            {"event_ACCURACY", "You received {0} points. ({1} points)"},
            {"event_ARENA_WINS", "You received {0} points. ({1} points)"},
            {"event_DEATHS_PLAYERS", "You received {0} points. ({1} points)"},
            {"event_FOUND_BUILDABLES", "You received {0} points. ({1} points)"},
            {"event_FOUND_CRAFTS", "You received {0} points. ({1} points)"},
            {"event_FOUND_EXPERIENCE", "You received {0} points. ({1} points)"},
            {"event_FOUND_FISHES", "You received {0} points. ({1} points)"},
            {"event_FOUND_ITEMS", "You received {0} points. ({1} points)"},
            {"event_FOUND_PLANTS", "You received {0} points. ({1} points)"},
            {"event_FOUND_RESOURCES", "You received {0} points. ({1} points)"},
            {"event_FOUND_THROWABLES", "You received {0} points. ({1} points)"},
            {"event_HEADSHOTS", "You received {0} points. ({1} points)"},
            {"event_KILLS_ANIMALS", "You received {0} points. ({1} points)"},
            {"event_KILLS_PLAYERS", "You received {0} points. ({1} points)"},
            {"event_KILLS_ZOMBIES_MEGA", "You received {0} points. ({1} points)"},
            {"event_KILLS_ZOMBIES_NORMAL", "You received {0} points. ({1} points)"},
            {"event_NONE", "You received {0} points. ({1} points)"},
            {"event_TRAVEL_FOOT", "You received {0} points. ({1} points)"},
            {"event_TRAVEL_VEHICLE", "You received {0} points. ({1} points)"},

            // Messages
            {"has_been_added_to", "{0} has been added to rank {1}!"},
            {"has_been_removed_from", "{0} has been removed from rank {1}!"},
            {"removed_from_database", "{0} has been removed from the Ranks database!"},

            // Warnings / Errors
            {"cmds_not_enabled", "Commands aren't enabled! Enable them in the config!"},
            {"not_in_database", "{0} is not in the Ranks database!"},

            // Change messages
            {"log_rank_added_to", "{0} ({1}) has been added to {2}!"},
            {"log_rank_removed_from", "{0} ({1}) has been removed from {2}!"}
        };
    }
}