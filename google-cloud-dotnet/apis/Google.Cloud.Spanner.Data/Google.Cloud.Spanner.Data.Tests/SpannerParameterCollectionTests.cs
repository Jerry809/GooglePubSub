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

using System;
using System.Linq;
using Xunit;

namespace Google.Cloud.Spanner.Data.Tests
{
    public class SpannerParameterCollectionTests
    {
        [Fact]
        public void NullNotAllowed()
        {
            // ReSharper disable once CollectionNeverQueried.Local
            var collection = new SpannerParameterCollection();
            // ReSharper disable once AssignNullToNotNullAttribute
            Assert.Throws<ArgumentNullException>(() => collection.Add(null));
        }

        [Fact]
        public void AddFromArray()
        {
            var parameters = Enumerable.Range(0, 5).Select(
                x => new SpannerParameter($"Param{x}", SpannerDbType.String)).ToArray();

            var collection = new SpannerParameterCollection();
            collection.AddRange(parameters);
            Assert.True(parameters.All(x => collection.Contains(x)));
        }

        [InlineData(0)]
        [InlineData(3)]
        [Theory]
        public void CopyToArrayWithBuffer(int extraBuffer)
        {
            var parameters1 = Enumerable.Range(0, 5).Select(
                x => new SpannerParameter($"Param{x}", SpannerDbType.String)).ToArray();
            var parameters2 = new SpannerParameter[parameters1.Length + extraBuffer];

            var collection = new SpannerParameterCollection();
            collection.AddRange(parameters1);
            collection.CopyTo(parameters2, extraBuffer);
            Assert.True(parameters1.SequenceEqual(parameters2.Skip(extraBuffer)));
        }

        [Fact]
        public void SetIndividualParameterName()
        {
            var parameters = Enumerable.Range(0, 5).Select(
                x => new SpannerParameter($"Param{x}", SpannerDbType.String)).ToArray();

            var collection = new SpannerParameterCollection();
            collection.AddRange(parameters);

            var newParameter = new SpannerParameter("Param4", SpannerDbType.Float64);
            collection[newParameter.ParameterName] = newParameter;

            Assert.True(collection.Contains(newParameter));
            Assert.False(collection.Contains(parameters[4]));
        }

        [Fact]
        public void RemoveName()
        {
            var parameters = Enumerable.Range(0, 5).Select(
                x => new SpannerParameter($"Param{x}", SpannerDbType.String)).ToArray();

            var collection = new SpannerParameterCollection();
            collection.AddRange(parameters);

            collection.RemoveAt(parameters[4].ParameterName);
            Assert.False(collection.Contains(parameters[4]));
        }

        [Fact]
        public void RemoveAt()
        {
            var parameters = Enumerable.Range(0, 5).Select(
                x => new SpannerParameter($"Param{x}", SpannerDbType.String)).ToArray();

            var collection = new SpannerParameterCollection();
            collection.AddRange(parameters);

            collection.RemoveAt(4);
            Assert.False(collection.Contains(parameters[4]));
        }

#if !NETCOREAPP1_0
        [Fact]
        public void AssertConsts()
        {
            // ReSharper disable once CollectionNeverUpdated.Local
            var collection = new SpannerParameterCollection();
            Assert.False(collection.IsFixedSize);
            Assert.False(collection.IsSynchronized);
            Assert.False(collection.IsReadOnly);
        }
#endif
    }
}
