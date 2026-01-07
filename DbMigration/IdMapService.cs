using Dapper;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbMigration
{
    public class IdMapService
    {
        private readonly NpgsqlConnection _db;
        private readonly NpgsqlTransaction _tx;

        public IdMapService(NpgsqlConnection db, NpgsqlTransaction tx)
        {
            _db = db;
            _tx = tx;
        }

        public void Save(string entity, long oldId, long newId)
        {
            _db.Execute(
                @"INSERT INTO migration_id_map(entity_name, old_id, new_id)
          SELECT @e, @o, @n
          WHERE NOT EXISTS (
              SELECT 1
              FROM migration_id_map
              WHERE entity_name = @e
                AND old_id = @o
          )",
                new { e = entity, o = oldId, n = newId },
                _tx
            );
        }

        public long Get(string entity, long oldId)
        {
            return _db.ExecuteScalar<long>(
                @"SELECT new_id FROM migration_id_map
              WHERE entity_name=@e AND old_id=@o",
                new { e = entity, o = oldId },
                _tx
            );
        }
    }
}