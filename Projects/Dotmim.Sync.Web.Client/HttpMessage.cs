﻿using Dotmim.Sync.Batch;
using Dotmim.Sync.Builders;

using System;
using System.Collections.Generic;
using System.Text;

using Dotmim.Sync.Enumerations;
using Dotmim.Sync.Filter;
using Dotmim.Sync.Messages;
using System.Runtime.Serialization;
using System.Data.Common;

namespace Dotmim.Sync.Web.Client
{
    [DataContract(Name = "changesres"), Serializable]
    public class HttpMessageSendChangesResponse
    {

        public HttpMessageSendChangesResponse()
        {

        }

        public HttpMessageSendChangesResponse(SyncContext context) 
            => this.SyncContext = context ?? throw new ArgumentNullException(nameof(context));

        /// <summary>
        /// Gets or Sets the Server HttpStep
        /// </summary>
        [DataMember(Name = "ss", IsRequired = true, Order = 1)]

        public HttpStep ServerStep { get; set; }

        /// <summary>
        /// Gets or Sets the SyncContext
        /// </summary>
        [DataMember(Name = "sc", IsRequired = true, Order = 2)]
        public SyncContext SyncContext { get; set; }

        /// <summary>
        /// Gets the current batch index, send from the server 
        /// </summary>
        [DataMember(Name = "bi", IsRequired = true, Order = 3)]
        public int BatchIndex { get; set; }

        /// <summary>
        /// Gets or Sets if this is the last Batch send from the server 
        /// </summary>
        [DataMember(Name = "islb", IsRequired = true, Order = 4)]
        public bool IsLastBatch { get; set; }

        /// <summary>
        /// The remote client timestamp generated by the server database
        /// </summary>
        [DataMember(Name = "rct", IsRequired = true, Order = 5)]
        public long RemoteClientTimestamp { get; set; }

        /// <summary>
        /// Gets the BatchParInfo send from the server 
        /// </summary>
        [DataMember(Name = "changes", IsRequired = true, Order = 6)]
        public ContainerSet Changes { get; set; }

        /// <summary>
        /// Gets the changes applied stats from the server
        /// </summary>
        [DataMember(Name = "cs", IsRequired = true, Order = 7)]
        public DatabaseChangesSelected ChangesSelected { get; set; }

        /// <summary>
        /// Gets or Sets the conflict resolution policy from the server
        /// </summary>

        [DataMember(Name = "policy", IsRequired = true, Order = 8)]
        public ConflictResolutionPolicy ConflictResolutionPolicy { get; set; }


    }

    [DataContract(Name = "morechangesreq"), Serializable]
    public class HttpMessageGetMoreChangesRequest
    {
        public HttpMessageGetMoreChangesRequest()
        {

        }

        public HttpMessageGetMoreChangesRequest(SyncContext context, int batchIndexRequested)
        {
            this.BatchIndexRequested = batchIndexRequested;
            this.SyncContext = context ?? throw new ArgumentNullException(nameof(context));

        }
        [DataMember(Name = "sc", IsRequired = true, Order = 1)]
        public SyncContext SyncContext { get; set; }

        [DataMember(Name = "bireq", IsRequired = true, Order = 2)]
        public int BatchIndexRequested { get; set; }
    }

    [DataContract(Name = "changesreq"), Serializable]
    public class HttpMessageSendChangesRequest
    {
        public HttpMessageSendChangesRequest()
        {

        }

        public HttpMessageSendChangesRequest(SyncContext context, ScopeInfo scope)
        {
            this.SyncContext = context ?? throw new ArgumentNullException(nameof(context));
            this.Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        }

        [DataMember(Name = "sc", IsRequired = true, Order = 1)]
        public SyncContext SyncContext { get; set; }

        /// <summary>
        /// Gets or Sets the reference scope for local repository, stored on server
        /// </summary>
        [DataMember(Name = "scope", IsRequired = true, Order = 2)]
        public ScopeInfo Scope { get; set; }

        /// <summary>
        /// Get the current batch index (if InMemory == false)
        /// </summary>
        [DataMember(Name = "bi", IsRequired = true, Order = 3)]
        public int BatchIndex { get; set; }

        /// <summary>
        /// Gets or Sets if this is the last Batch to sent to server 
        /// </summary>
        [DataMember(Name = "islb", IsRequired = true, Order = 4)]
        public bool IsLastBatch { get; set; }

        /// <summary>
        /// Changes to send
        /// </summary>
        [DataMember(Name = "changes", IsRequired = true, Order = 5)]
        public ContainerSet Changes { get; set; }
    }

    [DataContract(Name = "ensureres"), Serializable]
    public class HttpMessageEnsureScopesResponse
    {
        public HttpMessageEnsureScopesResponse()
        {

        }
        public HttpMessageEnsureScopesResponse(SyncContext context, SyncSet schema)
        {
            this.SyncContext = context ?? throw new ArgumentNullException(nameof(context));
            this.Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }

        [DataMember(Name = "sc", IsRequired = true, Order = 1)]
        public SyncContext SyncContext { get; set; }

        /// <summary>
        /// Gets or Sets the schema option (without schema itself, that is not serializable)
        /// </summary>
        [DataMember(Name = "schema", IsRequired = true, Order = 2)]
        public SyncSet Schema { get; set; }
    }

    [DataContract(Name = "ensurereq"), Serializable]
    public class HttpMessageEnsureScopesRequest
    {
        public HttpMessageEnsureScopesRequest() { }

        /// <summary>
        /// Create a new message to web remote server.
        /// Scope info table name is not provided since we do not care about it on the server side
        /// </summary>
        public HttpMessageEnsureScopesRequest(SyncContext context, string scopeName)
        {
            this.SyncContext = context ?? throw new ArgumentNullException(nameof(context));
            this.ScopeName = scopeName ?? throw new ArgumentNullException(nameof(scopeName));
        }

        [DataMember(Name = "sc", IsRequired = true, Order = 1)]
        public SyncContext SyncContext { get; set; }

        /// <summary>
        /// Gets or Sets the scope name
        /// </summary>
        [DataMember(Name = "scopename", IsRequired = true, Order = 2)]
        public string ScopeName { get; set; }
    }
}
