using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Api.Gax;
using Google.Api.Gax.Grpc;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Google.Protobuf.Collections;
using GoogleCloudSamples;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest
{
    [TestClass]
    public class UnitTest1
    {

        [TestMethod]
        public void CreateTopic_ResultNoException()
        {
            // Instantiates a client
            PublisherClient publisher = PublisherClient.Create();

            // Your Google Cloud Platform project ID
            string projectId = "";

            // The name for the new topic
            var topicName = new TopicName(projectId, "");

            // Creates the new topic
            try
            {
                Topic topic = publisher.CreateTopic(topicName);
                Console.WriteLine($"Topic {topic.Name} created.");
                Assert.Equals(topic.TopicName, topicName);
            }
            catch (Grpc.Core.RpcException e)
                when (e.Status.StatusCode == Grpc.Core.StatusCode.AlreadyExists)
            {
                Console.WriteLine($"Topic {topicName} already exists.");
                Assert.Fail();
            }
        }

        [TestMethod]
        public void CreateTopic_ResultAlreadyExistsException()
        {
            // Instantiates a client
            PublisherClient publisher = PublisherClient.Create();

            // Your Google Cloud Platform project ID
            string projectId = "";

            // The name for the new topic
            var topicName = new TopicName(projectId, "");

            // Creates the new topic
            try
            {
                Topic topic = publisher.CreateTopic(topicName);
                Console.WriteLine($"Topic {topic.Name} created.");
                Assert.Equals(topic.TopicName, topicName);
            }
            catch (Grpc.Core.RpcException e)
                when (e.Status.StatusCode == Grpc.Core.StatusCode.AlreadyExists)
            {
                Console.WriteLine($"Topic {topicName} already exists.");
                Assert.Equals(e.Status.StatusCode, Grpc.Core.StatusCode.AlreadyExists);
            }
        }

        [TestMethod]
        public void TestListTopics()
        {
            // Instantiates a client
            SubscriberClient subscriber = SubscriberClient.Create();

            // Your Google Cloud Platform project ID
            string projectId = "";

            // The name for the new topic
            var topicName = new TopicName(projectId, "");

            // Creates the new topic
            try
            {
                ProjectName projectName = new ProjectName(projectId);
                IEnumerable<Subscription> subscriptions = subscriber.ListSubscriptions(projectName);
                foreach (Subscription subscription in subscriptions)
                {
                    Console.WriteLine($"{subscription}");
                }
                Assert.IsTrue(subscriptions.Count() > 0);
            }
            catch (Exception e)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void TestPullMessage_ResultCountGreaterThanOne()
        {
            // Instantiates a client
            PublisherClient publisher = PublisherClient.Create();

            // Instantiates a client
            SubscriberClient subscriber = SubscriberClient.Create();

            // Your Google Cloud Platform project ID
            string projectId = "";

            string randomName = TestUtil.RandomName();

            // The name for the new topic
            var topicName = new TopicName(projectId, "");
            var subscriptionName = new SubscriptionName(projectId, "");

            // Creates the new topic
            try
            {
                var messages = new List<PubsubMessage>()
                {
                    new PubsubMessage()
                    {
                        Data = ByteString.CopyFromUtf8(randomName)
                    }
                };

                var pushResponse = publisher.Publish(
                    new PublishRequest
                    {
                        TopicAsTopicName = GaxPreconditions.CheckNotNull(topicName, nameof(topicName)),
                        Messages = { GaxPreconditions.CheckNotNull(messages, nameof(messages)) },
                    },
                    null);

                Thread.Sleep(10 * 1000);

                while (true)
                {
                    var pullResponse = subscriber.Pull(
                        new PullRequest()
                        {
                            SubscriptionAsSubscriptionName = subscriptionName,
                            ReturnImmediately = false, // Optional
                            MaxMessages = 1
                        });

                    if (pullResponse.ReceivedMessages.Count == 0) break;

                    var ackIds = pullResponse.ReceivedMessages
                        .ToList()
                        .Select(x => x.AckId);

                    subscriber.Acknowledge(
                        new AcknowledgeRequest()
                        {
                            SubscriptionAsSubscriptionName = subscriptionName,
                            AckIds = { GaxPreconditions.CheckNotNull(ackIds, nameof(ackIds)) },
                        }
                        );

                    Thread.Sleep(1000);

                    Console.WriteLine(pullResponse);
                }

            }
            catch (Exception e)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void TestMessages_ResultDuplicateFree()
        {
            var autoAck = false;
            // Instantiates a client
            SubscriberClient subscriberClient = SubscriberClient.Create();

            // Your Google Cloud Platform project ID
            string projectId = "";

            // The name for the new topic
            var topicName = new TopicName(projectId, "");
            var subscriptionName = new SubscriptionName(projectId, "");

            var Task1 = Task.Run(() =>
            {
                PullTask(autoAck);
            });

            var Task2 = Task.Run(() =>
            {
                PullTask(autoAck);
            });

            while (true)
            {
                Thread.Sleep(3000);
            }
        }

        private void PullTask(bool acknowledge)
        {
            // Instantiates a client
            SubscriberClient subscriberClient = SubscriberClient.Create();

            // Your Google Cloud Platform project ID
            string projectId = "";

            // The name for the new topic
            var topicName = new TopicName(projectId, "");
            var subscriptionName = new SubscriptionName(projectId, "");

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
                            maxOutstandingElementCount: 100,
                            maxOutstandardByteCount: 10240)
                });
            // SimpleSubscriber runs your message handle function on multiple
            // threads to maximize throughput.
            subscriber.StartAsync(
                async (PubsubMessage message, CancellationToken cancel) =>
                {
                    string text =
                        Encoding.UTF8.GetString(message.Data.ToArray());
                    await Console.Out.WriteLineAsync(
                        $"Message {message.MessageId}: {text}");
                    return acknowledge ? SimpleSubscriber.Reply.Ack
                        : SimpleSubscriber.Reply.Nack;
                });
            // Run for 3 seconds.
            Thread.Sleep(10 * 1000);
            subscriber.StopAsync(CancellationToken.None).Wait();
        }
    }
}
