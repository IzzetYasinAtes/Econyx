namespace Econyx.Dashboard.Hubs;

using Microsoft.AspNetCore.SignalR;

public class TradingHub : Hub
{
    public async Task SendTradeUpdate(string message) =>
        await Clients.All.SendAsync("TradeUpdate", message);

    public async Task SendBalanceUpdate(decimal balance, decimal pnl) =>
        await Clients.All.SendAsync("BalanceUpdate", balance, pnl);

    public async Task SendCycleUpdate(int cycleNumber, int marketsScanned, int signalsFound) =>
        await Clients.All.SendAsync("CycleUpdate", cycleNumber, marketsScanned, signalsFound);
}
