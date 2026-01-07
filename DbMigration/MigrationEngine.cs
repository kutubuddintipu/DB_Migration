using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static DbMigration.Models;
using System.IO;

namespace DbMigration
{
    public static class MigrationEngine
    {
        public static void Run(string oldCs, string newCs)
        {
            var maps = JsonSerializer.Deserialize<List<TableMap>>(
    File.ReadAllText("tables.json")
) ?? throw new Exception("tables.json could not be loaded");

            using var oldDb = Db.Open(oldCs);
            using var newDb = Db.Open(newCs);
            using var tx = newDb.BeginTransaction();

            newDb.Execute("SET session_replication_role = replica");

            var idMap = new IdMapService(newDb, tx);

            foreach (var map in maps)
            {
                Console.WriteLine($"Migrating {map.OldTable} → {map.NewTable}");
                TableMigrator.Migrate(map, oldDb, newDb, tx, idMap);
            }

            newDb.Execute("SET session_replication_role = DEFAULT");
            tx.Commit();
        }
    }
}