using Xunit;
using System.Collections.Generic;
using System.Linq;
using AFKSentinel.Core.Models;
using AFKSentinel.Core.Physics;
using System;
using System.Reflection; // For BindingFlags

namespace AFKSentinel.Tests;

public class PhysicsEngineTests
{
    private readonly DetectionSettings _settings;

    public PhysicsEngineTests()
    {
        _settings = new DetectionSettings(); // Use default settings for tests
    }

    // Helper to create a list of MotionData points with controlled timestamps
    private static List<MotionData> CreateMotionDataList(IEnumerable<(int x, int y, InputType type, bool isInjected)> points, long initialTimestamp = 0, long timeDelta = 1000)
    {
        var list = new List<MotionData>();
        long currentTimestamp = initialTimestamp == 0 ? DateTime.UtcNow.Ticks : initialTimestamp;
        foreach (var p in points)
        {
            list.Add(new MotionData(p.x, p.y, p.type, p.isInjected, 0, currentTimestamp));
            currentTimestamp += timeDelta; 
        }
        return list;
    }

    [Fact]
    public void IsLinear_DetectsPerfectlyLinearMotion()
    {
        // Arrange
        var linearPoints = CreateMotionDataList(new[]
        {
            (10, 10, InputType.MouseMove, false),
            (20, 20, InputType.MouseMove, false),
            (30, 30, InputType.MouseMove, false),
            (40, 40, InputType.MouseMove, false),
            (50, 50, InputType.MouseMove, false)
        });

        // Act
        var isLinearMethod = typeof(PhysicsEngine).GetMethod("IsLinear", BindingFlags.NonPublic | BindingFlags.Static);
        bool isLinear = (bool)isLinearMethod.Invoke(null, new object[] { linearPoints, _settings })!;

        // Assert
        Assert.True(isLinear);
    }

    [Fact]
    public void IsLinear_DoesNotDetectNonLinearMotion()
    {
        // Arrange
        var nonLinearPoints = CreateMotionDataList(new[]
        {
            (10, 10, InputType.MouseMove, false),
            (15, 25, InputType.MouseMove, false),
            (30, 20, InputType.MouseMove, false),
            (45, 35, InputType.MouseMove, false),
            (50, 10, InputType.MouseMove, false)
        });

        // Act
        var isLinearMethod = typeof(PhysicsEngine).GetMethod("IsLinear", BindingFlags.NonPublic | BindingFlags.Static);
        bool isLinear = (bool)isLinearMethod.Invoke(null, new object[] { nonLinearPoints, _settings })!;

        // Assert
        Assert.False(isLinear);
    }

    [Fact]
    public void IsPeriodic_DetectsConsistentPeriodicMotion()
    {
        // Arrange
        var periodicPoints = CreateMotionDataList(Enumerable.Range(0, 20).Select(i => (i * 10, i * 10, InputType.MouseMove, false)).ToArray(), 
                                                  initialTimestamp: DateTime.UtcNow.Ticks, 
                                                  timeDelta: 1_000_000); // 100ms

        // Act
        var isPeriodicMethod = typeof(PhysicsEngine).GetMethod("IsPeriodic", BindingFlags.NonPublic | BindingFlags.Static);
        bool isPeriodic = (bool)isPeriodicMethod.Invoke(null, new object[] { periodicPoints, _settings })!;

        // Assert
        Assert.True(isPeriodic);
    }

    [Fact]
    public void IsPeriodic_DoesNotDetectInconsistentMotion()
    {
        // Arrange
        var pointsData = new List<(int x, int y, InputType type, bool isInjected)>();
        for (int i = 0; i < 20; i++)
        {
            pointsData.Add((i * 10, i * 10, InputType.MouseMove, false));
        }
        
        var nonPeriodicPoints = new List<MotionData>();
        long currentTimestamp = DateTime.UtcNow.Ticks;
        for (int i = 0; i < pointsData.Count; i++)
        {
            nonPeriodicPoints.Add(new MotionData(pointsData[i].x, pointsData[i].y, pointsData[i].type, pointsData[i].isInjected, 0, currentTimestamp));
            currentTimestamp += (i % 2 == 0 ? 500_000 : 1_000_000); // 50ms or 100ms
        }

        // Act
        var isPeriodicMethod = typeof(PhysicsEngine).GetMethod("IsPeriodic", BindingFlags.NonPublic | BindingFlags.Static);
        bool isPeriodic = (bool)isPeriodicMethod.Invoke(null, new object[] { nonPeriodicPoints, _settings })!;

        // Assert
        Assert.False(isPeriodic);
    }

    [Fact]
    public void IsJittery_DetectsHighPathLengthLowDisplacement()
    {
        // Arrange
        var jitteryPoints = CreateMotionDataList(new[]
        {
            (10, 10, InputType.MouseMove, false), (11, 10, InputType.MouseMove, false),
            (10, 11, InputType.MouseMove, false), (11, 11, InputType.MouseMove, false),
            (10, 10, InputType.MouseMove, false), (11, 10, InputType.MouseMove, false),
            (10, 11, InputType.MouseMove, false), (11, 11, InputType.MouseMove, false),
            (10, 10, InputType.MouseMove, false), (11, 10, InputType.MouseMove, false),
            (10, 11, InputType.MouseMove, false), (11, 11, InputType.MouseMove, false),
            (10, 10, InputType.MouseMove, false), (11, 10, InputType.MouseMove, false),
            (10, 11, InputType.MouseMove, false), (11, 11, InputType.MouseMove, false),
            (10, 10, InputType.MouseMove, false), (11, 10, InputType.MouseMove, false),
            (10, 11, InputType.MouseMove, false), (11, 11, InputType.MouseMove, false),
            (10, 10, InputType.MouseMove, false)
        });

        // Act
        var isJitteryMethod = typeof(PhysicsEngine).GetMethod("IsJittery", BindingFlags.NonPublic | BindingFlags.Static);
        bool isJittery = (bool)isJitteryMethod.Invoke(null, new object[] { jitteryPoints, _settings })!;

        // Assert
        Assert.True(isJittery);
    }

    [Fact]
    public void IsJittery_DoesNotDetectDirectMotion()
    {
        // Arrange
        var directPoints = CreateMotionDataList(new[]
        {
            (10, 10, InputType.MouseMove, false),
            (20, 10, InputType.MouseMove, false),
            (30, 10, InputType.MouseMove, false),
            (40, 10, InputType.MouseMove, false),
            (50, 10, InputType.MouseMove, false),
            (60, 10, InputType.MouseMove, false),
            (70, 10, InputType.MouseMove, false),
            (80, 10, InputType.MouseMove, false),
            (90, 10, InputType.MouseMove, false),
            (100, 10, InputType.MouseMove, false),
            (110, 10, InputType.MouseMove, false)
        });

        // Act
        var isJitteryMethod = typeof(PhysicsEngine).GetMethod("IsJittery", BindingFlags.NonPublic | BindingFlags.Static);
        bool isJittery = (bool)isJitteryMethod.Invoke(null, new object[] { directPoints, _settings })!;

        // Assert
        Assert.False(isJittery);
    }

    [Fact]
    public void IsZeroInput_DetectsMovementWithoutOtherInputs()
    {
        // Arrange
        var zeroInputPoints = CreateMotionDataList(Enumerable.Range(0, 50).Select(i => (10 + i * 2, 10 + i * 2, InputType.MouseMove, false)).ToArray());

        // Act
        var isZeroInputMethod = typeof(PhysicsEngine).GetMethod("IsZeroInput", BindingFlags.NonPublic | BindingFlags.Static);
        bool isZeroInput = (bool)isZeroInputMethod.Invoke(null, new object[] { zeroInputPoints, _settings })!;

        // Assert
        Assert.True(isZeroInput);
    }

    [Fact]
    public void IsZeroInput_DoesNotDetectMovementWithOtherInputs()
    {
        // Arrange
        var withInputPoints = CreateMotionDataList(new[]
        {
            (10, 10, InputType.MouseMove, false),
            (20, 10, InputType.LeftDown, false),
            (30, 10, InputType.MouseMove, false),
            (40, 10, InputType.LeftUp, false),
            (50, 10, InputType.MouseMove, false)
        });

        // Act
        var isZeroInputMethod = typeof(PhysicsEngine).GetMethod("IsZeroInput", BindingFlags.NonPublic | BindingFlags.Static);
        bool isZeroInput = (bool)isZeroInputMethod.Invoke(null, new object[] { withInputPoints, _settings })!;

        // Assert
        Assert.False(isZeroInput);
    }

    [Fact]
    public void Analyze_DetectsSoftwareJiggler()
    {
        // Arrange
        var injectedPoints = CreateMotionDataList(new[]
        {
            (10, 10, InputType.MouseMove, true),
            (11, 10, InputType.MouseMove, false),
            (12, 10, InputType.MouseMove, false)
        });

        // Act
        DetectionResult result = PhysicsEngine.Analyze(injectedPoints, _settings);

        // Assert
        Assert.Equal(DetectionResult.SoftwareJiggler, result);
    }

    [Fact]
    public void Analyze_ReturnsHumanForNormalActivity()
    {
        // Arrange
        var humanPoints = CreateMotionDataList(new[]
        {
            (10, 10, InputType.MouseMove, false),
            (25, 12, InputType.MouseMove, false),
            (30, 20, InputType.LeftDown, false),
            (42, 28, InputType.MouseMove, false),
            (50, 30, InputType.LeftUp, false),
            (65, 35, InputType.MouseMove, false),
            (70, 38, InputType.MouseMove, false)
        });

        // Act
        DetectionResult result = PhysicsEngine.Analyze(humanPoints, _settings);

        // Assert
        Assert.Equal(DetectionResult.Human, result);
    }
}
