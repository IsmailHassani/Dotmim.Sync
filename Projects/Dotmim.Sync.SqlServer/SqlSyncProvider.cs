﻿using Dotmim.Sync.Builders;
using Dotmim.Sync.Manager;
using Dotmim.Sync.SqlServer.Builders;
using Dotmim.Sync.SqlServer.Manager;
using Dotmim.Sync.SqlServer.Scope;
using System;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace Dotmim.Sync.SqlServer
{
    public class SqlSyncProvider : CoreProvider
    {
        private DbMetadata dbMetadata;
        static string providerType;
        public SqlSyncProvider() : base()
        { }

        public SqlSyncProvider(string connectionString) : base() => this.ConnectionString = connectionString;

        public SqlSyncProvider(SqlConnectionStringBuilder builder) : base()
        {
            if (String.IsNullOrEmpty(builder.ConnectionString))
                throw new Exception("You have to provide parameters to the Sql builder to be able to construct a valid connection string.");

            this.ConnectionString = builder.ConnectionString;
        }

        public override string GetProviderTypeName() => ProviderType;

        public static string ProviderType
        {
            get
            {
                if (!string.IsNullOrEmpty(providerType))
                    return providerType;

                var type = typeof(SqlSyncProvider);
                providerType = $"{type.Name}, {type}";

                return providerType;
            }
        }

        /// <summary>
        /// Gets or sets the Metadata object which parse Sql server types
        /// </summary>
        public override DbMetadata GetMetadata()
        {
            if (dbMetadata == null)
                dbMetadata = new SqlDbMetadata();

            return dbMetadata;
        }

        /// <summary>
        /// Gets a chance to make a retry connection
        /// </summary>
        public override bool ShouldRetryOn(Exception exception) => SqlServerTransientExceptionDetector.ShouldRetryOn(exception);

        public override void EnsureSyncException(SyncException syncException)
        {
            if (!string.IsNullOrEmpty(this.ConnectionString))
            {
                var builder = new SqlConnectionStringBuilder(this.ConnectionString);

                syncException.DataSource = builder.DataSource;
                syncException.InitialCatalog = builder.InitialCatalog;
            }

            // Can add more info from SqlException
            var sqlException = syncException.InnerException as SqlException;

            if (sqlException == null)
                return;

            syncException.Number = sqlException.Number;

            return;
        }

        /// <summary>
        /// Sql Server supports to be a server side provider
        /// </summary>
        public override bool CanBeServerProvider => true;


        public override (ParserName tableName, ParserName trackingName) GetParsers(SyncTable tableDescription, SyncSetup setup)
        {
            var originalTableName = ParserName.Parse(tableDescription);

            var pref = setup.TrackingTablesPrefix;
            var suf = setup.TrackingTablesSuffix;

            // be sure, at least, we have a suffix if we have empty values. 
            // othewise, we have the same name for both table and tracking table
            if (string.IsNullOrEmpty(pref) && string.IsNullOrEmpty(suf))
                suf = "_tracking";

            var trakingTableNameString = $"{pref}{originalTableName.ObjectName}{suf}";

            if (!string.IsNullOrEmpty(originalTableName.SchemaName))
                trakingTableNameString = $"{originalTableName.SchemaName}.{trakingTableNameString}";

            var trackingTableName = ParserName.Parse(trakingTableNameString);

            return (originalTableName, trackingTableName);
        }

        public override DbConnection CreateConnection() => new SqlConnection(this.ConnectionString);
        public override DbScopeBuilder GetScopeBuilder(string scopeInfoTableName) => new SqlScopeBuilder(scopeInfoTableName);

        /// <summary>
        /// Get the table builder. Table builder builds table, stored procedures and triggers
        /// </summary>
        public override DbTableBuilder GetTableBuilder(SyncTable tableDescription, ParserName tableName, ParserName trackingTableName, SyncSetup setup)
        => new SqlTableBuilder(tableDescription, tableName, trackingTableName, setup);

        public override DbSyncAdapter GetSyncAdapter(SyncTable tableDescription, ParserName tableName, ParserName trackingTableName, SyncSetup setup)
            => new SqlSyncAdapter(tableDescription, tableName, trackingTableName, setup);

        public override DbBuilder GetDatabaseBuilder() => new SqlBuilder();

    }
}
