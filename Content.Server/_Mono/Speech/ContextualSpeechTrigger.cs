// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Server._Mono.Speech;

[DataDefinition]
public sealed partial class ContextualSpeechTrigger
{
    [DataField(required: true)]
    public ProtoId<LocalizedDatasetPrototype> Dataset { get; private set; }

    [DataField]
    public float SpeechChance { get; private set; } = 1f;
}
