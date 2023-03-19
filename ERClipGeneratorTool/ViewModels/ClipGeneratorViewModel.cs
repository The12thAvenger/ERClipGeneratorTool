using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Text.RegularExpressions;
using ERClipGeneratorTool.Util;
using HKLib.hk2018;
using ReactiveHistory;
using ReactiveUI;

// ReSharper disable BitwiseOperatorOnEnumWithoutFlags

namespace ERClipGeneratorTool.ViewModels;

public partial class ClipGeneratorViewModel : ViewModelBase, IActivatableViewModel
{
    private readonly hkbClipGenerator _clipGenerator;
    private readonly IHistory _history;
    private readonly List<CustomManualSelectorGenerator> _parents;

    public ClipGeneratorViewModel(hkbClipGenerator clipGenerator, List<CustomManualSelectorGenerator> parents,
        IHistory history)
    {
        _clipGenerator = clipGenerator;
        _history = history;
        _parents = parents;
        Activator = new ViewModelActivator();
        this.WhenActivated(d =>
        {
            DeleteCommand = ReactiveCommand.Create(Delete).DisposeWith(d);
            this.WhenAnyValue(x => x.Name)
                .ObserveWithHistory(value => Name = value ?? "", clipGenerator.m_name, history);
            this.WhenAnyValue(x => x.AnimationName).ObserveWithHistory(value => AnimationName = value ?? "",
                clipGenerator.m_animationName, history);
            this.WhenAnyValue(x => x.AnimationInternalId).ObserveWithHistory(value => AnimationInternalId = value,
                clipGenerator.m_animationInternalId, history);
            this.WhenAnyValue(x => x.StartTime).ObserveWithHistory(value => StartTime = value,
                clipGenerator.m_startTime, history);
            this.WhenAnyValue(x => x.CropStart).ObserveWithHistory(value => CropStart = value,
                clipGenerator.m_cropStartAmountLocalTime, history);
            this.WhenAnyValue(x => x.CropEnd).ObserveWithHistory(value => CropEnd = value,
                clipGenerator.m_cropEndAmountLocalTime, history);
            this.WhenAnyValue(x => x.EnforcedDuration).ObserveWithHistory(value => EnforcedDuration = value,
                clipGenerator.m_enforcedDuration, history);
            this.WhenAnyValue(x => x.UserControlledTimeFraction).ObserveWithHistory(
                value => UserControlledTimeFraction = value,
                clipGenerator.m_userControlledTimeFraction, history);
            this.WhenAnyValue(x => x.UserPartitionMask).ObserveWithHistory(value => UserPartitionMask = value,
                clipGenerator.m_userPartitionMask, history);
            this.WhenAnyValue(x => x.PlaybackSpeed).ObserveWithHistory(value => PlaybackSpeed = value,
                clipGenerator.m_playbackSpeed, history);
            this.WhenAnyValue(x => x.Mode).ObserveWithHistory(value => Mode = value,
                clipGenerator.m_mode, history);
            this.WhenAnyValue(x => x.ContinueMotionAtEnd).ObserveWithHistory(value => ContinueMotionAtEnd = value,
                Flags.HasFlag(hkbClipGenerator.ClipFlags.FLAG_CONTINUE_MOTION_AT_END), history);
            this.WhenAnyValue(x => x.SyncHalfCycleInPingPongMode).ObserveWithHistory(
                value => SyncHalfCycleInPingPongMode = value,
                Flags.HasFlag(hkbClipGenerator.ClipFlags.FLAG_SYNC_HALF_CYCLE_IN_PING_PONG_MODE), history);
            this.WhenAnyValue(x => x.Mirror).ObserveWithHistory(value => Mirror = value,
                Flags.HasFlag(hkbClipGenerator.ClipFlags.FLAG_MIRROR), history);
            this.WhenAnyValue(x => x.ForceDensePose).ObserveWithHistory(value => ForceDensePose = value,
                Flags.HasFlag(hkbClipGenerator.ClipFlags.FLAG_FORCE_DENSE_POSE), history);
            this.WhenAnyValue(x => x.DontConvertAnnotationsToTriggers).ObserveWithHistory(
                value => DontConvertAnnotationsToTriggers = value,
                Flags.HasFlag(hkbClipGenerator.ClipFlags.FLAG_DONT_CONVERT_ANNOTATIONS_TO_TRIGGERS), history);
            this.WhenAnyValue(x => x.IgnoreMotion).ObserveWithHistory(value => IgnoreMotion = value,
                Flags.HasFlag(hkbClipGenerator.ClipFlags.FLAG_IGNORE_MOTION), history);
        });
    }

    public IReadOnlyList<CustomManualSelectorGenerator> Parents => _parents;

    public string Name
    {
        get => _clipGenerator.m_name ?? "";
        set => this.RaiseAndSetIfChanged(ref _clipGenerator.m_name, value);
    }

    [CustomValidation(typeof(ClipGeneratorViewModel), nameof(ValidateAnimationName))]
    public string AnimationName
    {
        get => _clipGenerator.m_animationName ?? "";
        set => this.RaiseAndSetIfChanged(ref _clipGenerator.m_animationName, value);
    }

    public short AnimationInternalId
    {
        get => _clipGenerator.m_animationInternalId;
        set => this.RaiseAndSetIfChanged(ref _clipGenerator.m_animationInternalId, value);
    }

    public float StartTime
    {
        get => _clipGenerator.m_startTime;
        set => this.RaiseAndSetIfChanged(ref _clipGenerator.m_startTime, value);
    }

    public float CropStart
    {
        get => _clipGenerator.m_cropStartAmountLocalTime;
        set => this.RaiseAndSetIfChanged(ref _clipGenerator.m_cropStartAmountLocalTime, value);
    }

    public float CropEnd
    {
        get => _clipGenerator.m_cropEndAmountLocalTime;
        set => this.RaiseAndSetIfChanged(ref _clipGenerator.m_cropEndAmountLocalTime, value);
    }

    public float EnforcedDuration
    {
        get => _clipGenerator.m_enforcedDuration;
        set => this.RaiseAndSetIfChanged(ref _clipGenerator.m_enforcedDuration, value);
    }

    public float UserControlledTimeFraction
    {
        get => _clipGenerator.m_userControlledTimeFraction;
        set => this.RaiseAndSetIfChanged(ref _clipGenerator.m_userControlledTimeFraction, value);
    }

    public uint UserPartitionMask
    {
        get => _clipGenerator.m_userPartitionMask;
        set => this.RaiseAndSetIfChanged(ref _clipGenerator.m_userPartitionMask, value);
    }

    public float PlaybackSpeed
    {
        get => _clipGenerator.m_playbackSpeed;
        set => this.RaiseAndSetIfChanged(ref _clipGenerator.m_playbackSpeed, value);
    }

    public hkbClipGenerator.PlaybackMode Mode
    {
        get => _clipGenerator.m_mode;
        set => this.RaiseAndSetIfChanged(ref _clipGenerator.m_mode, value);
    }

    public IReadOnlyList<hkbClipGenerator.PlaybackMode> PlaybackModes { get; } = new List<hkbClipGenerator.PlaybackMode>
    {
        hkbClipGenerator.PlaybackMode.MODE_SINGLE_PLAY,
        hkbClipGenerator.PlaybackMode.MODE_LOOPING,
        hkbClipGenerator.PlaybackMode.MODE_USER_CONTROLLED,
        hkbClipGenerator.PlaybackMode.MODE_PING_PONG,
        hkbClipGenerator.PlaybackMode.MODE_COUNT
    };

    public hkbClipGenerator.ClipFlags Flags
    {
        get => (hkbClipGenerator.ClipFlags)_clipGenerator.m_flags;
        set => this.RaiseAndSetIfChanged(ref _clipGenerator.m_flags, (sbyte)value);
    }

    public bool ContinueMotionAtEnd
    {
        get => Flags.HasFlag(hkbClipGenerator.ClipFlags.FLAG_CONTINUE_MOTION_AT_END);
        set
        {
            Flags = value
                ? Flags | hkbClipGenerator.ClipFlags.FLAG_CONTINUE_MOTION_AT_END
                : Flags & ~hkbClipGenerator.ClipFlags.FLAG_CONTINUE_MOTION_AT_END;
            this.RaisePropertyChanged();
        }
    }

    public bool SyncHalfCycleInPingPongMode
    {
        get => Flags.HasFlag(hkbClipGenerator.ClipFlags.FLAG_SYNC_HALF_CYCLE_IN_PING_PONG_MODE);
        set
        {
            Flags = value
                ? Flags | hkbClipGenerator.ClipFlags.FLAG_SYNC_HALF_CYCLE_IN_PING_PONG_MODE
                : Flags & ~hkbClipGenerator.ClipFlags.FLAG_SYNC_HALF_CYCLE_IN_PING_PONG_MODE;
            this.RaisePropertyChanged();
        }
    }

    public bool Mirror
    {
        get => Flags.HasFlag(hkbClipGenerator.ClipFlags.FLAG_MIRROR);
        set
        {
            Flags = value
                ? Flags | hkbClipGenerator.ClipFlags.FLAG_MIRROR
                : Flags & ~hkbClipGenerator.ClipFlags.FLAG_MIRROR;
            this.RaisePropertyChanged();
        }
    }

    public bool ForceDensePose
    {
        get => Flags.HasFlag(hkbClipGenerator.ClipFlags.FLAG_FORCE_DENSE_POSE);
        set
        {
            Flags = value
                ? Flags | hkbClipGenerator.ClipFlags.FLAG_FORCE_DENSE_POSE
                : Flags & ~hkbClipGenerator.ClipFlags.FLAG_FORCE_DENSE_POSE;
            this.RaisePropertyChanged();
        }
    }

    public bool DontConvertAnnotationsToTriggers
    {
        get => Flags.HasFlag(hkbClipGenerator.ClipFlags.FLAG_DONT_CONVERT_ANNOTATIONS_TO_TRIGGERS);
        set
        {
            Flags = value
                ? Flags | hkbClipGenerator.ClipFlags.FLAG_DONT_CONVERT_ANNOTATIONS_TO_TRIGGERS
                : Flags & ~hkbClipGenerator.ClipFlags.FLAG_DONT_CONVERT_ANNOTATIONS_TO_TRIGGERS;
            this.RaisePropertyChanged();
        }
    }

    public bool IgnoreMotion
    {
        get => Flags.HasFlag(hkbClipGenerator.ClipFlags.FLAG_IGNORE_MOTION);
        set
        {
            Flags = value
                ? Flags | hkbClipGenerator.ClipFlags.FLAG_IGNORE_MOTION
                : Flags & ~hkbClipGenerator.ClipFlags.FLAG_IGNORE_MOTION;
            this.RaisePropertyChanged();
        }
    }

    public ReactiveCommand<Unit, Unit>? DeleteCommand { get; set; }

    public ViewModelActivator Activator { get; }

    public void Delete()
    {
        foreach (CustomManualSelectorGenerator parent in _parents)
        {
            parent.m_generators.Remove(_clipGenerator);
        }
        _parents.Clear();
    }

    public void AddToGraph(List<CustomManualSelectorGenerator> cmsgs)
    {
        foreach (CustomManualSelectorGenerator cmsg in cmsgs)
        {
            cmsg.m_generators.Add(_clipGenerator);
            _parents.Add(cmsg);
        }
    }

    public hkbClipGenerator DuplicateInternal()
    {
        hkbClipGenerator copy = new()
        {
            m_propertyBag = new hkPropertyBag(),
            m_variableBindingSet = _clipGenerator.m_variableBindingSet,
            m_userData = _clipGenerator.m_userData,
            m_name = _clipGenerator.m_name,
            m_animationName = _clipGenerator.m_animationName,
            m_triggers = _clipGenerator.m_triggers,
            m_userPartitionMask = _clipGenerator.m_userPartitionMask,
            m_cropStartAmountLocalTime = _clipGenerator.m_cropStartAmountLocalTime,
            m_cropEndAmountLocalTime = _clipGenerator.m_cropEndAmountLocalTime,
            m_startTime = _clipGenerator.m_startTime,
            m_playbackSpeed = _clipGenerator.m_playbackSpeed,
            m_enforcedDuration = _clipGenerator.m_enforcedDuration,
            m_userControlledTimeFraction = _clipGenerator.m_userControlledTimeFraction,
            m_mode = _clipGenerator.m_mode,
            m_flags = _clipGenerator.m_flags,
            m_animationInternalId = _clipGenerator.m_animationInternalId
        };
        return copy;
    }

    public ClipGeneratorViewModel Duplicate()
    {
        hkbClipGenerator copy = new()
        {
            m_propertyBag = new hkPropertyBag(),
            m_variableBindingSet = _clipGenerator.m_variableBindingSet,
            m_userData = _clipGenerator.m_userData,
            m_name = _clipGenerator.m_name,
            m_animationName = _clipGenerator.m_animationName,
            m_triggers = _clipGenerator.m_triggers,
            m_userPartitionMask = _clipGenerator.m_userPartitionMask,
            m_cropStartAmountLocalTime = _clipGenerator.m_cropStartAmountLocalTime,
            m_cropEndAmountLocalTime = _clipGenerator.m_cropEndAmountLocalTime,
            m_startTime = _clipGenerator.m_startTime,
            m_playbackSpeed = _clipGenerator.m_playbackSpeed,
            m_enforcedDuration = _clipGenerator.m_enforcedDuration,
            m_userControlledTimeFraction = _clipGenerator.m_userControlledTimeFraction,
            m_mode = _clipGenerator.m_mode,
            m_flags = _clipGenerator.m_flags,
            m_animationInternalId = _clipGenerator.m_animationInternalId
        };
        return new ClipGeneratorViewModel(copy, new List<CustomManualSelectorGenerator>(), _history);
    }

    public static ValidationResult? ValidateAnimationName(string? animationName)
    {
        if (animationName is null) return new ValidationResult("Animation name cannot be empty.");
        Regex regex = AnimationNameRegex();
        if (regex.IsMatch(animationName))
        {
            return ValidationResult.Success;
        }
        return new ValidationResult(
            "Invalid animation name format. The animation name must correspond to a valid TAE Id.");
    }

    public static ValidationResult? ValidateTaeIds(string? taeIdsString)
    {
        if (taeIdsString is null) return new ValidationResult("At least one TAE ID must be specified.");
        bool isValid = DupeExtensions.GetTaeIdsFromString(taeIdsString).Count > 0;
        if (isValid) return ValidationResult.Success;
        return new ValidationResult(
            "Invalid TAE ID values. All TAE IDs must be positive integer values.");
    }

    [GeneratedRegex("^a[0-9]{3}_[0-9]{6}$")]
    private static partial Regex AnimationNameRegex();
}