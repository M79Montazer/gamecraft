using Godot;

// Inspector-editable identity for one robot voice. Duplicate a profile .tres
// to create a new character without changing the speech renderer.
[GlobalClass]
public partial class RobotVoiceProfile : Resource
{
    [ExportGroup("Identity")]
    [Export] public string DisplayName { get; set; } = "Robot";
    [Export] public int VoiceSeed { get; set; } = 1;
    [Export] public int[] PhonemePalette { get; set; } = new[] { 0, 1, 2, 3 };

    [ExportGroup("Sound")]
    [Export(PropertyHint.Range, "90,520,1")]
    public float BasePitchHz { get; set; } = 240.0f;

    [Export(PropertyHint.Range, "0,12,0.1")]
    public float PitchRangeSemitones { get; set; } = 5.0f;

    [Export(PropertyHint.Range, "0.5,2,0.01")]
    public float Pace { get; set; } = 1.0f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float Brightness { get; set; } = 0.45f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float Buzz { get; set; } = 0.18f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float Noise { get; set; } = 0.035f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float Gain { get; set; } = 0.58f;
}
