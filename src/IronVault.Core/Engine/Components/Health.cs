namespace IronVault.Core.Engine.Components;

public sealed class Health
{
    public int Max { get; set; }
    public int Current { get; set; }
    public bool IsAlive => Current > 0;

    public Health(int max)
    {
        Max = max;
        Current = max;
    }

    public void TakeDamage(int amount) => Current = Math.Max(0, Current - amount);
    public void Heal(int amount) => Current = Math.Min(Max, Current + amount);
}
