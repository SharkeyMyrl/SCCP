using LandSharks.Config.Objects;
using Rocket.API;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace LandSharks.Config
{
    public class Configuration : IRocketPluginConfiguration
    {
        public DatabaseConfig ranks;
        public DatabaseConfig vault;

        public bool UseWhitelist;
        public bool UseIgnorelist;
        public bool Enabled;
        public bool SyncRanks;
        public bool EnableCommands;
        public bool LogChanges;

        public bool EnableLevelUpNotification;
        public bool EnableLevelUpNotificationGlobal;
        public bool EnableRankNotificationOnJoin;
        public bool EnableRankNotificationOnJoinGlobal;
        public bool EnableRankNotificationOnLeaveGlobal;
        public string NotificationColor;
        public string NotificationColorGlobal;
        public string NotificationColorJoinLeaveGlobal;
        public List<Event> Events;
        public List<Level> Level;

        [XmlArray(ElementName = "Whitelist")]
        [XmlArrayItem(ElementName = "Rank")]
        public List<string> Whitelist;

        [XmlArray(ElementName = "Ignorelist")]
        [XmlArrayItem(ElementName = "Rank")]
        public List<string> Ignorelist;

        public void LoadDefaults()
        {
            vault = new DatabaseConfig
            {
                DatabaseAddress = "127.0.0.1",
                DatabaseUsername = "unturned",
                DatabasePassword = "password",
                DatabaseName = "unturned",
                DatabaseTableName = "ranks",
                DatabasePort = 3306
            };

            ranks = new DatabaseConfig
            {
                DatabaseAddress = "127.0.0.1",
                DatabaseUsername = "unturned",
                DatabasePassword = "password",
                DatabaseName = "unturned",
                DatabaseTableName = "ranks",
                DatabasePort = 3306
            };

            UseWhitelist = false;
            UseIgnorelist = true;
            Enabled = true;
            SyncRanks = true;
            EnableCommands = true;
            LogChanges = true;

            EnableLevelUpNotification = false;
            EnableLevelUpNotificationGlobal = true;
            EnableRankNotificationOnJoin = true;
            EnableRankNotificationOnJoinGlobal = false;
            EnableRankNotificationOnLeaveGlobal = false;
            NotificationColor = "Green";
            NotificationColorGlobal = "Gray";
            NotificationColorJoinLeaveGlobal = "Green";

            Events = new List<Event>
            {
                new Event {EventName = "KILLS_ZOMBIES_NORMAL", Notify = false, Points = 10},
                new Event {EventName = "KILLS_ZOMBIES_MEGA", Notify = true, Points = 50},
                new Event {EventName = "KILLS_PLAYERS", Notify = true, Points = 60}
            };

            Level = new List<Level>
            {
                new Level {Points = 0, Name = "Pig"},
                new Level
                {
                    Points = 100, Name = "Small Zombie", UconomyReward = true, UconomyNotify = true, UconomyAmount = 100
                },
                new Level {Points = 200, Name = "Zombie", KitReward = true, KitNotify = true, KitName = "Zombie"},
                new Level
                {
                    Points = 500, Name = "Giant Zombie", KitReward = true, KitNotify = true, KitName = "Giant Zombie",
                    PermissionGroupReward = true, PermissionGroupNotify = true, PermissionGroupName = "VIP",
                    UconomyReward = true, UconomyNotify = false, UconomyAmount = 200
                }
            };

            Whitelist = new List<string>()
            {
                "mod",
                "vip"
            };

            Ignorelist = new List<string>()
            {
                "default"
            };



        }
    }

}