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
using ReactiveUI.Fody.Helpers;

// ReSharper disable BitwiseOperatorOnEnumWithoutFlags

namespace ERClipGeneratorTool.ViewModels;

public partial class ClipGeneratorViewModel : ViewModelBase, IActivatableViewModel
{
    private readonly hkbBehaviorGraph _behaviorGraph;
    private readonly hkbClipGenerator _clipGenerator;
    private readonly IHistory _history;
    private readonly List<CustomManualSelectorGenerator> _parents;

    public ClipGeneratorViewModel(hkbBehaviorGraph behaviorGraph, hkbClipGenerator clipGenerator,
        List<CustomManualSelectorGenerator> parents,
        IHistory history)
    {
        _behaviorGraph = behaviorGraph;
        _clipGenerator = clipGenerator;
        _history = history;
        _parents = parents;
        Activator = new ViewModelActivator();
        this.WhenActivated(d =>
        {
            Properties ??= new List<ViewModelBase>
            {
                new PropertyViewModel<float>(behaviorGraph, clipGenerator, x => ref x.m_playbackSpeed, "playbackSpeed",
                    history),
                new PropertyViewModel<float>(behaviorGraph, clipGenerator, x => ref x.m_startTime, "startTime",
                    history),
                new PropertyViewModel<float>(behaviorGraph, clipGenerator, x => ref x.m_cropStartAmountLocalTime,
                    "cropStartAmountLocalTime", history),
                new PropertyViewModel<float>(behaviorGraph, clipGenerator, x => ref x.m_cropEndAmountLocalTime,
                    "cropEndAmountLocalTime", history),
                new PropertyViewModel<float>(behaviorGraph, clipGenerator, x => ref x.m_enforcedDuration,
                    "enforcedDuration", history),
                new PropertyViewModel<float>(behaviorGraph, clipGenerator, x => ref x.m_userControlledTimeFraction,
                    "userControlledTimeFraction", history),
                new PropertyViewModel<uint>(behaviorGraph, clipGenerator, x => ref x.m_userPartitionMask,
                    "userPartitionMask", history)
            };

            DeleteCommand = ReactiveCommand.Create(Delete).DisposeWith(d);
            this.WhenAnyValue(x => x.Name)
                .ObserveWithHistory(value => Name = value ?? "", clipGenerator.m_name, history).DisposeWith(d);
            this.WhenAnyValue(x => x.AnimationName).ObserveWithHistory(value => AnimationName = value ?? "",
                clipGenerator.m_animationName, history).DisposeWith(d);
            this.WhenAnyValue(x => x.AnimationInternalId).ObserveWithHistory(value => AnimationInternalId = value,
                clipGenerator.m_animationInternalId, history).DisposeWith(d);
            this.WhenAnyValue(x => x.Mode).ObserveWithHistory(value => Mode = value,
                clipGenerator.m_mode, history).DisposeWith(d);
            this.WhenAnyValue(x => x.ContinueMotionAtEnd).ObserveWithHistory(value => ContinueMotionAtEnd = value,
                Flags.HasFlag(hkbClipGenerator.ClipFlags.FLAG_CONTINUE_MOTION_AT_END), history).DisposeWith(d);
            this.WhenAnyValue(x => x.SyncHalfCycleInPingPongMode).ObserveWithHistory(
                    value => SyncHalfCycleInPingPongMode = value,
                    Flags.HasFlag(hkbClipGenerator.ClipFlags.FLAG_SYNC_HALF_CYCLE_IN_PING_PONG_MODE), history)
                .DisposeWith(d);
            this.WhenAnyValue(x => x.Mirror).ObserveWithHistory(value => Mirror = value,
                Flags.HasFlag(hkbClipGenerator.ClipFlags.FLAG_MIRROR), history).DisposeWith(d);
            this.WhenAnyValue(x => x.ForceDensePose).ObserveWithHistory(value => ForceDensePose = value,
                Flags.HasFlag(hkbClipGenerator.ClipFlags.FLAG_FORCE_DENSE_POSE), history).DisposeWith(d);
            this.WhenAnyValue(x => x.DontConvertAnnotationsToTriggers).ObserveWithHistory(
                    value => DontConvertAnnotationsToTriggers = value,
                    Flags.HasFlag(hkbClipGenerator.ClipFlags.FLAG_DONT_CONVERT_ANNOTATIONS_TO_TRIGGERS), history)
                .DisposeWith(d);
            this.WhenAnyValue(x => x.IgnoreMotion).ObserveWithHistory(value => IgnoreMotion = value,
                Flags.HasFlag(hkbClipGenerator.ClipFlags.FLAG_IGNORE_MOTION), history).DisposeWith(d);
        });
    }

    public IReadOnlyList<CustomManualSelectorGenerator> Parents => _parents;

    [Reactive] public IReadOnlyCollection<ViewModelBase>? Properties { get; set; }

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
        // not used, keeps track of the number of enum elements
        // hkbClipGenerator.PlaybackMode.MODE_COUNT
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
        hkbClipGenerator copy = DuplicateInternal();
        return new ClipGeneratorViewModel(_behaviorGraph, copy, new List<CustomManualSelectorGenerator>(), _history);
    }


    public static hkbClipGenerator GetDefaultClipGenerator(string animationName)
    {
        return new hkbClipGenerator
        {
            m_propertyBag = new hkPropertyBag(),
            m_variableBindingSet = null,
            m_userData = 0,
            m_name = animationName,
            m_animationName = animationName,
            m_triggers = null,
            m_userPartitionMask = 0,
            m_cropStartAmountLocalTime = 0,
            m_cropEndAmountLocalTime = 0,
            m_startTime = 0,
            m_playbackSpeed = 1,
            m_enforcedDuration = 0,
            m_userControlledTimeFraction = 0,
            m_mode = hkbClipGenerator.PlaybackMode.MODE_SINGLE_PLAY,
            m_flags = 0,
            m_animationInternalId = 0 // must be set by the caller
        };
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
        List<int> taeIdsFromString = DupeExtensions.GetTaeIdsFromString(taeIdsString);
        bool isValid = taeIdsFromString.Count > 0 && taeIdsFromString.All(x => x is >= 0 and <= 999);
        if (isValid) return ValidationResult.Success;
        return new ValidationResult(
            "Invalid TAE ID values. All TAE IDs must be integer values between 0 and 999.");
    }

    [GeneratedRegex("^a[0-9]{3}_[0-9]{6}$")]
    private static partial Regex AnimationNameRegex();
}