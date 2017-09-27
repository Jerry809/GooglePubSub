// Copyright 2015 Google Inc. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Google.Apis.Download;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;

namespace Google.Cloud.Storage.V1
{
    /// <summary>
    /// Options for <c>DownloadObject</c> operations.
    /// </summary>
    public sealed class DownloadObjectOptions
    {
        /// <summary>
        /// The chunk size to use for each request.
        /// </summary>
        public int? ChunkSize { get; set; }

        /// <summary>
        /// The generation to download. When not specified, the latest version
        /// is always downloaded.
        /// </summary>
        public long? Generation { get; set; }

        /// <summary>
        /// Precondition for download: the object is only downloaded if its generation matches the given value.
        /// </summary>
        public long? IfGenerationMatch { get; set; }

        /// <summary>
        /// Precondition for download: the object is only downloaded if its generation does not match the given value.
        /// </summary>
        public long? IfGenerationNotMatch { get; set; }

        /// <summary>
        /// Precondition for download: the object is only downloaded if its meta-generation matches the given value.
        /// </summary>
        public long? IfMetagenerationMatch { get; set; }

        /// <summary>
        /// Precondition for download: the object is only downloaded if its meta-generation does not match the given value.
        /// </summary>
        public long? IfMetagenerationNotMatch { get; set; }

        /// <summary>
        /// If non-null, limits the range of bytes of the object to download.
        /// </summary>
        public RangeHeaderValue Range { get; set; }

        /// <summary>
        /// The encryption key to use for this operation. If this property is null, the <see cref="StorageClient.EncryptionKey"/>
        /// will be used instead. Use <see cref="EncryptionKey.None"/> to remove encryption headers from this request.
        /// </summary>
        public EncryptionKey EncryptionKey { get; set; }

        /// <summary>
        /// If set, this is the ID of the project which will be billed for the request, for requester-pays buckets.
        /// The caller must have suitable permissions for the project being billed.
        /// </summary>
        public string UserProject { get; set; }

        internal void ModifyDownloader(MediaDownloader downloader)
        {
            if (ChunkSize != null)
            {
                downloader.ChunkSize = ChunkSize.Value;
            }
            if (Range != null)
            {
                downloader.Range = Range;
            }
        }

        /// <summary>
        /// Returns the URI to use for a download request, appending any options specified by this object.
        /// </summary>
        /// <param name="baseUri">Base URI which may end with a query parameter.</param>
        /// <returns>The URI including the specified options.</returns>
        internal string GetUri(string baseUri)
        {
            // Note the use of ArgumentException here, as this will basically be the result of invalid
            // options being passed to a public method.
            if (IfGenerationMatch != null && IfGenerationNotMatch != null)
            {
                throw new ArgumentException($"Cannot specify {nameof(IfGenerationMatch)} and {nameof(IfGenerationNotMatch)} in the same options", "options");
            }
            if (IfMetagenerationMatch != null && IfMetagenerationNotMatch != null)
            {
                throw new ArgumentException($"Cannot specify {nameof(IfMetagenerationMatch)} and {nameof(IfMetagenerationNotMatch)} in the same options", "options");
            }

            StringBuilder queryBuilder = new StringBuilder();
            MaybeAppendParameter(queryBuilder, "generation", Generation);
            MaybeAppendParameter(queryBuilder, "ifGenerationMatch", IfGenerationMatch);
            MaybeAppendParameter(queryBuilder, "ifGenerationNotMatch", IfGenerationNotMatch);
            MaybeAppendParameter(queryBuilder, "ifMetagenerationMatch", IfMetagenerationMatch);
            MaybeAppendParameter(queryBuilder, "ifMetagenerationNotMatch", IfMetagenerationNotMatch);
            MaybeAppendParameter(queryBuilder, "userProject", UserProject);

            // If we haven't appended any parameters, we don't need to do anything else.
            if (queryBuilder.Length == 0)
            {
                return baseUri;
            }

            // Otherwise, we might need to change the leading & in the builder to a ?
            bool alreadyHasQuery = !string.IsNullOrEmpty(new Uri(baseUri).Query);
            if (!alreadyHasQuery)
            {
                queryBuilder[0] = '?';
            }
            // And then just return the original URI with the parameters
            return baseUri + queryBuilder.ToString();
        }

        private static void MaybeAppendParameter(StringBuilder queryBuilder, string name, long? value)
        {
            if (value != null)
            {
                queryBuilder.AppendFormat(CultureInfo.InvariantCulture, "&{0}={1}", name, value.Value);
            }
        }

        private static void MaybeAppendParameter(StringBuilder queryBuilder, string name, string value)
        {
            if (value != null)
            {
                queryBuilder.AppendFormat("&{0}={1}", name, Uri.EscapeDataString(value));
            }
        }
    }
}
