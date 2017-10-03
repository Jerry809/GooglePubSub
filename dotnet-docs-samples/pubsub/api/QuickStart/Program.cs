// Copyright 2017 Google Inc.
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
// limitations under the License.using System;

// [START pubsub_quickstart]

using Google.Cloud.PubSub.V1;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Api.Gax;
using Google.Protobuf;
using System.Configuration;
using System.IO;
using Grpc.Core;
using Google.Apis.Auth.OAuth2;
using Grpc.Auth;

namespace GoogleCloudSamples
{
    class QuickStart
    {
        private static string _topic = "";
        private static string _project_id = "";
        private static string _sub_id = "";
        private static Channel _channel;

        static void Main(string[] args)
        {
           

            GoogleCredential googleCredential = null;
            //json檔為google cloud platform 下載該服務的Credential
            using (var jsonStream = new FileStream("E-Record-1a8760774549.json", FileMode.Open,
                FileAccess.Read, FileShare.Read))
            {
                googleCredential = GoogleCredential.FromStream(jsonStream)
                    .CreateScoped(PublisherClient.DefaultScopes);
            }

            _channel = new Channel(PublisherClient.DefaultEndpoint.Host,
                PublisherClient.DefaultEndpoint.Port,
                googleCredential.ToChannelCredentials());

            // Instantiates a client
            SubscriberClient subscriberClient = SubscriberClient.Create(_channel);

            Task.Run(() => PullTask("worker"));

            var input = string.Empty;

            do
            {
                Console.WriteLine("Press sss to send messages \r\n" +
                                  "or tt to queueTest \r\n" +
                                  "or q to exit...\r\n");
                input = Console.ReadLine();

                switch (input)
                {
                    case "sss":
                        {
                            for (var i = 0; i < 10; ++i)
                            {
                                var msg = "v" + i;
                                PushTask(msg);
                                Console.Out.WriteLine(
                                    $"[{DateTime.Now:yyyy/MM/dd HH:mm:ss}][publish] Message: {msg}");

                                Thread.Sleep(10);
                            }
                            break;
                        }
                    case "tt":
                        {
                            QueueTest();
                            break;
                        }
                }

            } while (input != "q");
        }

        #region Pub
        private static void PushTask(string message)
        {
            PublisherClient publisher = PublisherClient.Create();
            var topicName = new TopicName(_project_id, _topic);

            var messages = new List<PubsubMessage>()
            {
                new PubsubMessage()
                {
                    Data = ByteString.CopyFromUtf8(message)
                }
            };

            var pushResponse = publisher.PublishAsync(
                new PublishRequest
                {
                    TopicAsTopicName = GaxPreconditions.CheckNotNull(topicName, nameof(topicName)),
                    Messages = { GaxPreconditions.CheckNotNull(messages, nameof(messages)) },
                },
                null);

        }

        private static void QueueTest()
        {
            PushTask("v0");
            PushTask("v1");
        }
        #endregion

        #region Sub
        private static void PullTask(string workerName, SimpleSubscriber.Reply? forceAck = null)
        {
            while (true)
            {
                // Instantiates a client
                SubscriberClient subscriberClient = SubscriberClient.Create();

                // The name for the new topic
                var topicName = new TopicName(_project_id, _topic);
                var subscriptionName = new SubscriptionName(_project_id, _sub_id);

                var getMessageIds = new ConcurrentDictionary<string, string>();

                SimpleSubscriber subscriber = SimpleSubscriber.Create(
                    subscriptionName, new[] { subscriberClient },
                    new SimpleSubscriber.Settings()
                    {
                        AckExtensionWindow = TimeSpan.FromSeconds(4),
                        Scheduler = Google.Api.Gax.SystemScheduler.Instance,
                        StreamAckDeadline = TimeSpan.FromSeconds(10),
                        FlowControlSettings = new Google.Api.Gax
                            .FlowControlSettings(
                                maxOutstandingElementCount: 1,
                                maxOutstandardByteCount: null)
                    });
                // SimpleSubscriber runs your message handle function on multiple
                // threads to maximize throughput.
                Console.WriteLine("subscriber Start");
                subscriber.StartAsync(
                    async (PubsubMessage message, CancellationToken cancel) =>
                    {
                        string text =
                            Encoding.UTF8.GetString(message.Data.ToArray());
                        await Console.Out.WriteLineAsync(
                            $"[{DateTime.Now:yyyy/MM/dd HH:mm:ss}][{workerName}] Message {message.MessageId}: {text}, Status: processing");

                        // Run for 5 seconds.
                        //Thread.Sleep(1 * 1000);
                        var fileName = "Test.txt";
                        string count = File.ReadAllText(fileName);  // 讀取檔案內所有文字
                        File.WriteAllText(fileName, (int.Parse(count) + 1).ToString());


                        var ack = SimpleSubscriber.Reply.Ack; //(SimpleSubscriber.Reply)Enum.Parse(typeof(SimpleSubscriber.Reply), (DateTime.Now.Ticks % 3).ToString());
                        await Console.Out.WriteLineAsync(
                            $"[{DateTime.Now:yyyy/MM/dd HH:mm:ss}][{workerName}] Message {message.MessageId}: {text}, Status: acking ({(int)ack})");

                        //if (forceAck.HasValue)
                        //    ack = forceAck.Value;

                        //return acknowledge ? SimpleSubscriber.Reply.Ack : SimpleSubscriber.Reply.Nack;
                        if (ack == SimpleSubscriber.Reply.Nack || ack == SimpleSubscriber.Reply.Ack)
                            return ack;
                        else
                            throw new Exception("timeout");
                    });
                // Run for 3 seconds.
                Thread.Sleep(120 * 1000);
                subscriber.StopAsync(CancellationToken.None).Wait();
                Console.WriteLine("subscriber Stope");
            }
        }
        #endregion

        /// <summary>
        /// From Google
        /// </summary>
        private static void Example()
        {
            // Instantiates a client
            PublisherClient publisher = PublisherClient.Create();

            // Your Google Cloud Platform project ID
            string projectId = "YOUR-PROJECT-ID";
            // [END pubsub_quickstart]
            Debug.Assert(projectId != "YOUR-PROJECT" + "-ID",
                "Edit Program.cs and replace YOUR-PROJECT-ID with your project id.");
            // [START pubsub_quickstart]

            // The name for the new topic
            var topicName = new TopicName(projectId, "");

            // Creates the new topic
            try
            {
                Topic topic = publisher.CreateTopic(topicName);
                Console.WriteLine($"Topic {topic.Name} created.");
            }
            catch (Grpc.Core.RpcException e)
                when (e.Status.StatusCode == Grpc.Core.StatusCode.AlreadyExists)
            {
                Console.WriteLine($"Topic {topicName} already exists.");
            }
        }

    }
}
// [END pubsub_quickstart]