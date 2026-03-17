using IronVault.Core.Engine.Entities;
using IronVault.Core.Engine.Systems;
using IronVault.Core.Map;
using System.Linq;

namespace IronVault.Core.Engine;

public enum GameState { NotStarted, Playing, Paused, WaveComplete, Victory, GameOver }

public sealed class GameEngine
{
    // ── State ────────────────────────────────────────────────────────────────
    public GameState    State       { get; private set; } = GameState.NotStarted;
    public TileMap      Map         { get; private set; }
    public AIDifficulty Difficulty  { get; set; } = AIDifficulty.Normal;
    public GameMode     Mode        { get; set; } = GameMode.Classic;

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
    private float       _spawnTimer;
    private const float SpawnInterval = 4f;
    private int         _enemiesSpawned;
    private int         _maxSimultaneousEnemies = 4;
    private WaveScript  _currentScript = WaveScript.ForWave(1);
    private bool        _waveClearFired;   // prevents double-firing WaveCleared event

    private readonly Random _rng = new();

    // ── Events ───────────────────────────────────────────────────────────────
    public event EventHandler<GameState>? StateChanged;
    public event EventHandler<int>?       ScoreChanged;
    /// <summary>Fired when all enemies in the current wave are destroyed. Parameter = wave number.</summary>
    public event EventHandler<int>?       WaveCleared;
    /// <summary>Fired when one or more new bullets are spawned this tick.</summary>
    public event EventHandler? ShotFired;
    /// <summary>Fired when one or more new explosions are created this tick.</summary>
    public event EventHandler? HitOccurred;
    /// <summary>Fired when an enemy tank is destroyed (health reaches zero).</summary>
    public event EventHandler? EnemyDestroyed;
    /// <summary>Fired when the player tank takes damage this tick.</summary>
    public event EventHandler? PlayerHurt;

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

    /// <summary>
    /// Call from the UpgradeView after the player chooses (or skips) an upgrade.
    /// Advances the wave counters and resumes play.
    /// </summary>
    public void ContinueToNextWave()
    {
        if (State != GameState.WaveComplete) return;

        Wave++;
        _currentScript          = GetScript(Wave);
        _enemiesSpawned         = 0;
        _waveClearFired         = false;
        EnemiesLeft             = _currentScript.TotalEnemies;
        TotalEnemies            = _currentScript.TotalEnemies;
        _maxSimultaneousEnemies = _currentScript.MaxSimultaneous;
        _spawnTimer             = 0;

        // Reward: grant an ally tank if the script asks for it
        if (_currentScript.GrantsAlly)
            SpawnAllyReward();

        State = GameState.Playing;
        StateChanged?.Invoke(this, State);
    }

    /// <summary>Apply a chosen upgrade to the player tank immediately.</summary>
    public void ApplyPlayerUpgrade(UpgradeType type)
    {
        if (Player is null || !Player.IsAlive) return;
        switch (type)
        {
            case UpgradeType.ArmorPlating:
                Player.Health.Max += 1;
                Player.Health.Heal(1);
                break;
            case UpgradeType.NitroBoosters:
                Player.Velocity.Speed *= 1.15f;
                break;
            case UpgradeType.RapidFireSystem:
                Player.Weapon.FireCooldown *= 0.80f;
                break;
            case UpgradeType.DualCannon:
                Player.Weapon.MaxBullets = Math.Min(5, Player.Weapon.MaxBullets + 1);
                break;
            case UpgradeType.ArmourPiercing:
                Player.Weapon.Power       = 2;
                Player.Weapon.BulletSpeed = 320f;
                break;
            case UpgradeType.RepairKit:
                Player.Health.Heal(Player.Health.Max);
                break;
        }
    }

    /// <summary>Called every frame from the rendering thread. dt is in seconds.</summary>
    public void Tick(float dt)
    {
        if (State != GameState.Playing) return;

        int prevBullets    = Bullets.Count;
        int prevExplosions = Explosions.Count;
        int prevPlayerHp   = Player?.Health.Current ?? 0;

        // Systems update
        MoveSystem.Update(Tanks, Map, dt);
        WeaponSystem.Update(Tanks, Bullets, dt);
        BulletSystem.Update(Bullets, Tanks, Explosions, Map, dt);
        AISystem.Update(Tanks, Bullets, Map, Difficulty, dt);
        AllyAISystem.Update(Tanks, Map, dt);
        ExplosionSystem.Update(Explosions, dt);

        // Player hurt check (before cleanup removes dead player)
        if (Player is { IsAlive: true } p && p.Health.Current < prevPlayerHp)
            PlayerHurt?.Invoke(this, EventArgs.Empty);

        // Wave spawn
        UpdateWaveSpawn(dt);

        // Dead entity cleanup + score
        CleanupDeadTanks();

        // Win / lose / wave-clear checks
        CheckEndConditions();

        // Sound signals
        if (Bullets.Count    > prevBullets)    ShotFired?.Invoke(this, EventArgs.Empty);
        if (Explosions.Count > prevExplosions) HitOccurred?.Invoke(this, EventArgs.Empty);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private void Reset()
    {
        Map                     = TileMap.CreateDefault();
        Tanks.Clear();
        Bullets.Clear();
        Explosions.Clear();
        PowerUps.Clear();
        Score                   = 0;
        Wave                    = 1;
        Lives                   = 3;
        _currentScript          = GetScript(1);
        EnemiesLeft             = _currentScript.TotalEnemies;
        TotalEnemies            = _currentScript.TotalEnemies;
        _maxSimultaneousEnemies = _currentScript.MaxSimultaneous;
        _enemiesSpawned         = 0;
        _spawnTimer             = 0;
        _waveClearFired         = false;
        AISystem.Reset();
        AllyAISystem.Reset();
    }

    private void SpawnPlayer()
    {
        // Spawn in the clear channel between the two central brick clusters:
        // cols 12-13, row 19 (pixel 288, 456).
        int   midCol = Map.Cols / 2; // 13
        float px     = (midCol - 1) * TileMap.TileSize; // col 12 → x = 288
        float py     = 19 * TileMap.TileSize;            // row 19 → y = 456
        Player = TankEntity.CreatePlayer(px, py);
        Player.Position.Facing = Components.Direction.Up;
        Tanks.Add(Player);
    }

    private void SpawnAllyReward()
    {
        // Place the ally a little left of the player spawn so they don't overlap
        int   midCol = Map.Cols / 2;
        float ax     = (midCol - 3) * TileMap.TileSize; // col 10
        float ay     = 19 * TileMap.TileSize;
        var   ally   = TankEntity.CreateAlly(ax, ay);
        ally.Position.Facing = Components.Direction.Up;
        Tanks.Add(ally);
    }

    private void UpdateWaveSpawn(float dt)
    {
        if (_enemiesSpawned >= _currentScript.TotalEnemies) return;

        int current = 0;
        foreach (var t in Tanks)
            if (t.Team == TankTeam.Enemy && t.IsAlive) current++;

        if (current >= _maxSimultaneousEnemies) return;

        _spawnTimer += dt;
        if (_spawnTimer < SpawnInterval) return;
        _spawnTimer = 0;

        SpawnEnemy();
    }

    private void SpawnEnemy()
    {
        var spawns = new List<(int c, int r)>();
        for (int r = 0; r < Map.Rows; r++)
            for (int c = 0; c < Map.Cols; c++)
                if (Map[c, r] == TileType.Spawn) spawns.Add((c, r));

        if (spawns.Count == 0) return;

        // Only use spawns where the tank has room to move
        var usable = spawns
            .Where(s => SpawnIsUsable(s.c, s.r))
            .OrderBy(_ => _rng.Next())
            .ToList();

        // All spawns blocked → retry next tick (don't advance spawn counter)
        if (usable.Count == 0) return;

        var (sc, sr) = usable[0];
        var tier  = _currentScript.RollTier(_rng);
        var enemy = TankEntity.CreateEnemy(tier, sc * TileMap.TileSize, sr * TileMap.TileSize);
        enemy.Position.Facing = Components.Direction.Down;
        Tanks.Add(enemy);
        _enemiesSpawned++;
    }

    /// <summary>
    /// Returns true when a spawn tile at (col,row) has a clear 2-tile footprint
    /// AND at least one passable step downward, AND no existing tank is too close.
    /// </summary>
    private bool SpawnIsUsable(int col, int row)
    {
        float px = col * TileMap.TileSize;
        float py = row * TileMap.TileSize;

        // Tank footprint must be fully passable
        if (!TankFootprintClear(px, py)) return false;

        // Must be able to move one tile downward (initial facing = Down)
        if (!TankFootprintClear(px, py + TileMap.TileSize)) return false;

        // No other alive tank within 2-tile radius
        float minDist = TankEntity.Size + TileMap.TileSize;
        foreach (var t in Tanks)
        {
            if (!t.IsAlive) continue;
            float dx = MathF.Abs(px - t.Position.X);
            float dy = MathF.Abs(py - t.Position.Y);
            if (dx < minDist && dy < minDist) return false;
        }
        return true;
    }

    /// <summary>Checks that a 48×48 tank body fits entirely on passable tiles.</summary>
    private bool TankFootprintClear(float x, float y)
    {
        const float margin = 1f;
        int size = TankEntity.Size;
        return IsPixelPassable(x + margin,        y + margin)
            && IsPixelPassable(x + size - margin, y + margin)
            && IsPixelPassable(x + margin,        y + size - margin)
            && IsPixelPassable(x + size - margin, y + size - margin);
    }

    private bool IsPixelPassable(float px, float py)
        => Map.IsPassable((int)(px / TileMap.TileSize), (int)(py / TileMap.TileSize));

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
                    EnemyDestroyed?.Invoke(this, EventArgs.Empty);
                }
                Tanks.RemoveAt(i);
            }
        }

        // Prune stale AI state entries
        AISystem.Cleanup(Tanks.Select(t => t.Id));
        AllyAISystem.Cleanup(Tanks.Select(t => t.Id));
    }

    /// <summary>Returns the correct wave script for the current game mode.</summary>
    private WaveScript GetScript(int wave)
        => Mode == GameMode.Defense
               ? DefenseWaveScript.ForWave(wave)
               : WaveScript.ForWave(wave);

    private void AddScore(int points)
    {
        Score += points;
        ScoreChanged?.Invoke(this, Score);
    }

    private void CheckEndConditions()
    {
        // ── Player dead? ─────────────────────────────────────────────────────
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
            SpawnPlayer(); // respawn with 2s invincibility
        }

        // ── Base destroyed? ───────────────────────────────────────────────────
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

        // ── Wave cleared? ─────────────────────────────────────────────────────
        if (!_waveClearFired
            && EnemiesLeft <= 0
            && _enemiesSpawned >= _currentScript.TotalEnemies)
        {
            bool anyEnemy = false;
            foreach (var t in Tanks)
                if (t.Team == TankTeam.Enemy && t.IsAlive) { anyEnemy = true; break; }

            if (!anyEnemy)
            {
                _waveClearFired = true;

                // Defense mode: final wave → go directly to Victory
                if (Mode == GameMode.Defense && Wave >= DefenseWaveScript.TotalWaves)
                {
                    State = GameState.Victory;
                    StateChanged?.Invoke(this, State);
                    return;
                }

                State = GameState.WaveComplete;
                StateChanged?.Invoke(this, State);
                WaveCleared?.Invoke(this, Wave);
            }
        }
    }
}
