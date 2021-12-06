﻿using Dotmim.Sync.Batch;
using Dotmim.Sync.Builders;
using Dotmim.Sync.Enumerations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dotmim.Sync
{
    public partial class RemoteOrchestrator : BaseOrchestrator
    {

        /// <summary>
        /// Get the server scope histories
        /// </summary>
        public virtual async Task<List<ServerHistoryScopeInfo>> GetServerHistoryScopesAsync(DbConnection connection = default, DbTransaction transaction = default, CancellationToken cancellationToken = default, IProgress<ProgressArgs> progress = null)
        {
            await using var runner = await this.GetConnectionAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
            var ctx = this.GetContext();
            ctx.SyncStage = SyncStage.ScopeLoading;

            try
            {
                List<ServerHistoryScopeInfo> serverHistoryScopes = null;

                // Get Scope Builder
                var scopeBuilder = this.GetScopeBuilder(this.Options.ScopeInfoTableName);

                var exists = await this.InternalExistsScopeInfoTableAsync(ctx, DbScopeType.ServerHistory, scopeBuilder, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

                if (!exists)
                    await this.InternalCreateScopeInfoTableAsync(ctx, DbScopeType.ServerHistory, scopeBuilder, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

                // Get scope if exists
                serverHistoryScopes = await this.InternalGetAllScopesAsync<ServerHistoryScopeInfo>(ctx, DbScopeType.ServerHistory, this.ScopeName, scopeBuilder, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

                await runner.CommitAsync().ConfigureAwait(false);

                return serverHistoryScopes;
            }
            catch (Exception ex)
            {
                throw GetSyncError(ex);
            }

        }


        /// <summary>
        /// Get the local configuration, ensures the local scope is created
        /// </summary>
        /// <returns>Server scope info, containing all scopes names, version, setup and related schema infos</returns>
        public virtual async Task<ServerScopeInfo> GetServerScopeAsync(DbConnection connection = default, DbTransaction transaction = default, CancellationToken cancellationToken = default, IProgress<ProgressArgs> progress = null)
        {
            await using var runner = await this.GetConnectionAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
            var ctx = this.GetContext();
            ctx.SyncStage = SyncStage.ScopeLoading;

            try
            {
                ServerScopeInfo serverScopeInfo = null;

                var scopeBuilder = this.GetScopeBuilder(this.Options.ScopeInfoTableName);

                var exists = await this.InternalExistsScopeInfoTableAsync(ctx, DbScopeType.Server, scopeBuilder, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

                if (!exists)
                    await this.InternalCreateScopeInfoTableAsync(ctx, DbScopeType.Server, scopeBuilder, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

                exists = await this.InternalExistsScopeInfoTableAsync(ctx, DbScopeType.ServerHistory, scopeBuilder, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

                if (!exists)
                    await this.InternalCreateScopeInfoTableAsync(ctx, DbScopeType.ServerHistory, scopeBuilder, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

                serverScopeInfo = await this.InternalGetScopeAsync<ServerScopeInfo>(ctx, DbScopeType.Server, this.ScopeName, scopeBuilder, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

                // if serverscopeinfo is a new, because we never run any sync before, grab schema and affect setup
                if (serverScopeInfo.Setup == null && serverScopeInfo.Schema == null)
                {
                    // 1) Get Schema from remote provider
                    var schema = await this.InternalGetSchemaAsync(ctx, this.Setup, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

                    // 2) Provision
                    var provision = SyncProvision.TrackingTable | SyncProvision.StoredProcedures | SyncProvision.Triggers;
                    schema = await InternalProvisionAsync(ctx, false, schema, this.Setup, provision, serverScopeInfo, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

                    return serverScopeInfo;
                }

                // Compare serverscope setup with current
                if (!serverScopeInfo.Setup.EqualsByProperties(this.Setup))
                {
                    SyncSet schema;
                    // 1) Get Schema from remote provider
                    schema = await this.InternalGetSchemaAsync(ctx, this.Setup, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

                    // Migrate the old setup (serverScopeInfo.Setup) to the new setup (this.Setup) based on the new schema 
                    await this.InternalMigrationAsync(ctx, schema, serverScopeInfo.Setup, this.Setup, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

                    serverScopeInfo.Setup = this.Setup;
                    serverScopeInfo.Schema = schema;

                    // Write scopes locally
                    await this.InternalSaveScopeAsync(ctx, DbScopeType.Server, serverScopeInfo, scopeBuilder, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);
                }

                await runner.CommitAsync().ConfigureAwait(false);

                return serverScopeInfo;
            }
            catch (Exception ex)
            {
                throw GetSyncError(ex);
            }

        }



        /// <summary>
        /// Update or Insert a server scope row
        /// </summary>
        public virtual async Task<ServerScopeInfo> SaveServerScopeAsync(ServerScopeInfo scopeInfo, DbConnection connection = default, DbTransaction transaction = default, CancellationToken cancellationToken = default, IProgress<ProgressArgs> progress = null)
        {
            await using var runner = await this.GetConnectionAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
            var ctx = this.GetContext();
            ctx.SyncStage = SyncStage.ScopeWriting;

            try
            {
                var scopeBuilder = this.GetScopeBuilder(this.Options.ScopeInfoTableName);

                var exists = await this.InternalExistsScopeInfoTableAsync(ctx, DbScopeType.Server, scopeBuilder, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);
                if (!exists)
                    await this.InternalCreateScopeInfoTableAsync(ctx, DbScopeType.Server, scopeBuilder, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

                // Write scopes locally
                var scopeInfoUpdated = await this.InternalSaveScopeAsync(ctx, DbScopeType.Server, scopeInfo, scopeBuilder, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

                await runner.CommitAsync().ConfigureAwait(false);

                return scopeInfoUpdated;
            }
            catch (Exception ex)
            {
                throw GetSyncError(ex);
            }
        }

        /// <summary>
        /// Update or Insert a server scope row
        /// </summary>
        public virtual async Task<ServerHistoryScopeInfo> SaveServerHistoryScopeAsync(ServerHistoryScopeInfo scopeInfo, DbConnection connection = default, DbTransaction transaction = default, CancellationToken cancellationToken = default, IProgress<ProgressArgs> progress = null)
        {
            await using var runner = await this.GetConnectionAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
            var ctx = this.GetContext();
            ctx.SyncStage = SyncStage.ScopeWriting;

            try
            {
                var scopeBuilder = this.GetScopeBuilder(this.Options.ScopeInfoTableName);

                var exists = await this.InternalExistsScopeInfoTableAsync(ctx, DbScopeType.ServerHistory, scopeBuilder, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);
                if (!exists)
                    await this.InternalCreateScopeInfoTableAsync(ctx, DbScopeType.ServerHistory, scopeBuilder, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

                // Write scopes locally
                var scopeInfoUpdated = await this.InternalSaveScopeAsync(ctx, DbScopeType.ServerHistory, scopeInfo, scopeBuilder, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

                await runner.CommitAsync().ConfigureAwait(false);

                return scopeInfoUpdated;
            }
            catch (Exception ex)
            {
                throw GetSyncError(ex);
            }
        }
    }
}