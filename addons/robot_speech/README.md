# Robot speech profiles

The dialogue scene uses the editable Godot resources in `profiles/`:

- `rgn.tres` controls the upper/left robot.
- `steve.tres` controls the lower/right robot.

Open either resource in the Godot Inspector to tune pitch, pitch range, pace,
brightness, buzz, noise, gain, voice seed, and the weighted phoneme palette.
Palette values are `0` pip, `1` rise, `2` fall, `3` wobble, `4` trill, and
`5` bubble. Repeating a value makes that sound more common.

To add a character, duplicate a profile and assign it to `Upper Voice Profile`
or `Lower Voice Profile` on an intro dialogue scene. The displayed names are
independent from the sound profiles: set `Upper Character Name` and
`Lower Character Name` on that intro dialogue's root. If those fields are
empty, the labels authored in `stage_dialogue.tscn` are preserved. Each
`DialogueLine` also has an `Emotion` field: Neutral, Urgent, Sad, Curious, or
Happy.
