﻿using System;
using System.Activities;
using System.Activities.Statements;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Markup;
using UiPath.Database.Activities.Properties;

namespace UiPath.Database.Activities
{
    public class DatabaseTransaction : AsyncTaskCodeActivity
    {
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [LocalizedDisplayName(nameof(Resources.ProviderNameDisplayName))]
        public InArgument<string> ProviderName { get; set; }

        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [DefaultValue(null)]
        [LocalizedDisplayName(nameof(Resources.ConnectionStringDisplayName))]
        public InArgument<string> ConnectionString { get; set; }


        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [LocalizedDisplayName(nameof(Resources.ConnectionSecureStringDisplayName))]
        public InArgument<SecureString> ConnectionSecureString { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [LocalizedDisplayName(nameof(Resources.ExistingDbConnectionDisplayName))]
        public InArgument<DatabaseConnection> ExistingDbConnection { get; set; }

        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.ContinueOnErrorDisplayName))]
        public InArgument<bool> ContinueOnError { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.DatabaseConnectionDisplayName))]
        public OutArgument<DatabaseConnection> DatabaseConnection { get; set; }

        [Browsable(false)]
        public System.Activities.Activity Body { get; set; }

        [LocalizedDisplayName(nameof(Resources.UseTransactionDisplayName))]
        public bool UseTransaction { get; set; }



        public DatabaseTransaction()
        {
            UseTransaction = true;
            Body = new Sequence
            {
                DisplayName = "Do"
            };
        }

        private void HandleException(Exception ex, bool continueOnError)
        {
            if (continueOnError) return;
            throw ex;
        }

        protected async override Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            var connString = ConnectionString.Get(context);
            SecureString connSecureString = null;
            var provName = ProviderName.Get(context);
            connSecureString = ConnectionSecureString.Get(context);
            DatabaseConnection existingConnection = null;
            existingConnection = ExistingDbConnection.Get(context);
            DatabaseConnection dbConnection = null;
            var continueOnError = ContinueOnError.Get(context);
            try
            {
                ConnectionHelper.ConnectionValidation(existingConnection, connSecureString, connString, provName);
                dbConnection = await Task.Run(() => existingConnection ?? new DatabaseConnection().Initialize(connString ?? new NetworkCredential("", connSecureString).Password, provName));
                if (UseTransaction)
                {
                    dbConnection.BeginTransaction();
                }
            }
            catch (Exception e)
            {
                Trace.TraceError($"{e}");
                HandleException(e, continueOnError);
            }
            return asyncCodeActivityContext =>
            {
                DatabaseConnection.Set(asyncCodeActivityContext, dbConnection);
            };
        }
    }
}