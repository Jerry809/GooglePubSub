﻿// Copyright 2017, Google Inc. All rights reserved.
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

using System;
using Google.Api.Gax;
using Google.Api.Gax.Grpc;
using Grpc.Core;
// ReSharper disable UnusedParameterInPartialMethod

namespace Google.Cloud.Spanner.V1
{
    public partial class SpannerClient
    {
        /// <summary>
        /// Executes an SQL Query on Spanner returning the results as a set of partialresult streams.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="callSettings"></param>
        /// <returns></returns>
        public virtual AsyncServerStreamingCall<PartialResultSet> ExecuteSqlStream(ExecuteSqlRequest request, CallSettings callSettings = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Executes an SQL Query on Spanner returning the results as a set of partialresult streams.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="session"></param>
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        public virtual ReliableStreamReader GetSqlStreamReader(ExecuteSqlRequest request, Session session, int timeoutSeconds)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the effective <see cref="CallSettings"/> used for each API call.
        /// This can be used to create a modified instance of <see cref="CallSettings"/> to pass
        /// for a single API request.
        /// </summary>
        public virtual SpannerSettings Settings { get; protected set; }
    }

    /// <summary>
    /// Settings for a <see cref="SpannerClient"/>.
    /// </summary>
    public sealed partial class SpannerSettings
    {
        partial void OnCopy(SpannerSettings existing)
        {
            ExecuteSqlStreamSettings = existing.ExecuteSqlStreamSettings;
            AllowImmediateTimeouts = existing.AllowImmediateTimeouts;
        }

        /// <summary>
        /// <see cref="CallSettings"/> for synchronous and asynchronous calls to
        /// <c>SpannerClient.ExecuteSqlStream</c>.
        /// </summary>
        /// <remarks>
        /// The default is an RPC expiration of 60 seconds.
        /// </remarks>
        public CallSettings ExecuteSqlStreamSettings { get; set; } = CallSettings.FromCallTiming(
            CallTiming.FromTimeout(TimeSpan.FromSeconds(60)));

        /// <summary>
        /// If true, then timeouts of '0' converted with <see cref="ConvertTimeoutToExpiration(int)"/>
        /// will result in an <see cref="Expiration"/> with an immediate timeout.
        /// </summary>
        public bool AllowImmediateTimeouts { get; set; } = false;

        /// <summary>
        /// Returns Timeout expressed as an <see cref="Expiration"/> and also accounts for
        /// <see cref="AllowImmediateTimeouts"/>
        /// </summary>
        public Expiration ConvertTimeoutToExpiration(int timeoutSeconds) =>
            ConvertTimeoutToExpiration(timeoutSeconds, AllowImmediateTimeouts);

        /// <summary>
        /// Returns Timeout expressed as an <see cref="Expiration"/> and also accounts for
        /// <see cref="AllowImmediateTimeouts"/>
        /// </summary>
        public static Expiration ConvertTimeoutToExpiration(int timeoutSeconds, bool allowImmediateTimeouts) =>
            timeoutSeconds == 0 && !allowImmediateTimeouts ?  Expiration.None :
                Expiration.FromTimeout(TimeSpan.FromSeconds(timeoutSeconds));
    }

    public partial class SpannerClientImpl
    {
        private ApiServerStreamingCall<ExecuteSqlRequest, PartialResultSet> _callExecuteSqlStream;

        partial void OnConstruction(Spanner.SpannerClient grpcClient, SpannerSettings effectiveSettings, ClientHelper clientHelper)
        {
            Settings = effectiveSettings;
            _callExecuteSqlStream = clientHelper.BuildApiCall<ExecuteSqlRequest, PartialResultSet>(
                GrpcClient.ExecuteStreamingSql, Settings.ExecuteSqlStreamSettings);
        }

        /// <summary>
        /// Executes an SQL Query on Spanner returning the results as a set of partialresult streams.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="callSettings"></param>
        /// <returns></returns>
        public override AsyncServerStreamingCall<PartialResultSet> ExecuteSqlStream(ExecuteSqlRequest request, CallSettings callSettings = null)
        {
            Modify_ExecuteSqlRequest(ref request, ref callSettings);
            return _callExecuteSqlStream.Call(request, callSettings);
        }

        /// <summary>
        /// Executes an SQL Query on Spanner returning the results as a set of partialresult streams.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="session"></param>
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        public override ReliableStreamReader GetSqlStreamReader(ExecuteSqlRequest request, Session session, int timeoutSeconds)
        {
            return new ReliableStreamReader(this, request, session, timeoutSeconds);
        }
    }
}
