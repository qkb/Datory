﻿using System;
using System.Data;
using Dapper;
using Datory.Utils;

namespace Datory
{
    public class Database
    {
        public DatabaseType DatabaseType { get; }

        public string ConnectionString { get; }

        public string Name { get; }

        public string Owner { get; }

        public IDbConnection Connection { get; }

        public Database(DatabaseType databaseType, IDbConnection connection)
        {
            DatabaseType = databaseType;
            Connection = connection;

            ConnectionString = connection.ConnectionString;
            Name = connection.Database;

            //if (databaseType == DatabaseType.Oracle)
            //{
            //    var index1 = connectionString.IndexOf("SERVICE_NAME=", StringComparison.Ordinal);
            //    var index2 = connectionString.IndexOf(")));", StringComparison.Ordinal);
            //    Name = connectionString.Substring(index1 + 13, index2 - index1 - 13);
            //}
            //else
            //{
            //    Name = SqlUtils.GetValueFromConnectionString(ConnectionString, "Database");
            //}

            Owner = SqlUtils.GetConnectionStringUserId(ConnectionString);
        }
    }
}