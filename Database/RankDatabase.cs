using LandSharks.Config.Objects;
using LandSharks.Database.Objects;
using MySql.Data.MySqlClient;
using Rocket.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using I18N.West;

namespace LandSharks.Database
{
    public class RankDatabase
    {
        internal RankDatabase()
        {
            new CP1250();

            CheckSchema();
        }

        internal MySqlConnection CreateConnection()
        {
            MySqlConnection connection = null;
            try
            {
                connection = new MySqlConnection($"SERVER={SharkTank.Config.ranks.DatabaseAddress};DATABASE={SharkTank.Config.ranks.DatabaseName};UID={SharkTank.Config.ranks.DatabaseUsername};PASSWORD={SharkTank.Config.ranks.DatabasePassword};PORT={SharkTank.Config.ranks.DatabasePort};");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            return connection;
        }

        internal void CheckSchema()
        {
            using (MySqlConnection connection = this.CreateConnection())
            {
                try
                {
                    MySqlCommand command = connection.CreateCommand();
                    connection.Open();
                    command.CommandText = "SHOW TABLES LIKE '" + SharkTank.Config.ranks.DatabaseTableName + "';";

                    object test = command.ExecuteScalar();
                    if (test == null)
                    {
                        Logger.Log("Tables not found, creating!");
                        command.CommandText =
                            $@"CREATE TABLE `{SharkTank.Config.ranks.DatabaseTableName}`
                            (
	                            `steamId` VARCHAR(32) NOT NULL COLLATE,
                                `points` INT(32) NOT NULL DEFAULT '0',
                                `name` varchar(64) NOT NULL,
	                            `ranks` TEXT NULL DEFAULT NULL COLLATE,
                                `lastUpdated` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP, 
	                            PRIMARY KEY (`steamId`) USING BTREE
                            ) COLLATE = 'utf8_general_ci' ENGINE = InnoDB;";
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
            }
        }

        internal async Task<bool> CheckExists(string id)
        {
            using (MySqlConnection connection = CreateConnection())
            {
                try
                {
                    MySqlCommand command = new MySqlCommand
                    (
                        $@"SELECT EXISTS(SELECT 1 FROM `{SharkTank.Config.ranks.DatabaseTableName}`
                        WHERE `steamId` = @steamId);", connection
                    );

                    command.Parameters.AddWithValue("@steamId", id);
                    await connection.OpenAsync();

                    var status = Convert.ToInt32(await command.ExecuteScalarAsync());

                    await connection.CloseAsync();
                    return status > 0;
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                    return false;
                }
            }
        }

        internal async Task<List<string>> GetRanks(string steamId)
        {
            using (MySqlConnection connection = CreateConnection())
            {
                string output = "";

                try
                {
                    MySqlCommand command = new MySqlCommand
                    (
                        $@"SELECT * FROM {SharkTank.Config.ranks.DatabaseTableName}
                        WHERE steamId = @steamId", connection
                    );

                    command.Parameters.AddWithValue("@steamId", steamId.ToString());
                    await connection.OpenAsync();
                    var dataReader = await command.ExecuteReaderAsync(System.Data.CommandBehavior.SingleRow);

                    while (await dataReader.ReadAsync())
                    {
                        output = Convert.ToString(dataReader["ranks"]);
                    }
                    dataReader.Close();
                    await connection.CloseAsync();

                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
                var list = output.Split(',').ToList();
                return list;
            }
        }

        internal async Task RemoveRanks(string steamId)
        {
            using (MySqlConnection connection = CreateConnection())
            {
                try
                {
                    MySqlCommand command = new MySqlCommand
                    (
                        $@"UPDATE {SharkTank.Config.ranks.DatabaseTableName}
                        SET `ranks` = @Ranks WHERE `steamId` = @steamId", connection
                    );

                    command.Parameters.AddWithValue("@steamId", steamId.ToString());
                    command.Parameters.AddWithValue("@Ranks", "");

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                    await connection.CloseAsync();
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
            }
        }

        internal async Task UpdateRanks(string steamId, string ranks)
        {
            using (MySqlConnection connection = CreateConnection())
            {
                try
                {
                    MySqlCommand command = new MySqlCommand
                    (
                        $@"UPDATE {SharkTank.Config.ranks.DatabaseTableName}
                        SET `ranks` = @Ranks WHERE `steamId` = @steamId", connection
                    );

                    command.Parameters.AddWithValue("@steamId", steamId.ToString());
                    command.Parameters.AddWithValue("@Ranks", ranks);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                    await connection.CloseAsync();
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
            }
        }

        public void AddUpdatePlayer(string steamId, string lastDisplayName)
        {
            try
            {
                ThreadPool.QueueUserWorkItem(yes => AddUpdatePlayerThread(steamId, lastDisplayName));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        private void AddUpdatePlayerThread(string steamId, string lastDisplayName)
        {
            Logger.Log("Initializing player in database, for " + steamId);
            ExecuteQuery(EQueryType.NonQuery,
                $"INSERT INTO `{SharkTank.Config.ranks.DatabaseTableName}` (`steamId`,`name`) VALUES (@steamId, @name) ON DUPLICATE KEY UPDATE name = @name;",
                new MySqlParameter("@name", lastDisplayName), new MySqlParameter("@steamId", steamId));
        }

        public void AddPoints(string steamId, int points)
        {
            try
            {
                ThreadPool.QueueUserWorkItem(yes => AddPointsThread(steamId, points));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        private void AddPointsThread(string steamId, int points)
        {
            ExecuteQuery(EQueryType.NonQuery,
                $"UPDATE `{SharkTank.Config.ranks.DatabaseTableName}` SET `points`=`points`+{points} WHERE `steamId`=@steamId;",
                new MySqlParameter("@steamId", steamId));
        }

        public void SetPoints(string steamId, int points)
        {
            try
            {
                ThreadPool.QueueUserWorkItem(yes => SetPointsThread(steamId, points));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        private void SetPointsThread(string steamId, int points)
        {
            ExecuteQuery(EQueryType.NonQuery,
                $"UPDATE `{SharkTank.Config.ranks.DatabaseTableName}` SET `points`={points} WHERE `steamId`=@steamId;",
                new MySqlParameter("@steamId", steamId));
        }

        public PlayerRank GetAccountBySteamId(string steamId)
        {
            var readerResult = (List<Row>)ExecuteQuery(EQueryType.Reader,
                $"SELECT * FROM (SELECT t.steamId, t.points, t.name, @`rownum` := @`rownum` + 1 AS currentRank FROM `{SharkTank.Config.ranks.DatabaseTableName}` t JOIN (SELECT @`rownum` := 0) r ORDER BY t.points DESC) x WHERE x.steamId = @steamId;",
                new MySqlParameter("@steamId", steamId));

            return readerResult?.Select(k => new PlayerRank
            {
                Points = k.Values["points"].ToString(),
                CurrentRank = k.Values["currentRank"].ToString(),
                LastDisplayName = k.Values["name"].ToString()
            }).First();
        }

        public PlayerRank GetAccountByRank(int rank)
        {
            var readerResult = (List<Row>)ExecuteQuery(EQueryType.Reader,
                $"SELECT * FROM (SELECT t.steamId, t.points, t.name, @`rownum` := @`rownum` + 1 AS currentRank FROM `{SharkTank.Config.ranks.DatabaseTableName}` t JOIN (SELECT @`rownum` := 0) r ORDER BY t.points DESC) x WHERE x.currentRank = {rank};");

            return readerResult?.Select(k => new PlayerRank
            {
                Points = k.Values["points"].ToString(),
                CurrentRank = k.Values["currentRank"].ToString(),
                LastDisplayName = k.Values["name"].ToString()
            }).First();
        }

        public int GetRankBySteamId(string steamId)
        {
            var output = 0;
            var result = ExecuteQuery(EQueryType.Scalar,
                $"SELECT `currentRank` FROM (SELECT t.steamId, t.points, @`rownum` := @`rownum` + 1 AS currentRank FROM `{SharkTank.Config.ranks.DatabaseTableName}` t JOIN (SELECT @`rownum` := 0) r ORDER BY t.points DESC) x WHERE x.steamId = @steamId;",
                new MySqlParameter("@steamId", steamId));

            if (result != null) int.TryParse(result.ToString(), out output);

            return output;
        }

        public IEnumerable<PlayerRank> GetTopRanks(int limit)
        {
            var readerResult = (List<Row>)ExecuteQuery(EQueryType.Reader,
                $"SELECT * FROM (SELECT t.steamId, t.points, t.name, @`rownum` := @`rownum` + 1 AS currentRank FROM `{SharkTank.Config.ranks.DatabaseTableName}` t JOIN (SELECT @`rownum` := 0) r ORDER BY t.points DESC) x LIMIT 0,{limit};");

            return readerResult?.Select(row => new PlayerRank
            {
                Points = row.Values["points"].ToString(),
                CurrentRank = row.Values["currentRank"].ToString(),
                LastDisplayName = row.Values["name"].ToString()
            });
        }

        private object ExecuteQuery(EQueryType queryType, string query, params MySqlParameter[] parameters)
        {
            object result = null;
            MySqlDataReader reader = null;

            using (var connection = CreateConnection())
            {
                try
                {
                    var command = connection.CreateCommand();
                    command.CommandText = query;

                    foreach (var parameter in parameters)
                        command.Parameters.Add(parameter);

                    connection.Open();
                    switch (queryType)
                    {
                        case EQueryType.Reader:
                            var readerResult = new List<Row>();

                            reader = command.ExecuteReader();
                            while (reader.Read())
                                try
                                {
                                    var values = new Dictionary<string, object>();

                                    for (var i = 0; i < reader.FieldCount; i++)
                                    {
                                        var columnName = reader.GetName(i);
                                        values.Add(columnName, reader[columnName]);
                                    }

                                    readerResult.Add(new Row { Values = values });
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogError(
                                        $"The following query threw an error during reader execution:\nQuery: \"{query}\"\nError: {ex.Message}");
                                }

                            result = readerResult;
                            break;
                        case EQueryType.Scalar:
                            result = command.ExecuteScalar();
                            break;
                        case EQueryType.NonQuery:
                            result = command.ExecuteNonQuery();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
                finally
                {
                    reader?.Close();
                    connection.Close();
                }
            }

            return result;
        }
    }
}
