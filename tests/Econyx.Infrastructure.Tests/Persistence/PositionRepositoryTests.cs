using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.Repositories;
using Econyx.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace Econyx.Infrastructure.Tests.Persistence;

public sealed class PositionRepositoryTests
{
    private readonly Mock<IPositionRepository> _repositoryMock = new();

    [Fact]
    public async Task GetByIdAsync_ShouldReturnPosition_WhenExists()
    {
        var position = CreatePosition();
        _repositoryMock
            .Setup(x => x.GetByIdAsync(position.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(position);

        var result = await _repositoryMock.Object.GetByIdAsync(position.Id);

        result.Should().NotBeNull();
        result!.MarketQuestion.Should().Be("Will it rain?");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        _repositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Position?)null);

        var result = await _repositoryMock.Object.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetOpenPositionsAsync_ShouldReturnOnlyOpen()
    {
        var open = CreatePosition();
        _repositoryMock
            .Setup(x => x.GetOpenPositionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Position> { open });

        var result = await _repositoryMock.Object.GetOpenPositionsAsync();

        result.Should().ContainSingle();
        result[0].IsOpen.Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync_ShouldBeCallable()
    {
        var position = CreatePosition();
        _repositoryMock
            .Setup(x => x.AddAsync(position, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _repositoryMock.Object.AddAsync(position);

        _repositoryMock.Verify(x => x.AddAsync(position, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Update_ShouldBeCallable()
    {
        var position = CreatePosition();
        _repositoryMock.Setup(x => x.Update(position));

        _repositoryMock.Object.Update(position);

        _repositoryMock.Verify(x => x.Update(position), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenExists()
    {
        var id = Guid.NewGuid();
        _repositoryMock
            .Setup(x => x.ExistsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _repositoryMock.Object.ExistsAsync(id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CountAsync_ShouldReturnCount()
    {
        _repositoryMock
            .Setup(x => x.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var count = await _repositoryMock.Object.CountAsync();

        count.Should().Be(5);
    }

    private static Position CreatePosition()
    {
        return Position.Create(
            Guid.NewGuid(), "Will it rain?", "tok-yes",
            PlatformType.Polymarket, TradeSide.Yes,
            Money.Create(0.40m), 50m, "RuleBased");
    }
}
