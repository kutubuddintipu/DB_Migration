using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbMigration
{
    public class Models
    {
        public class TableMap
        {
            public string Entity { get; set; }
            public string OldTable { get; set; }
            public string NewTable { get; set; }
            public string OldPk { get; set; }
            public string NewPk { get; set; }
            public bool GenerateNewId { get; set; }
            public List<ColumnMap> Columns { get; set; }
        }

        public class ColumnMap
        {
            public string Old { get; set; }
            public string New { get; set; }
        }
    }
}