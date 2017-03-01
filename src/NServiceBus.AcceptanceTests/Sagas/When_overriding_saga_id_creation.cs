﻿namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    public class When_overriding_saga_id_creation : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_generate_saga_id_accordingly()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointThatHostsASaga>(
                    b => b.When(session => session.SendLocal(new StartSaga
                    {
                        CustomerId = "42",
                    })))
                .Done(c => c.SagaId.HasValue)
                .Run();

            Assert.AreEqual(new Guid("5ebef5b7-815e-653c-2ee7-37ed83d7d7b5"), context.SagaId);
        }

        public class Context : ScenarioContext
        {
            public Guid? SagaId { get; set; }
        }

        public class EndpointThatHostsASaga : EndpointConfigurationBuilder
        {
            public EndpointThatHostsASaga()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.EnableFeature<TimeoutManager>();
                    config.RegisterComponents(c => c.RegisterSingleton<ISagaIdGenerator>(new CustomSagaIdGenerator()));
                });
            }

            class CustomSagaIdGenerator : ISagaIdGenerator
            {
                public Guid Generate(SagaIdGeneratorContext context)
                {
                    return ToGuid($"{context.SagaMetadata.SagaEntityType.FullName}_{context.CorrelationPropertyName}_{context.CorrelationPropertyValue}");
                }

                static Guid ToGuid(string src)
                {
                    var stringbytes = Encoding.UTF8.GetBytes(src);
                    using (var provider = new SHA1CryptoServiceProvider())
                    {
                        var hashedBytes = provider.ComputeHash(stringbytes);
                        Array.Resize(ref hashedBytes, 16);
                        return new Guid(hashedBytes);
                    }
                }
            }

            public class CustomSagaIdSaga : Saga<CustomSagaIdSaga.CustomSagaIdSagaData>,
                IAmStartedByMessages<StartSaga>
            {
                public Context TestContext { get; set; }

                public Task Handle(StartSaga message, IMessageHandlerContext context)
                {
                    Data.CustomerId = message.CustomerId;
                    TestContext.SagaId = Data.Id;

                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<CustomSagaIdSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSaga>(m => m.CustomerId).ToSaga(s => s.CustomerId);
                }

                public class CustomSagaIdSagaData : ContainSagaData
                {
                    public virtual string CustomerId { get; set; }
                }

                public class TimeHasPassed
                {
                }
            }
        }

        public class StartSaga : IMessage
        {
            public string CustomerId { get; set; }
        }
    }
}