using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbMigration
{
    public static class Db
    {
        public static NpgsqlConnection Open(string cs)
        {
            var con = new NpgsqlConnection(cs);
            con.Open();
            return con;
        }
    }
}