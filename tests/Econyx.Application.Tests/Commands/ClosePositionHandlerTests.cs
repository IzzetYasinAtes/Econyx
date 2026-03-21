using Econyx.Application.Commands.ClosePosition;
using Econyx.Application.Ports;
using Econyx.Core.Interfaces;
using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.Repositories;
using Econyx.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace Econyx.Application.Tests.Commands;

public sealed class ClosePositionHandlerTests
{
    private readonly Mock<IPositionRepository> _positionRepoMock = new();
    private readonly Mock<ITradeRepository> _tradeRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IPlatformAdapter> _platformMock = new();

    private ClosePositionHandler CreateHandler() => new(
        _positionRepoMock.Object,
        _tradeRepoMock.Object,
        _unitOfWorkMock.Object,
        _platformMock.Object);

    private static Position CreateOpenPosition()
    {
        return Position.Create(
            Guid.NewGuid(), "Will BTC hit 100k?", "tok-yes",
            PlatformType.Polymarket, TradeSide.Yes,
            Money.Create(0.30m), 100m, "RuleBased");
    }

    [Fact]
    public async Task Handle_ShouldClosePositionAndCreateTrade()
    {
        var position = CreateOpenPosition();
        var exitPrice = Money.Create(0.50m);

        _positionRepoMock
            .Setup(x => x.GetByIdAsync(position.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(position);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var command = new ClosePositionCommand(position.Id, exitPrice);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(20m);
        position.IsOpen.Should().BeFalse();

        _positionRepoMock.Verify(x => x.Update(position), Times.Once);
        _tradeRepoMock.Verify(x => x.AddAsync(It.IsAny<Trade>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenPositionDoesNotExist()
    {
        _positionRepoMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Position?)null);

        var handler = CreateHandler();
        var command = new ClosePositionCommand(Guid.NewGuid(), Money.Create(0.5m));

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Error.NotFound");
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenPositionAlreadyClosed()
    {
        var position = CreateOpenPosition();
        position.Close(Money.Create(0.40m));

        _positionRepoMock
            .Setup(x => x.GetByIdAsync(position.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(position);

        var handler = CreateHandler();
        var command = new ClosePositionCommand(position.Id, Money.Create(0.50m));

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Error.Conflict");
    }
}
