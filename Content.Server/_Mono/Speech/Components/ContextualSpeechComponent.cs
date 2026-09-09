// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Mono.Speech;

namespace Content.Server._Mono.Speech.Components;

[RegisterComponent]
[ComponentProtoName("contextualSpeech")]
public sealed partial class ContextualSpeechComponent : Component
{
    [DataField]
    public Dictionary<SpeechTrigger, ContextualSpeechTrigger> Triggers { get; private set; } = [];
}
