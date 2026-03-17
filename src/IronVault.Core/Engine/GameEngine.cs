using IronVault.Core.Engine.Entities;
using IronVault.Core.Engine.Systems;
using IronVault.Core.Map;
using System.Linq;

namespace IronVault.Core.Engine;

public enum GameState { NotStarted, Playing, Paused, Victory, GameOver }

public sealed class GameEngine
{
    // ── State ────────────────────────────────────────────────────────────────
    public GameState State { get; private set; } = GameState.NotStarted;
    public TileMap   Map   { get; private set; }
    public AIDifficulty Difficulty { get; set; } = AIDifficulty.Normal;

    // ── Entity lists ─────────────────────────────────────────────────────────
    public List<TankEntity>      Tanks      { get; } = [];
    public List<BulletEntity>    Bullets    { get; } = [];
    public List<ExplosionEntity> Explosions { get; } = [];
    public List<PowerUpEntity>   PowerUps   { get; } = [];

    // ── Game counters ────────────────────────────────────────────────────────
    public int Score        { get; private set; }
    public int Wave         { get; private set; } = 1;
    public int Lives        { get; private set; } = 3;
    public int EnemiesLeft  { get; private set; }
    public int TotalEnemies { get; private set; }

    // ── Wave management ──────────────────────────────────────────────────────
    private float _spawnTimer;
    private const float SpawnInterval = 4f;
    private const int   EnemiesPerWave = 20;
    private int         _enemiesSpawned;
    private int         _maxSimultaneousEnemies = 4;

    private readonly Random _rng = new();

    // ── Events ───────────────────────────────────────────────────────────────
    public event EventHandler<GameState>? StateChanged;
    public event EventHandler<int>?       ScoreChanged;

    // ── Player ───────────────────────────────────────────────────────────────
    public TankEntity? Player { get; private set; }

    public GameEngine(TileMap? map = null)
    {
        Map = map ?? TileMap.CreateDefault();
    }

    // ── Public API ───────────────────────────────────────────────────────────
    public void StartGame()
    {
        Reset();
        SpawnPlayer();
        State = GameState.Playing;
        StateChanged?.Invoke(this, State);
    }

    public void TogglePause()
    {
        if (State == GameState.Playing)
            State = GameState.Paused;
        else if (State == GameState.Paused)
            State = GameState.Playing;
        StateChanged?.Invoke(this, State);
    }

    /// <summary>Called every frame from the rendering thread. dt is in seconds.</summary>
    public void Tick(float dt)
    {
        if (State != GameState.Playing) return;

        // Systems update
        MoveSystem.Update(Tanks, Map, dt);
        WeaponSystem.Update(Tanks, Bullets, dt);
        BulletSystem.Update(Bullets, Tanks, Explosions, Map, dt);
        AISystem.Update(Tanks, Bullets, Map, Difficulty, dt);
        ExplosionSystem.Update(Explosions, dt);

        // Wave spawn
        UpdateWaveSpawn(dt);

        // Dead enemy cleanup + score
        CleanupDeadTanks();

        // Win/lose checks
        CheckEndConditions();
    }

    // ── Private helpers ──────────────────────────────────────────────────────
    private void Reset()
    {
        Map = TileMap.CreateDefault();
        Tanks.Clear();
        Bullets.Clear();
        Explosions.Clear();
        PowerUps.Clear();
        Score = 0;
        Wave = 1;
        Lives = 3;
        EnemiesLeft = EnemiesPerWave;
        TotalEnemies = EnemiesPerWave;
        _enemiesSpawned = 0;
        _spawnTimer = 0;
        AISystem.Reset(); // clear per-tank AI state for the new game
    }

    private void SpawnPlayer()
    {
        // Spawn in the clear channel between the two central brick clusters:
        // cols 12-13, row 19 (pixel 288, 456). This is the area kept obstacle-free
        // in TileMap.CreateDefault specifically to allow player placement and movement.
        int midCol = Map.Cols / 2; // 13
        float px = (midCol - 1) * TileMap.TileSize; // col 12 → x=288
        float py = 19 * TileMap.TileSize;            // row 19 → y=456
        Player = TankEntity.CreatePlayer(px, py);
        Player.Position.Facing = IronVault.Core.Engine.Components.Direction.Up;
        Tanks.Add(Player);
    }

    private void UpdateWaveSpawn(float dt)
    {
        if (_enemiesSpawned >= EnemiesPerWave) return;

        int currentEnemies = 0;
        foreach (var t in Tanks)
            if (t.Team == TankTeam.Enemy && t.IsAlive) currentEnemies++;

        if (currentEnemies >= _maxSimultaneousEnemies) return;

        _spawnTimer += dt;
        if (_spawnTimer < SpawnInterval) return;
        _spawnTimer = 0;

        SpawnEnemy();
    }

    private void SpawnEnemy()
    {
        // Randomly pick a spawn tile
        var spawns = new List<(int c, int r)>();
        for (int r = 0; r < Map.Rows; r++)
            for (int c = 0; c < Map.Cols; c++)
                if (Map[c, r] == TileType.Spawn) spawns.Add((c, r));

        if (spawns.Count == 0) return;

        var (sc, sr) = spawns[_rng.Next(spawns.Count)];
        var tier = (TankTier)(1 + Math.Min(3, Wave / 3));
        var enemy = TankEntity.CreateEnemy(tier, sc * TileMap.TileSize, sr * TileMap.TileSize);
        // Start moving DOWN from the top spawn points so they don't immediately
        // run into the steel border (row 0) on the first move.
        enemy.Position.Facing = IronVault.Core.Engine.Components.Direction.Down;
        Tanks.Add(enemy);
        _enemiesSpawned++;
    }

    private void CleanupDeadTanks()
    {
        for (int i = Tanks.Count - 1; i >= 0; i--)
        {
            var tank = Tanks[i];
            if (!tank.IsAlive)
            {
                if (tank.Team == TankTeam.Enemy)
                {
                    EnemiesLeft--;
                    AddScore(100 * (int)tank.Tier);
                }
                Tanks.RemoveAt(i);
            }
        }

        // Prune AI state entries for tanks that are no longer alive
        AISystem.Cleanup(Tanks.Select(t => t.Id));
    }

    private void AddScore(int points)
    {
        Score += points;
        ScoreChanged?.Invoke(this, Score);
    }

    private void CheckEndConditions()
    {
        // Player dead?
        bool playerAlive = false;
        foreach (var t in Tanks)
            if (t.IsPlayerControlled && t.IsAlive) { playerAlive = true; break; }

        if (!playerAlive)
        {
            Lives--;
            if (Lives <= 0)
            {
                State = GameState.GameOver;
                StateChanged?.Invoke(this, State);
                return;
            }
            SpawnPlayer(); // respawn
        }

        // Base destroyed?
        bool baseExists = false;
        for (int r = 0; r < Map.Rows; r++)
            for (int c = 0; c < Map.Cols; c++)
                if (Map[c, r] == TileType.Base) { baseExists = true; break; }

        if (!baseExists)
        {
            State = GameState.GameOver;
            StateChanged?.Invoke(this, State);
            return;
        }

        // All enemies cleared?
        if (EnemiesLeft <= 0 && _enemiesSpawned >= EnemiesPerWave)
        {
            bool anyEnemyAlive = false;
            foreach (var t in Tanks)
                if (t.Team == TankTeam.Enemy && t.IsAlive) { anyEnemyAlive = true; break; }

            if (!anyEnemyAlive)
            {
                // Next wave
                Wave++;
                _enemiesSpawned = 0;
                EnemiesLeft = EnemiesPerWave + Wave * 2;
                TotalEnemies = EnemiesLeft;
                _maxSimultaneousEnemies = Math.Min(6, 4 + Wave / 2);
            }
        }
    }
}
