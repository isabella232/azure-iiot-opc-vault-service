// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.IIoT.OpcUa.Api.Vault;
using Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models;
using Opc.Ua.Gds.Server.OpcVault;

namespace Opc.Ua.Gds.Server.Database.OpcVault
{
    public class OpcVaultApplicationsDatabase : ApplicationsDatabaseBase
    {
        private IOpcVault _opcVaultServiceClient { get; }

        public OpcVaultApplicationsDatabase(IOpcVault OpcVaultServiceClient)
        {
            this._opcVaultServiceClient = OpcVaultServiceClient;
        }

        #region IApplicationsDatabase 
        public override void Initialize()
        {
        }

        public override NodeId RegisterApplication(
                ApplicationRecordDataType application
                )
        {
            bool isUpdate = true;
            string applicationId = null;
            // TODO: remove workaround if base nuget is fixed. Fill NA for servers without caps.
            if (application.ApplicationType != Opc.Ua.ApplicationType.Client &&
                (application.ServerCapabilities == null || application.ServerCapabilities.Count == 0))
            {
                application.ServerCapabilities = new StringCollection() { "NA" };
            }
            NodeId appNodeId = base.RegisterApplication(application);
            if (NodeId.IsNull(appNodeId))
            {
                isUpdate = false;
            }
            else
            {
                applicationId = OpcVaultClientHelper.GetServiceIdFromNodeId(appNodeId, NamespaceIndex);
            }

            string capabilities = base.ServerCapabilities(application);

            ApplicationRecordApiModel applicationModel = new ApplicationRecordApiModel(
                ApplicationState.New,
                (Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models.ApplicationType)application.ApplicationType,
                applicationId
                )
            {
                ApplicationUri = application.ApplicationUri,
                ApplicationName = application.ApplicationNames[0].Text,
                //ApplicationType = (ApplicationType)application.ApplicationType,
                ProductUri = application.ProductUri,
                ServerCapabilities = capabilities
            };

            if (application.DiscoveryUrls != null)
            {
                applicationModel.DiscoveryUrls = application.DiscoveryUrls.ToArray();
            }

            if (application.ApplicationNames != null && application.ApplicationNames.Count > 0)
            {
                var applicationNames = new List<ApplicationNameApiModel>();
                foreach (var applicationName in application.ApplicationNames)
                {
                    applicationNames.Add(new ApplicationNameApiModel()
                    {
                        Locale = applicationName.Locale,
                        Text = applicationName.Text
                    });
                }
                applicationModel.ApplicationNames = applicationNames.ToArray();
            }

            if (isUpdate)
            {
                applicationModel = _opcVaultServiceClient.UpdateApplication(applicationId, applicationModel);
            }
            else
            {
                applicationModel = _opcVaultServiceClient.RegisterApplication(applicationModel);
                applicationId = applicationModel.ApplicationId;
            }

            return OpcVaultClientHelper.GetNodeIdFromServiceId(applicationId, NamespaceIndex);
        }


        public override void UnregisterApplication(NodeId applicationId)
        {
            base.UnregisterApplication(applicationId);

            string id = OpcVaultClientHelper.GetServiceIdFromNodeId(applicationId, NamespaceIndex);

            try
            {
                _opcVaultServiceClient.UnregisterApplication(id);
            }
            catch
            {
                throw new ArgumentException("A record with the specified application id does not exist.", nameof(applicationId));
            }
        }

        public override ApplicationRecordDataType GetApplication(
            NodeId applicationId
            )
        {
            base.GetApplication(applicationId);
            string id = OpcVaultClientHelper.GetServiceIdFromNodeId(applicationId, NamespaceIndex);
            ApplicationRecordApiModel result;

            try
            {
                result = _opcVaultServiceClient.GetApplication(id);
            }
            catch
            {
                return null;
            }

            var names = new List<LocalizedText>();
            foreach (var applicationName in result.ApplicationNames)
            {
                names.Add(new LocalizedText(applicationName.Locale, applicationName.Text));
            }

            StringCollection discoveryUrls = null;

            var endpoints = result.DiscoveryUrls;
            if (endpoints != null)
            {
                discoveryUrls = new StringCollection();

                foreach (var endpoint in endpoints)
                {
                    discoveryUrls.Add(endpoint);
                }
            }

            var capabilities = new StringCollection();
            if (!String.IsNullOrWhiteSpace(result.ServerCapabilities))
            {
                capabilities.AddRange(result.ServerCapabilities.Split(','));
            }

            NodeId appNodeId = OpcVaultClientHelper.GetNodeIdFromServiceId(result.ApplicationId, NamespaceIndex);
            return new ApplicationRecordDataType()
            {
                ApplicationId = appNodeId,
                ApplicationUri = result.ApplicationUri,
                ApplicationType = (ApplicationType)result.ApplicationType,
                ApplicationNames = new LocalizedTextCollection(names),
                ProductUri = result.ProductUri,
                DiscoveryUrls = discoveryUrls,
                ServerCapabilities = capabilities
            };
        }

        public override ApplicationRecordDataType[] FindApplications(
            string applicationUri
            )
        {
            base.FindApplications(applicationUri);
            List<ApplicationRecordDataType> records = new List<ApplicationRecordDataType>();
            string nextPageLink = null;
            do
            {
                IList<ApplicationRecordApiModel> results;
                var pagedResults = _opcVaultServiceClient.ListApplications(applicationUri, nextPageLink);
                results = pagedResults.Applications;

                foreach (var result in results)
                {
                    LocalizedText[] names = null;

                    if (result.ApplicationName != null)
                    {
                        names = new LocalizedText[] { result.ApplicationName };
                    }

                    StringCollection discoveryUrls = null;

                    var endpoints = result.DiscoveryUrls;
                    if (endpoints != null)
                    {
                        discoveryUrls = new StringCollection();

                        foreach (var endpoint in endpoints)
                        {
                            discoveryUrls.Add(endpoint);
                        }
                    }

                    string[] capabilities = null;

                    if (result.ServerCapabilities != null)
                    {
                        capabilities = result.ServerCapabilities.Split(',');
                    }

                    NodeId appNodeId = OpcVaultClientHelper.GetNodeIdFromServiceId(result.ApplicationId, NamespaceIndex);

                    records.Add(new ApplicationRecordDataType()
                    {
                        ApplicationId = appNodeId,
                        ApplicationUri = result.ApplicationUri,
                        ApplicationType = (ApplicationType)result.ApplicationType,
                        ApplicationNames = new LocalizedTextCollection(names),
                        ProductUri = result.ProductUri,
                        DiscoveryUrls = discoveryUrls,
                        ServerCapabilities = capabilities
                    });
                }
                nextPageLink = pagedResults.NextPageLink;
            } while (nextPageLink != null);
            return records.ToArray();
        }

        public override ServerOnNetwork[] QueryServers(
            uint startingRecordId,
            uint maxRecordsToReturn,
            string applicationName,
            string applicationUri,
            string productUri,
            string[] serverCapabilities,
            out DateTime lastCounterResetTime)
        {
            lastCounterResetTime = DateTime.MinValue;
            List<ServerOnNetwork> records = new List<ServerOnNetwork>();

            var query = new QueryApplicationsByIdApiModel(
                (int)startingRecordId,
                (int)maxRecordsToReturn,
                applicationName,
                applicationUri,
                QueryApplicationType.Server,
                productUri,
                serverCapabilities?.ToList()
                );
            var resultModel = _opcVaultServiceClient.QueryApplicationsById(query);

            foreach (var application in resultModel.Applications)
            {
                if (application.DiscoveryUrls != null)
                {
                    foreach (var discoveryUrl in application.DiscoveryUrls)
                    {
                        records.Add(new ServerOnNetwork()
                        {
                            RecordId = (uint)application.Id,
                            ServerName = application.ApplicationName,
                            DiscoveryUrl = discoveryUrl,
                            ServerCapabilities = application.ServerCapabilities?.Split(",")
                        });
                    }
                }
            }

            lastCounterResetTime = resultModel.LastCounterResetTime != null ? resultModel.LastCounterResetTime : DateTime.MinValue;

            return records.ToArray();
        }

        public override ApplicationDescription[] QueryApplications(
            uint startingRecordId,
            uint maxRecordsToReturn,
            string applicationName,
            string applicationUri,
            uint applicationType,
            string productUri,
            string[] serverCapabilities,
            out DateTime lastCounterResetTime,
            out uint nextRecordId)
        {
            lastCounterResetTime = DateTime.MinValue;
            var records = new List<ApplicationDescription>();

            var query = new QueryApplicationsByIdApiModel(
                (int)startingRecordId,
                (int)maxRecordsToReturn,
                applicationName,
                applicationUri,
                (QueryApplicationType)applicationType,
                productUri,
                serverCapabilities?.ToList()
                );
            var resultModel = _opcVaultServiceClient.QueryApplicationsById(query);

            foreach (var application in resultModel.Applications)
            {
                records.Add(new ApplicationDescription()
                {
                    ApplicationUri = application.ApplicationUri,
                    ProductUri = application.ProductUri,
                    ApplicationName = application.ApplicationName,
                    ApplicationType = (ApplicationType)application.ApplicationType,
                    GatewayServerUri = application.GatewayServerUri,
                    DiscoveryProfileUri = application.DiscoveryProfileUri,
                    DiscoveryUrls = application.DiscoveryUrls != null ? new StringCollection(application.DiscoveryUrls) : null
                });
            }

            lastCounterResetTime = resultModel.LastCounterResetTime;
            nextRecordId = (uint)resultModel.NextRecordId;
            return records.ToArray();
        }

        #endregion
    }
}
