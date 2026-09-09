// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Chat.Systems;
using Content.Server._Mono.Speech.Components;
using Content.Shared._Mono.Speech;
using Content.Shared.Chat;
using Content.Shared.Dataset;
using Content.Shared.Hands;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components; // Goobstation - Contextual speech state guard
using Content.Shared.Throwing;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing; // Goobstation - Contextual speech cooldown

namespace Content.Server._Mono.Speech.EntitySystems;

/// <summary>
/// Handles contextual speech triggered by entity events.
/// </summary>
public sealed class ContextualSpeechSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!; // Goobstation - Contextual speech cooldown

    private readonly Dictionary<ProtoId<LocalizedDatasetPrototype>, LocalizedDatasetPrototype> _cachedDatasets = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ContextualSpeechComponent, SpeechTriggerEvent>(OnSpeechTrigger);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnProtoReload);
        SubscribeLocalEvent<ContextualSpeechComponent, GotEquippedHandEvent>(OnPickedUp);
        SubscribeLocalEvent<ContextualSpeechComponent, ThrowEvent>(OnThrown);
        SubscribeLocalEvent<ContextualSpeechComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnPickedUp(Entity<ContextualSpeechComponent> entity, ref GotEquippedHandEvent args)
    {
        RaiseSpeechTrigger(entity, SpeechTrigger.PickedUp);
    }

    private void OnThrown(Entity<ContextualSpeechComponent> entity, ref ThrowEvent args)
    {
        RaiseSpeechTrigger(entity, SpeechTrigger.Thrown);
    }

    private void OnMobStateChanged(Entity<ContextualSpeechComponent> entity, ref MobStateChangedEvent args)
    {
        var trigger = args.NewMobState switch
        {
            MobState.Critical when args.OldMobState == MobState.Alive => SpeechTrigger.Critical,
            MobState.Dead => SpeechTrigger.Dead,
            MobState.Alive when args.OldMobState is MobState.Critical or MobState.Dead => SpeechTrigger.Revived,
            _ => (SpeechTrigger?) null,
        };

        if (trigger == null)
            return;

        RaiseSpeechTrigger(entity, trigger.Value);
    }

    private void RaiseSpeechTrigger(EntityUid uid, SpeechTrigger trigger)
    {
        var speechEvent = new SpeechTriggerEvent(trigger);
        RaiseLocalEvent(uid, ref speechEvent);
    }

    private void OnSpeechTrigger(Entity<ContextualSpeechComponent> entity, ref SpeechTriggerEvent args)
    {
        if (TryComp<MobStateComponent>(entity, out var mobState) && mobState.CurrentState is MobState.Critical or MobState.Dead) // Goobstation - Contextual speech state guard
            return; // Goobstation - Contextual speech state guard

        if (entity.Comp.NextSpeechTime > _timing.CurTime) // Goobstation - Contextual speech cooldown
            return; // Goobstation - Contextual speech cooldown

        if (!entity.Comp.Triggers.TryGetValue(args.Trigger, out var trigger))
            return;

        if (!_random.Prob(trigger.SpeechChance))
            return;

        if (!_cachedDatasets.TryGetValue(trigger.Dataset, out var dataset))
        {
            if (!_prototypeManager.TryIndex(trigger.Dataset, out dataset))
                return;

            _cachedDatasets[trigger.Dataset] = dataset;
        }

        if (dataset.Values.Count == 0)
            return;

        var message = Loc.GetString(_random.Pick(dataset.Values));
        entity.Comp.NextSpeechTime = _timing.CurTime + entity.Comp.SpeechCooldown; // Goobstation - Contextual speech cooldown
        _chat.TrySendInGameICMessage(entity, message, InGameICChatType.Speak, hideChat: true, ignoreActionBlocker: true);
    }

    private void OnProtoReload(PrototypesReloadedEventArgs args)
    {
        _cachedDatasets.Clear();
    }
}
