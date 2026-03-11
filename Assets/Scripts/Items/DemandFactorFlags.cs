using System;

namespace AsakuShop.Items
{
    // Bit-flags that declare which external systems may influence demand for an item at runtime. 
    // Items do not compute their own demand — they only declare what could influence it. 
    // The actual demand calculation is performed by the AsakuShop.Economy andAsakuShop.Markets systems, 
    // which read these flags to decide which modifiers to apply.
    [Flags]
    public enum DemandFactorFlags
    {
        //No external demand factors; demand is static.
        None = 0,

        // Demand changes with weather conditions, e.g. cold drinks sell
        // better in a heatwave; umbrellas spike during rain.
        WeatherSensitive = 1 << 0,

        // Demand can be boosted by social-media trends or viral moments covered in the in-game news feed.
        Trendable = 1 << 1,

        // Demand spikes around local events, festivals, and public holidays on the in-game calendar.
        EventDriven = 1 << 2,

        // Demand shifts by season — e.g. hot drinks in winter, chilled items in summer.
        SeasonallyDriven = 1 << 3,

        // Demand or price can be affected by supply-chain disruptions reported through the in-game economy system.
        SupplyChainSensitive = 1 << 4,
    }
}