﻿using Danmokou.Core;
using Danmokou.DMath;
using Danmokou.Scriptables;

namespace Danmokou.GameInstance {
public static class InstanceConsts {
    public static bool PowerMechanicEnabled { get; } = false;
    public static bool MeterMechanicEnabled { get; } = true;
    
    public const int defltContinues = 42;
    public static int StartLives(InstanceMode mode) {
        if (mode == InstanceMode.CAMPAIGN || mode == InstanceMode.TUTORIAL || mode == InstanceMode.STAGE_PRACTICE) 
            return 7;
        else if (mode.OneLife()) 
            return 1;
        else if (mode == InstanceMode.DEBUG || mode == InstanceMode.NULL) 
            return 14;
        else 
            return 1;
    }
    public static int StartBombs(InstanceMode mode) {
        if (mode == InstanceMode.CAMPAIGN || mode == InstanceMode.TUTORIAL || mode == InstanceMode.STAGE_PRACTICE) 
            return 2;
        else if (mode.OneLife()) 
            return 0;
        else 
            return 3;
    }
    public static double StartPower(InstanceMode mode) {
        if (mode.OneLife() || !PowerMechanicEnabled) 
            return powerMax;
        else 
            return M.Clamp(powerMin, powerMax, powerDefault);
    }
    public static double StartMeter(InstanceMode mode) {
        if (mode.IsOneCard()) 
            return 0;
        else
            return 0.7;
    }

    #region Difficulty
    
    /// <summary>
    /// Inclusive
    /// </summary>
    public const int minRankLevel = 0;
    /// <summary>
    /// Inclusive
    /// </summary>
    public const int maxRankLevel = 42;

    //Note: since you get half of this by default, you only need the other half to go up or down a level.
    public static double RankPointsRequiredForLevel(int level) =>
        2000;
    ///M.BlockRound(100, 1000 * (1 + Math.Log(Math.Max(1, level), 4)));
    
    public static double DefaultRankPointsForLevel(int level) => RankPointsRequiredForLevel(level) * 0.5;
    
    public const double RankPointsGraze = 8;
    public const double RankPointsCollectItem = 3;
    public const double RankPointsMissedItem = -5;
    public const double RankPointsScoreExtend = 420;
    public const double RankPointsDeath = -10000;
    public const double RankPointsBomb = -1000;

    public static double RankPointsForCard(CardRecord cr) => 100 * cr.stars;
    
    #endregion
    
    #region Power
    
    public const double powerMax = 4;
    public const double powerMin = 1;
#if UNITY_EDITOR
    public const double powerDefault = 1000;
#else
    public const double powerDefault = 1;
#endif
    public const double powerDeathLoss = -1;
    public const double powerItemValue = 0.05;
    public const double powerToValueConversion = 2;
    
    #endregion
    
    #region Meter
    
    public const double meterBoostGem = 0.021;
    public const double meterRefillRate = 0.002;
    public const double meterUseRate = 0.314;
    public const double meterUseThreshold = 0.42;
    public const double meterUseInstantCost = 0.042;
    
    #endregion
    
    #region PIV
    
    public const long smallValueItemPoints = 314;
    public const long valueItemPoints = 3142;
    public const decimal smallValueRatio = 0.1m;
    
    public const double pivPerPPP = 0.01;
    public const double pivFallStep = 0.1;
    
    public const double faithDecayRate = 0.12;
    public const double faithLenienceFall = 5;
    public const double faithLenienceValue = 0.2;
    public const double faithLeniencePointPP = 0.3;
    public const double faithLenienceEnemyDestroy = 0.1;
    public const double faithBoostValue = 0.02;
    public const double faithBoostPointPP = 0.09;
    public const double faithLeniencePhase = 4;
    
    #endregion


    public static readonly long[] scoreLives = {
        2000000,
        5000000,
        10000000,
        15000000,
        20000000,
        25000000,
        30000000,
        40000000,
        50000000,
        60000000,
        70000000,
        80000000,
        100000000
    };
    public static readonly int[] pointLives = {
        69,
        141,
        224,
        314,
        420,
        618,
        840,
        1084,
        1337,
        1618,
        2048,
        2718,
        3142,
        9001,
        int.MaxValue
    };
}
}