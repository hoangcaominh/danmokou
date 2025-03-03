﻿using System;
using System.Reactive;
using System.Threading.Tasks;
using BagoumLib;
using BagoumLib.Cancellation;
using Danmokou.Core;
using Danmokou.Danmaku;
using Danmokou.GameInstance;
using Danmokou.Scenes;
using Danmokou.Scriptables;
using Danmokou.Services;
using Danmokou.SM;
using JetBrains.Annotations;

namespace Danmokou.Behavior {
/// <summary>
/// Service that allows executing a state machine for a "level".
/// <br/>A "level" is a self-contained construct such as a stage in a multi-stage danmaku game
///  or a scene in a scene-based danmaku game.
/// </summary>
public class LevelController : BehaviorEntity {
    public IStageConfig? stage;
    public StageConfig? wip_stage;
    private string? _DefaultSuicideStyle => stage?.DefaultSuicideStyle;
    public static string? DefaultSuicideStyle { get; private set; }

    /// <inheritdoc cref="LevelRunRequest.Method"/>
    public enum LevelRunMethod {
        /// <summary>
        /// Run just the specified phase in the provided state machine, then finish.
        /// </summary>
        SINGLE,
        /// <summary>
        /// Run the specified phase in the provided state machine, then continue executing the rest from there.
        /// </summary>
        CONTINUE
    }
    /// <summary>
    /// Struct containing information for running a state machine in a level.
    /// </summary>
    /// <param name="ToPhase">Phase of the state machine to execute.</param>
    /// <param name="Method">Enum describing how to execute the provided state machine for the level.</param>
    /// <param name="Stage">Stage configuration.</param>
    /// <param name="cT">Cancellation token.</param>
    public record LevelRunRequest(int ToPhase, LevelRunMethod Method, IStageConfig Stage, ICancellee cT) { }

    /// <summary>
    /// Run a level.
    /// </summary>
    public async Task<InstanceStepCompletion> RunLevel(LevelRunRequest req, Checkpoint? fromCheckpoint) {
        if (req.Method == LevelRunMethod.SINGLE) 
            phaseController.Override(fromCheckpoint?.StagePhase ?? req.ToPhase);
        else if (req.Method == LevelRunMethod.CONTINUE) 
            phaseController.SetGoTo(fromCheckpoint?.StagePhase ?? req.ToPhase);
        stage = req.Stage;
        var checkpointCancel = new TaskCompletionSource<Checkpoint>();
        using var _ = GameManagement.Instance.SetStageCheckpointCancel(req.Stage, checkpointCancel.SetResult);
        var checkpointTask = checkpointCancel.Task;
        var smTask = RunBehaviorSM(SMRunner.RunRoot(req.Stage.StateMachine, req.cT),
            new SMContext { LoadCheckpoint = fromCheckpoint, ExternalCT = req.cT });
        await Task.WhenAny(checkpointCancel.Task, smTask);
        if (checkpointTask.IsCompletedSuccessfully)
            return new InstanceStepCompletion.RestartCheckpoint(checkpointTask.Result);
        else if (smTask.IsCanceled)
            return new InstanceStepCompletion.Cancelled();
        else
            return new InstanceStepCompletion.Completed();
    }

    protected override void Awake() {
        DefaultSuicideStyle = stage?.DefaultSuicideStyle;
#if UNITY_EDITOR
        if (SceneIntermediary.IsFirstScene) {
            if (wip_stage != null) {
                Logs.Log("Running default stage under editor first-scene conditions");
                behaviorScript = wip_stage.stateMachine;
            } else if (behaviorScript != null) {
                Logs.Log("Running default level script under editor first-scene conditions");
            }
        } else
            behaviorScript = null;
#else
        behaviorScript = null;
#endif
        base.Awake();
    }

    protected override void BindListeners() {
        base.BindListeners();
        RegisterService(this, new ServiceLocator.ServiceOptions { Unique = true });
    }

    private void OnDestroy() {
        DefaultSuicideStyle = null;
    }
}
}