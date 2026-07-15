using System;

namespace ProceduralGeneration
{
    [Flags]
    public enum RoomTags
    {
        None = 0,
        Power = 1 << 0,
        Industrial = 1 << 1,
        Maintenance = 1 << 2,
        Storage = 1 << 3,
        Residential = 1 << 4,
        Medical = 1 << 5,
        Security = 1 << 6,
        Command = 1 << 7,
        Navigation = 1 << 8,
        Engineering = 1 << 9,
        Hot = 1 << 10,
        Cold = 1 << 11,
        Hazard = 1 << 12,
        Clean = 1 << 13,
        Large = 1 << 14,
        Small = 1 << 15,
        Critical = 1 << 16,
        Service = 1 << 17,
        UpperDeck = 1 << 18,
        LowerDeck = 1 << 19,
        DeadEnd = 1 << 20,
        MainRoute = 1 << 21,
        Optional = 1 << 22,
        Hidden = 1 << 23,
        SafeZone = 1 << 24,
        DangerZone = 1 << 25,
        OuterHull = 1 << 26,
        FutureMonsterSpawn = 1 << 27,
        FutureEventZone = 1 << 28,
        FutureLootZone = 1 << 29
    }
}
