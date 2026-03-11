namespace Econyx.Dashboard.Services;

public sealed class AppLocalizer
{
    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
    {
        ["tr"] = new()
        {
            ["Nav_Dashboard"] = "Panel",
            ["Nav_Positions"] = "Pozisyonlar",
            ["Nav_Trades"] = "İşlemler",
            ["Nav_Settings"] = "Ayarlar",

            ["Loading"] = "Yükleniyor...",
            ["Save"] = "Kaydet",
            ["Saving"] = "KAYDEDİLİYOR...",
            ["Saved"] = "KAYDEDİLDİ",
            ["Refresh"] = "YENİLE",
            ["None"] = "Yok",
            ["Free"] = "ÜCRETSİZ",
            ["Prev"] = "< ÖNCEKİ",
            ["Next"] = "SONRAKİ >",
            ["Days"] = "gün",
            ["Error_Unhandled"] = "Beklenmeyen bir hata oluştu.",
            ["Reload"] = "Yeniden Yükle",

            ["Home_Title"] = "Econyx Panel",
            ["Home_Agent"] = "ECONYX AJAN",
            ["Home_SurvivalMode"] = "HAYATTA KALMA MODU",
            ["Home_Alive"] = "AKTİF",
            ["Home_Uptime"] = "ÇALIŞMA SÜRESİ",
            ["Home_Cycle"] = "DÖNGÜ",
            ["Home_CurrentBalance"] = "MEVCUT BAKİYE",
            ["Home_TotalPnl"] = "TOPLAM KÂR/ZARAR",
            ["Home_ApiCostsPaid"] = "API MALİYETLERİ",
            ["Home_SelfFunded"] = "kendi fonlu çıkarım",
            ["Home_WinRate"] = "KAZANMA ORANI",
            ["Home_BalanceHistory"] = "BAKİYE GEÇMİŞİ (LOG ÖLÇEĞİ)",
            ["Home_ChartPlaceholder"] = "Grafik canlı veriyle oluşturulacak",
            ["Home_DailyApiCost"] = "Günlük API Maliyeti",
            ["Home_Runway"] = "Kalan Süre",
            ["Home_Trades"] = "İşlemler",
            ["Home_MarketsScanned"] = "Taranan Pazarlar",
            ["Home_OpenPositions"] = "Açık Pozisyonlar",
            ["Home_BestTrade"] = "En İyi İşlem",
            ["Home_WorstTrade"] = "En Kötü İşlem",
            ["Home_SharpeRatio"] = "Sharpe Oranı",
            ["Home_AvgEdge"] = "Ort. Avantaj",
            ["Home_DataLoaded"] = "Panel verileri yüklendi",

            ["Trades_Title"] = "İşlemler — Econyx",
            ["Trades_Header"] = "İŞLEM GEÇMİŞİ",
            ["Trades_TotalCount"] = "toplam {0} işlem",
            ["Trades_Empty"] = "Henüz işlem yok",
            ["Trades_Market"] = "Pazar",
            ["Trades_Side"] = "Yön",
            ["Trades_Entry"] = "Giriş",
            ["Trades_Exit"] = "Çıkış",
            ["Trades_Qty"] = "Miktar",
            ["Trades_PnL"] = "Kâr/Zarar",
            ["Trades_Duration"] = "Süre",
            ["Trades_Strategy"] = "Strateji",
            ["Trades_Closed"] = "Kapanış",

            ["Positions_Title"] = "Pozisyonlar — Econyx",
            ["Positions_Header"] = "AÇIK POZİSYONLAR",
            ["Positions_Empty"] = "Açık pozisyon yok",
            ["Positions_Current"] = "Güncel",
            ["Positions_Opened"] = "Açılış",

            ["Settings_Title"] = "Ayarlar — Econyx",
            ["Settings_Header"] = "YAPILANDIRMA",
            ["Settings_ActiveModel"] = "AKTİF MODEL",
            ["Settings_SelectModel"] = "MODEL SEÇ",
            ["Settings_SearchModels"] = "Model ara...",
            ["Settings_NoModels"] = "Model bulunamadı",
            ["Settings_MaxTokens"] = "MAKSİMUM TOKEN",
            ["Settings_ApiKeys"] = "API ANAHTARLARI",
            ["Settings_Trading"] = "İŞLEM",
            ["Settings_Platform"] = "PLATFORM",
            ["Settings_Mode"] = "Mod",
            ["Settings_InitialBalance"] = "Başlangıç Bakiyesi",
            ["Settings_ScanInterval"] = "Tarama Aralığı",
            ["Settings_MaxPositions"] = "Maks Pozisyon",
            ["Settings_PositionSize"] = "Pozisyon Boyutu",
            ["Settings_MinEdge"] = "Min Avantaj",
            ["Settings_MinVolume"] = "Min Hacim",
            ["Settings_MaxSpread"] = "Maks Spread",
            ["Settings_StopLoss"] = "Zarar Durdur",
            ["Settings_TakeProfit"] = "Kâr Al",
            ["Settings_Survival"] = "Hayatta Kalma",
        }
    };

    private static readonly Dictionary<string, string> English = new()
    {
        ["Nav_Dashboard"] = "Dashboard",
        ["Nav_Positions"] = "Positions",
        ["Nav_Trades"] = "Trades",
        ["Nav_Settings"] = "Settings",

        ["Loading"] = "Loading...",
        ["Save"] = "Save",
        ["Saving"] = "SAVING...",
        ["Saved"] = "SAVED",
        ["Refresh"] = "REFRESH",
        ["None"] = "None",
        ["Free"] = "FREE",
        ["Prev"] = "< PREV",
        ["Next"] = "NEXT >",
        ["Days"] = "days",
        ["Error_Unhandled"] = "An unhandled error has occurred.",
        ["Reload"] = "Reload",

        ["Home_Title"] = "Econyx Dashboard",
        ["Home_Agent"] = "ECONYX AGENT",
        ["Home_SurvivalMode"] = "SURVIVAL MODE",
        ["Home_Alive"] = "ALIVE",
        ["Home_Uptime"] = "UPTIME",
        ["Home_Cycle"] = "CYCLE",
        ["Home_CurrentBalance"] = "CURRENT BALANCE",
        ["Home_TotalPnl"] = "TOTAL PnL",
        ["Home_ApiCostsPaid"] = "API COSTS PAID",
        ["Home_SelfFunded"] = "self-funded inference",
        ["Home_WinRate"] = "WIN RATE",
        ["Home_BalanceHistory"] = "BALANCE HISTORY (LOG SCALE)",
        ["Home_ChartPlaceholder"] = "Chart will render with live data",
        ["Home_DailyApiCost"] = "Daily API Cost",
        ["Home_Runway"] = "Runway",
        ["Home_Trades"] = "Trades",
        ["Home_MarketsScanned"] = "Markets Scanned",
        ["Home_OpenPositions"] = "Open Positions",
        ["Home_BestTrade"] = "Best Trade",
        ["Home_WorstTrade"] = "Worst Trade",
        ["Home_SharpeRatio"] = "Sharpe Ratio",
        ["Home_AvgEdge"] = "Avg Edge",
        ["Home_DataLoaded"] = "Dashboard data loaded",

        ["Trades_Title"] = "Trades — Econyx",
        ["Trades_Header"] = "TRADE HISTORY",
        ["Trades_TotalCount"] = "{0} total trades",
        ["Trades_Empty"] = "No trades yet",
        ["Trades_Market"] = "Market",
        ["Trades_Side"] = "Side",
        ["Trades_Entry"] = "Entry",
        ["Trades_Exit"] = "Exit",
        ["Trades_Qty"] = "Qty",
        ["Trades_PnL"] = "PnL",
        ["Trades_Duration"] = "Duration",
        ["Trades_Strategy"] = "Strategy",
        ["Trades_Closed"] = "Closed",

        ["Positions_Title"] = "Positions — Econyx",
        ["Positions_Header"] = "OPEN POSITIONS",
        ["Positions_Empty"] = "No open positions",
        ["Positions_Current"] = "Current",
        ["Positions_Opened"] = "Opened",

        ["Settings_Title"] = "Settings — Econyx",
        ["Settings_Header"] = "CONFIGURATION",
        ["Settings_ActiveModel"] = "ACTIVE MODEL",
        ["Settings_SelectModel"] = "SELECT MODEL",
        ["Settings_SearchModels"] = "Search models...",
        ["Settings_NoModels"] = "No models found",
        ["Settings_MaxTokens"] = "MAX TOKENS",
        ["Settings_ApiKeys"] = "API KEYS",
        ["Settings_Trading"] = "TRADING",
        ["Settings_Platform"] = "PLATFORM",
        ["Settings_Mode"] = "Mode",
        ["Settings_InitialBalance"] = "Initial Balance",
        ["Settings_ScanInterval"] = "Scan Interval",
        ["Settings_MaxPositions"] = "Max Positions",
        ["Settings_PositionSize"] = "Position Size",
        ["Settings_MinEdge"] = "Min Edge",
        ["Settings_MinVolume"] = "Min Volume",
        ["Settings_MaxSpread"] = "Max Spread",
        ["Settings_StopLoss"] = "Stop Loss",
        ["Settings_TakeProfit"] = "Take Profit",
        ["Settings_Survival"] = "Survival",
    };

    public string Culture { get; private set; } = "en";

    public void SetCulture(string culture)
    {
        Culture = Translations.ContainsKey(culture) ? culture : "en";
    }

    public string this[string key]
    {
        get
        {
            if (Culture != "en" && Translations.TryGetValue(Culture, out var dict) && dict.TryGetValue(key, out var val))
                return val;
            return English.GetValueOrDefault(key, key);
        }
    }

    public string Format(string key, params object[] args) => string.Format(System.Globalization.CultureInfo.InvariantCulture, this[key], args);
}
