using System;
using System.Threading.Tasks;
using LarryWisherMan.ApiUtils.Domain.Factories;
using LarryWisherMan.ApiUtils.Domain.Interfaces;
using LarryWisherMan.ApiUtils.Infrastructure.Repositories;
using Moq;
using Xunit;

namespace LarryWisherMan.ApiUtils.Core.Tests.Infrastructure
{
    public class ServiceLocatorTests
    {
        public ServiceLocatorTests()
        {
            ServiceLocator.Reset();
        }

        [Fact]
        public void SessionRepository_ShouldCreateDefaultInstance_IfNoneSet()
        {
            // Arrange
            var mockFactory = new Mock<ISessionRepositoryFactory>();
            var mockRepo = new Mock<ISessionRepository>();

            mockFactory
                .Setup(f => f.Create(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string?>()))
                .Returns(mockRepo.Object);

            ServiceLocator.SetFactory(mockFactory.Object);

            // Act
            var repo = ServiceLocator.CreateRepository();

            // Assert
            Assert.NotNull(repo);
            Assert.Same(mockRepo.Object, repo);
        }

        [Fact]
        public void SetFactory_ShouldReplaceFactory_AndResetRepository()
        {
            // Arrange
            var mockFactory = new Mock<ISessionRepositoryFactory>();
            var mockRepo = new Mock<ISessionRepository>();

            mockFactory
                .Setup(f => f.Create(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string?>()))
                .Returns(mockRepo.Object);

            ServiceLocator.SetFactory(mockFactory.Object);

            // Act
            var repo = ServiceLocator.SessionRepository;

            // Assert
            Assert.Same(mockRepo.Object, repo);
            mockFactory.Verify(f => f.Create(true, true, null), Times.Once);
        }

        [Fact]
        public void SetRepository_ShouldOverrideSessionRepository()
        {
            // Arrange
            var mockRepo = new Mock<ISessionRepository>();

            // Act
            ServiceLocator.SetRepository(mockRepo.Object);
            var result = ServiceLocator.SessionRepository;

            // Assert
            Assert.Same(mockRepo.Object, result);
        }

        [Fact]
        public void CreateRepository_ShouldRespectParameters()
        {
            // Arrange
            var mockFactory = new Mock<ISessionRepositoryFactory>();
            var mockRepo = new Mock<ISessionRepository>();

            mockFactory
                .Setup(f => f.Create(false, false, null))
                .Returns(mockRepo.Object);

            ServiceLocator.SetFactory(mockFactory.Object);

            // Act
            var result = ServiceLocator.CreateRepository(saveToFile: false, preloadFromFile: false);

            // Assert
            Assert.Same(mockRepo.Object, result);
            mockFactory.Verify(f => f.Create(false, false, null), Times.Once);
        }

        [Fact]
        public void Dispose_ShouldCallDispose_OnDisposableRepository()
        {
            // Arrange
            var mockDisposableRepo = new Mock<ISessionRepository>();
            mockDisposableRepo.As<IDisposable>().Setup(d => d.Dispose());

            ServiceLocator.SetRepository(mockDisposableRepo.Object);

            // Act
            ServiceLocator.Dispose();

            // Assert
            mockDisposableRepo.As<IDisposable>().Verify(d => d.Dispose(), Times.Once);
        }

        [Fact]
        public void Dispose_ShouldNotThrow_WhenRepositoryIsNotDisposable()
        {
            // Arrange
            var mockRepo = new Mock<ISessionRepository>();
            ServiceLocator.SetRepository(mockRepo.Object);

            // Act & Assert
            var ex = Record.Exception(() => ServiceLocator.Dispose());
            Assert.Null(ex);
        }


    }
}
