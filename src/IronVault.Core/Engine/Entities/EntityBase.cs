namespace IronVault.Core.Engine.Entities;

public abstract class EntityBase
{
    private static int _nextId;
    public int Id { get; } = System.Threading.Interlocked.Increment(ref _nextId);
    public bool IsAlive { get; set; } = true;
}
