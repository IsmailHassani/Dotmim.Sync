﻿
using System;
using System.Collections.Generic;
using System.Text;

namespace Dotmim.Sync.Builders
{
    public enum DbCommandType 
    {
        SelectChanges,
        SelectInitializedChanges,
        SelectInitializedChangesWithFilters,
        SelectChangesWithFilters,
        SelectRow,
        UpdateRow,
        InsertRow,
        DeleteRow,
        DisableConstraints,
        EnableConstraints,
        DeleteMetadata,
        UpdateMetadata,
        InsertTrigger,
        UpdateTrigger,
        DeleteTrigger,
        UpdateRows,
        InsertRows,
        DeleteRows,
        BulkTableType,
        UpdateUntrackedRows,
        Reset
    }
}
