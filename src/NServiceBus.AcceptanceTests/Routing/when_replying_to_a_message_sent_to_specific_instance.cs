﻿namespace NServiceBus.AcceptanceTests.ScaleOut;

using System.Collections.Generic;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using Configuration.AdvancedExtensibility;
using EndpointTemplates;
using NServiceBus.Routing;
using NUnit.Framework;

public class When_replying_to_a_message_sent_to_specific_instance : NServiceBusAcceptanceTest
{
    static string ReceiverEndpoint => Conventions.EndpointNamingConvention(typeof(Receiver));

    [Test]
    public async Task Reply_address_should_be_set_to_shared_endpoint_queue()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Receiver>()
            .WithEndpoint<Sender>(b => b.When(s => s.Send(new MyRequest())))
            .Done(c => c.ReplyToAddress != null)
            .Run();

        StringAssert.DoesNotContain("XZY", context.ReplyToAddress);
    }

    public class Context : ScenarioContext
    {
        public string ReplyToAddress { get; set; }
    }

    public class Sender : EndpointConfigurationBuilder
    {
        public Sender()
        {
            EndpointSetup<DefaultServer>((c, r) =>
            {
                c.ConfigureRouting().RouteToEndpoint(typeof(MyRequest), ReceiverEndpoint);
                c.GetSettings().GetOrCreate<EndpointInstances>()
                    .AddOrReplaceInstances("testing", new List<EndpointInstance>
                    {
                        new EndpointInstance(ReceiverEndpoint, "XYZ")
                    });
            });
        }

        public class MyResponseHandler : IHandleMessages<MyResponse>
        {
            public MyResponseHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(MyResponse message, IMessageHandlerContext context)
            {
                testContext.ReplyToAddress = context.MessageHeaders[Headers.ReplyToAddress];
                return Task.CompletedTask;
            }

            Context testContext;
        }
    }

    public class Receiver : EndpointConfigurationBuilder
    {
        public Receiver()
        {
            EndpointSetup<DefaultServer>(c => { c.MakeInstanceUniquelyAddressable("XYZ"); });
        }

        public class MyRequestHandler : IHandleMessages<MyRequest>
        {
            public Task Handle(MyRequest message, IMessageHandlerContext context)
            {
                return context.Reply(new MyResponse());
            }
        }
    }

    public class MyRequest : IMessage
    {
    }

    public class MyResponse : IMessage
    {
    }
}