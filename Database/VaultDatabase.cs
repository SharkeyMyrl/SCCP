using MySql.Data.MySqlClient;
using Rocket.Core.Logging;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;

namespace LandSharks.Database
{
    internal class VaultDatabase
    {
        private static MySqlConnection createConnection()
        {
            MySqlConnection result = null;
            try
            {
                result = new MySqlConnection(String.Format("SERVER={0};DATABASE={1};UID={2};PASSWORD={3};PORT={4};", SharkTank.Config.vault.DatabaseAddress, SharkTank.Config.vault.DatabaseName, SharkTank.Config.vault.DatabaseUsername, SharkTank.Config.vault.DatabasePassword, SharkTank.Config.vault.DatabasePort));
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            return result;
        }
        public static void CheckSchema()
        {
            try
            {
                MySqlConnection mySqlConnection = VaultDatabase.createConnection();
                MySqlCommand mySqlCommand = mySqlConnection.CreateCommand();
                mySqlCommand.CommandText = "show tables like '" + SharkTank.Config.vault.DatabaseTableName + "'";
                mySqlConnection.Open();
                if (mySqlCommand.ExecuteScalar() == null)
                {
                    mySqlCommand.CommandText = "CREATE TABLE `" + SharkTank.Config.vault.DatabaseTableName + "` (`id` int(11) NOT NULL AUTO_INCREMENT,`durability` int(3) NOT NULL,`stacksize` int(11) NULL,`x` int(11) NULL,`y` int(11) NULL,`rotation` int(11) NULL,`itemid` int(4) NOT NULL,`metadata` varchar(255) NOT NULL,`csteamid` varchar(32) NOT NULL,PRIMARY KEY (`id`)) ";
                    mySqlCommand.ExecuteNonQuery();
                }
                mySqlConnection.Close();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
        internal static List<ItemJar> RetrieveItems(CSteamID csteamid)
        {
            List<ItemJar> list = new List<ItemJar>();
            try
            {
                int num = 0;
                MySqlConnection mySqlConnection = VaultDatabase.createConnection();
                MySqlCommand mySqlCommand = mySqlConnection.CreateCommand();
                mySqlCommand.CommandText = string.Concat(new object[]
                {
                    "select count(*) from `",
                    SharkTank.Config.vault.DatabaseTableName,
                    "` where `csteamid` = ",
                    csteamid,
                    ";"
                });
                mySqlConnection.Open();
                object obj = mySqlCommand.ExecuteScalar();
                if (obj != null)
                {
                    num = int.Parse(obj.ToString());
                }
                mySqlConnection.Close();
                if (num != 0)
                {
                    mySqlCommand.CommandText = string.Concat(new object[]
                    {
                        "select * from `",
                        SharkTank.Config.vault.DatabaseTableName,
                        "` where `csteamid` = '",
                        csteamid,
                        "' order by id;"
                    });
                    mySqlConnection.Open();
                    MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
                    while (mySqlDataReader.Read())
                    {
                        list.Add(new ItemJar((byte)mySqlDataReader.GetInt32("x"), (byte)mySqlDataReader.GetInt32("y"), (byte)mySqlDataReader.GetInt32("rotation"), new Item(mySqlDataReader.GetUInt16("itemid"), true)
                        {
                            durability = (byte)mySqlDataReader.GetInt32("durability"),
                            metadata = Convert.FromBase64String(mySqlDataReader.GetString("metadata")),
                            amount = (byte)mySqlDataReader.GetInt32("stacksize")
                        }));
                    }
                    mySqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            return list;
        }

        internal static void UpdateItem(CSteamID cSteamID, ItemJar item)
        {
            try
            {
                MySqlConnection mySqlConnection = VaultDatabase.createConnection();
                MySqlCommand mySqlCommand = mySqlConnection.CreateCommand();
                string text = (item.item.metadata == null) ? "" : Convert.ToBase64String(item.item.metadata);
                mySqlCommand.CommandText = string.Concat(new object[]
                {
                    "update `",
                    SharkTank.Config.vault.DatabaseTableName,
                    "` set `metadata`='",
                    text,
                    "' where `csteamid`='",
                    cSteamID,
                    "' and `x`=",
                    item.x,
                    " and `y`=",
                    item.y,
                    " and `itemid` = ",
                    item.item.id,
                    " limit 1;"
                });
                mySqlConnection.Open();
                mySqlCommand.ExecuteNonQuery();
                mySqlConnection.Close();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        internal static bool AddItem(CSteamID cSteamID, ItemJar item)
        {
            try
            {
                MySqlConnection mySqlConnection = VaultDatabase.createConnection();
                MySqlCommand mySqlCommand = mySqlConnection.CreateCommand();
                string text = (item.item.metadata == null) ? "" : Convert.ToBase64String(item.item.metadata);
                mySqlCommand.CommandText = string.Concat(new object[]
                {
                    "insert into `",
                    SharkTank.Config.vault.DatabaseTableName,
                    "` (`csteamid`,`durability`,`x`,`y`,`rotation`,`metadata`,`itemid`,`stacksize`) values(",
                    cSteamID,
                    ",",
                    item.item.durability,
                    ",",
                    item.x,
                    ",",
                    item.y,
                    ",",
                    item.rot,
                    ",'",
                    text,
                    "',",
                    item.item.id,
                    ",",
                    item.item.amount,
                    ");"
                });
                mySqlConnection.Open();
                mySqlCommand.ExecuteNonQuery();
                mySqlConnection.Close();
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            return false;
        }
        internal static bool DeleteItem(CSteamID cSteamID, ItemJar item)
        {
            try
            {
                MySqlConnection mySqlConnection = VaultDatabase.createConnection();
                MySqlCommand mySqlCommand = mySqlConnection.CreateCommand();
                string text = (item.item.metadata == null) ? "" : Convert.ToBase64String(item.item.metadata);
                mySqlCommand.CommandText = string.Concat(new object[]
                {
                    "delete from `",
                    SharkTank.Config.vault.DatabaseTableName,
                    "` where `csteamid`='",
                    cSteamID,
                    "' and `x`=",
                    item.x,
                    " and `y`=",
                    item.y,
                    " and `itemid` = ",
                    item.item.id,
                    " limit 1;"
                });
                mySqlConnection.Open();
                mySqlCommand.ExecuteScalar();
                mySqlConnection.Close();
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            return false;
        }
    }
}
