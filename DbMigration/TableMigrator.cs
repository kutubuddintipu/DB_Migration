using Dapper;
using Npgsql;
using System.Text;
using static DbMigration.Models;

namespace DbMigration
{
    public static class TableMigrator
    {
        public static void Migrate(
            TableMap map,
            NpgsqlConnection oldDb,
            NpgsqlConnection newDb,
            NpgsqlTransaction tx,
            IdMapService idMap)
        {
            var selectSql =
                $"SELECT {map.OldPk}, {string.Join(",", map.Columns.Select(c => c.Old))} " +
                $"FROM {map.OldTable} ORDER BY trns_treasury_detail_id";

            using var cmd = new NpgsqlCommand(selectSql, oldDb);
            using var reader = cmd.ExecuteReader();

            while (true)
            {
                try
                {
                    if (!reader.Read())
                        break;
                }
                catch (PostgresException ex) when (ex.SqlState == "22021")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"UTF8 ERROR while reading table {map.OldTable}");
                    Console.ResetColor();
                    throw;
                }

                var dict = new Dictionary<string, object>();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var colName = reader.GetName(i);

                    try
                    {
                        var value = reader.GetValue(i);

                        if (value is string s)
                            value = s.Replace("\0", "");

                        dict[colName] = value == DBNull.Value ? null : value;
                    }
                    catch (PostgresException ex) when (ex.SqlState == "22021")
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("=======================================");
                        Console.WriteLine("🚨 INVALID UTF8 COLUMN FOUND");
                        Console.WriteLine($"Table      : {map.OldTable}");
                        Console.WriteLine($"Column     : {colName}");
                        Console.WriteLine($"ColumnIndex: {i}");
                        Console.WriteLine($"Old PK     : {reader[map.OldPk]}");
                        Console.WriteLine("=======================================");
                        Console.ResetColor();

                        throw;
                    }
                }

                InsertRow(map, dict, newDb, tx, idMap);
            }
        }

        private static void InsertRow(
            TableMap map,
            Dictionary<string, object> dict,
            NpgsqlConnection newDb,
            NpgsqlTransaction tx,
            IdMapService idMap)
        {
            var cols = string.Join(",", map.Columns.Select(c => c.New));
            var vals = string.Join(",", map.Columns.Select(c => "@" + c.New));

            var sql = new StringBuilder(
                $"INSERT INTO {map.NewTable} ({cols}) VALUES ({vals})"
            );

            if (map.GenerateNewId)
                sql.Append($" RETURNING {map.NewPk}");

            var parameters = new DynamicParameters();

            foreach (var col in map.Columns)
            {
                object value = dict[col.Old];

                if (value is DateOnly d)
                    value = d.ToDateTime(TimeOnly.MinValue);

                parameters.Add(col.New, value);
            }

            if (map.GenerateNewId)
            {
                long newId = newDb.ExecuteScalar<long>(sql.ToString(), parameters, tx);

                idMap.Save(
                    map.Entity,
                    Convert.ToInt64(dict[map.OldPk]),
                    newId
                );
            }
            else
            {
                try
                {
                    newDb.Execute(sql.ToString(), parameters, tx);
                }
                catch (PostgresException ex) when (ex.SqlState == "22021")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("=======================================");
                    Console.WriteLine("🚨 UTF8 ERROR DETECTED");
                    Console.WriteLine($"Table  : {map.NewTable}");
                    Console.WriteLine($"Old PK : {dict[map.OldPk]}");
                    Console.WriteLine("Finding exact column...");
                    Console.WriteLine("=======================================");
                    Console.ResetColor();

                    foreach (var col in map.Columns)
                    {
                        var testParams = new DynamicParameters();

                        // add ALL previous safe columns
                        foreach (var c in map.Columns)
                        {
                            if (c.New == col.New) break;
                            testParams.Add(c.New, parameters.Get<object>(c.New));
                        }

                        // now add ONLY ONE column at a time
                        testParams.Add(col.New, parameters.Get<object>(col.New));

                        var testCols = string.Join(",", testParams.ParameterNames);
                        var testVals = string.Join(",", testParams.ParameterNames.Select(p => "@" + p));

                        var testSql = $"INSERT INTO {map.NewTable} ({testCols}) VALUES ({testVals})";

                        try
                        {
                            newDb.Execute(testSql, testParams, tx);
                        }
                        catch (PostgresException e) when (e.SqlState == "22021")
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"❌ BAD COLUMN FOUND → {col.New}");
                            Console.ResetColor();

                            throw;
                        }
                    }

                    throw;
                }

                //newDb.Execute(sql.ToString(), parameters, tx);
            }
        }
    }
}