﻿// Copyright 2017 Google Inc. All Rights Reserved.
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

using Google.Api.Gax;

namespace Google.Cloud.Diagnostics.Common
{
    /// <summary>
    /// Functions to create <see cref="IContextExceptionLogger"/>s.
    /// </summary>
    public static class ContextExceptionLogger
    {
        /// <summary>
        /// Creates an <see cref="IContextExceptionLogger"/>.
        /// </summary>
        /// <param name="projectId">The Google Cloud Platform project ID.  If unspecified and running
        /// on GAE or GCE the project ID will be detected from the platform.</param>
        /// <param name="serviceName"> An identifier of the service, such as the name of the executable or job. Cannot be null.</param>
        /// <param name="version">Represents the source code version that the developer provided. Cannot be null.</param>
        /// <param name="options">The error reporting options. Can be null, if null default options will be used.</param>
        /// <returns>An <see cref="IContextExceptionLogger"/> for the given options.</returns>
        public static IContextExceptionLogger Create(string projectId, string serviceName,
            string version, ErrorReportingOptions options)
        {
            GaxPreconditions.CheckNotNull(serviceName, nameof(serviceName));
            GaxPreconditions.CheckNotNull(version, nameof(version));
            return ErrorReportingContextExceptionLogger.Create(projectId, serviceName, version, options);
        }
    }
}
