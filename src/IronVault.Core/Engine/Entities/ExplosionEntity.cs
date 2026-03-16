namespace IronVault.Core.Engine.Entities;

public sealed class ExplosionEntity : EntityBase
{
    public float X { get; set; }
    public float Y { get; set; }

    /// <summary>0 = small (bullet hit), 1 = medium, 2 = large (tank destroyed).</summary>
    public int Size { get; set; }

    /// <summary>Animation frame index (0..MaxFrames-1).</summary>
    public int Frame { get; set; }

    public const int MaxFrames = 6;
    public const float FrameDuration = 0.07f;
    public float FrameTimer { get; set; }

    public bool IsFinished => Frame >= MaxFrames;
}
