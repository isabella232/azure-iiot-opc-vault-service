// <auto-generated>
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//
// Code generated by Microsoft (R) AutoRest Code Generator 1.0.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models
{
    using Newtonsoft.Json;
    using System.Linq;

    public partial class TrustListApiModel
    {
        /// <summary>
        /// Initializes a new instance of the TrustListApiModel class.
        /// </summary>
        public TrustListApiModel()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the TrustListApiModel class.
        /// </summary>
        public TrustListApiModel(string id = default(string), X509Certificate2CollectionApiModel issuerCertificates = default(X509Certificate2CollectionApiModel), X509CrlCollectionApiModel issuerCrls = default(X509CrlCollectionApiModel), X509Certificate2CollectionApiModel trustedCertificates = default(X509Certificate2CollectionApiModel), X509CrlCollectionApiModel trustedCrls = default(X509CrlCollectionApiModel))
        {
            Id = id;
            IssuerCertificates = issuerCertificates;
            IssuerCrls = issuerCrls;
            TrustedCertificates = trustedCertificates;
            TrustedCrls = trustedCrls;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Id")]
        public string Id { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "IssuerCertificates")]
        public X509Certificate2CollectionApiModel IssuerCertificates { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "IssuerCrls")]
        public X509CrlCollectionApiModel IssuerCrls { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "TrustedCertificates")]
        public X509Certificate2CollectionApiModel TrustedCertificates { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "TrustedCrls")]
        public X509CrlCollectionApiModel TrustedCrls { get; set; }

    }
}