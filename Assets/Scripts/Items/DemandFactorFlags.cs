using System;

namespace AsakuShop.Items
{
    /// <summary>
    /// Bit-flags that declare which external systems may influence demand
    /// for an item at runtime. Items do not compute their own demand — they
    /// only declare what <em>could</em> influence it. The actual demand
    /// calculation is performed by the <c>AsakuShop.Economy</c> and
    /// <c>AsakuShop.Markets</c> systems, which read these flags to decide
    /// which modifiers to apply.
    /// </summary>
    [Flags]
    public enum DemandFactorFlags
    {
        /// <summary>No external demand factors; demand is static.</summary>
        None = 0,

        /// <summary>
        /// Demand changes with weather conditions, e.g. cold drinks sell
        /// better in a heatwave; umbrellas spike during rain.
        /// </summary>
        WeatherSensitive = 1 << 0,

        /// <summary>
        /// Demand can be boosted by social-media trends or viral moments
        /// covered in the in-game news feed.
        /// </summary>
        Trendable = 1 << 1,

        /// <summary>
        /// Demand spikes around local events, festivals, and public holidays
        /// on the in-game calendar.
        /// </summary>
        EventDriven = 1 << 2,

        /// <summary>
        /// Demand shifts by season — e.g. hot drinks in winter, chilled
        /// items in summer.
        /// </summary>
        SeasonallyDriven = 1 << 3,

        /// <summary>
        /// Demand or price can be affected by supply-chain disruptions
        /// reported through the in-game economy system.
        /// </summary>
        SupplyChainSensitive = 1 << 4,
    }
}
