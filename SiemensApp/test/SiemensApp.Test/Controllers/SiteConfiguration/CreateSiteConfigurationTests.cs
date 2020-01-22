using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using SiemensApp.Dto;
using SiemensApp.Entities;
using Willow.Tests.Infrastructure;
using Workflow.Tests;
using Xunit;
using Xunit.Abstractions;

namespace SiemensApp.Test.Controllers.Airports
{
    public class CreateSiteConfigurationTests : BaseInMemoryTest
    {
        public CreateSiteConfigurationTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task DataProvided_CreateSiteConfigurationTests_SiteConfigurationCreatedAndReturnNoContent()
        {
            using (var server = CreateServerFixture(ServerFixtureConfigurations.InMemoryDb))
            using (var client = server.CreateClient())
            {
                var siteConfiguration = Fixture.Build<SiteConfigurationDto>().Create();
                var response = await client.PostAsJsonAsync("api/siteConfiguration/create", siteConfiguration);

                response.StatusCode.Should().Be(HttpStatusCode.NoContent);

                var context = server.Arrange().CreateDbContext<SiemensDbContext>();
                var savedSiteConfiguration = context.SiteConfigurations.FirstOrDefault(sc => sc.SiteId == siteConfiguration.SiteId);
                Assert.NotNull(savedSiteConfiguration);
            }
        }

        [Fact]
        public async Task SiteConfigurationExist_CreateSiteConfigurationTests_ReturnBadRequest()
        {
            var siteId = Guid.NewGuid();
            var existingSiteConfiguration = Fixture.Build<SiteConfigurationEntity>()
                                           .With(sc => sc.SiteId, siteId)
                                           .Create();
            using (var server = CreateServerFixture(ServerFixtureConfigurations.InMemoryDb))
            using (var client = server.CreateClient())
            {
                var context = server.Arrange().CreateDbContext<SiemensDbContext>();
                context.SiteConfigurations.Add(existingSiteConfiguration);
                context.SaveChanges();

                var creatingSiteConfiguration = Fixture.Build<SiteConfigurationDto>()
                                                       .With(sc => sc.SiteId, siteId)
                                                       .Create();
                var response = await client.PostAsJsonAsync("api/siteConfiguration/create", creatingSiteConfiguration);

                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            }
        }
    }
}