﻿// Copyright(c) 2017 Google Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not
// use this file except in compliance with the License. You may obtain a copy of
// the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
// License for the specific language governing permissions and limitations under
// the License.

using CommandLine;
using Google.Cloud.VideoIntelligence.V1Beta1;
using System;
using System.IO;
using System.Linq;

namespace GoogleCloudSamples.VideoIntelligence
{
    [Verb("labels", HelpText = "Print a list of labels found in the video.")]
    class AnalyzeLabelsOptions
    {
        [Value(0, HelpText = "The uri of the video to examine. "
            + "Can be path to a local file or a Cloud storage uri like "
            + "gs://bucket/object.",
            Required = true)]
        public string Uri { get; set; }
    }

    [Verb("shots", HelpText = "Print a list shot changes.")]
    class AnalyzeShotsOptions
    {
        [Value(0, HelpText = "The uri of the video to examine. "
            + "Must be a Cloud storage uri like "
            + "gs://bucket/object.",
            Required = true)]
        public string Uri { get; set; }
    }

    [Verb("safesearch", HelpText = "Analyze the content of the video.")]
    class AnalyzeSafeSearchOptions
    {
        [Value(0, HelpText = "The uri of the video to examine. "
            + "Must be a Cloud storage uri like "
            + "gs://bucket/object.",
            Required = true)]
        public string Uri { get; set; }
    }

    public class Analyzer
    {
        // [START analyze_shots_gcs]
        public static object AnalyzeShotsGcs(string uri)
        {
            var client = VideoIntelligenceServiceClient.Create();
            var request = new AnnotateVideoRequest()
            {
                InputUri = uri,
                Features = { Feature.ShotChangeDetection }
            };
            var op = client.AnnotateVideo(request).PollUntilCompleted();
            foreach (var result in op.Result.AnnotationResults)
            {
                foreach (var annotation in result.ShotAnnotations)
                {
                    Console.Out.WriteLine("Start Time Offset: {0}\tEnd Time Offset: {1}",
                        annotation.StartTimeOffset, annotation.EndTimeOffset);
                }
            }
            return 0;
        }
        // [END analyze_shots_gcs]

        // [START analyze_labels]
        public static object AnalyzeLabels(string path)
        {
            var client = VideoIntelligenceServiceClient.Create();
            var request = new AnnotateVideoRequest()
            {
                InputContent = Convert.ToBase64String(File.ReadAllBytes(path)),
                Features = { Feature.LabelDetection }
            };
            var op = client.AnnotateVideo(request).PollUntilCompleted();
            foreach (var result in op.Result.AnnotationResults)
            {
                foreach (var annotation in result.LabelAnnotations)
                {
                    Console.WriteLine(annotation.Description);
                }
            }
            return 0;
        }
        // [END analyze_labels]

        // [START analyze_labels_gcs]
        public static object AnalyzeLabelsGcs(string uri)
        {
            var client = VideoIntelligenceServiceClient.Create();
            var request = new AnnotateVideoRequest()
            {
                InputUri = uri,
                Features = { Feature.LabelDetection }
            };
            var op = client.AnnotateVideo(request).PollUntilCompleted();
            foreach (var result in op.Result.AnnotationResults)
            {
                foreach (var annotation in result.LabelAnnotations)
                {
                    Console.WriteLine(annotation.Description);
                }
            }
            return 0;
        }
        // [END analyze_labels_gcs]

        // [START analyze_safesearch_gcs]
        public static object AnalyzeSafeSearchGcs(string uri)
        {
            var client = VideoIntelligenceServiceClient.Create();
            var request = new AnnotateVideoRequest()
            {
                InputUri = uri,
                Features = { Feature.SafeSearchDetection }
            };
            var op = client.AnnotateVideo(request).PollUntilCompleted();
            foreach (var result in op.Result.AnnotationResults)
            {
                foreach (SafeSearchAnnotation annotation in result.SafeSearchAnnotations)
                {
                    Console.WriteLine("Time Offset: {0}", annotation.TimeOffset);
                    Console.WriteLine("Adult: {0}", annotation.Adult);
                    Console.WriteLine("Medical: {0}", annotation.Medical);
                    Console.WriteLine("Racy: {0}", annotation.Racy);
                    Console.WriteLine("Spoof: {0}", annotation.Spoof);
                    Console.WriteLine("Violent: {0}", annotation.Violent);
                    Console.WriteLine();
                }
            }
            return 0;
        }
        // [END analyze_safesearch_gcs]

        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<
                AnalyzeShotsOptions,
                AnalyzeSafeSearchOptions,
                AnalyzeLabelsOptions
                >(args).MapResult(
                (AnalyzeShotsOptions opts) => AnalyzeShotsGcs(opts.Uri),
                (AnalyzeSafeSearchOptions opts) => AnalyzeSafeSearchGcs(opts.Uri),
                (AnalyzeLabelsOptions opts) => IsStorageUri(opts.Uri) ? AnalyzeLabelsGcs(opts.Uri) : AnalyzeLabels(opts.Uri),
                errs => 1);
        }
        static bool IsStorageUri(string s) => s.Substring(0, 4).ToLower() == "gs:/";
    }
}
