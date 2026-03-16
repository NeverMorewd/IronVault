using System.Text.Json;
using System.Text.Json.Serialization;

namespace IronVault.Core.Map;

// AOT-safe JSON serialization via source generators
[JsonSerializable(typeof(MapDto))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class MapSerializerContext : JsonSerializerContext { }

internal sealed class MapDto
{
    public int Cols { get; set; }
    public int Rows { get; set; }
    public byte[] Tiles { get; set; } = [];
}

public static class MapSerializer
{
    public static string Serialize(TileMap map)
    {
        var dto = new MapDto
        {
            Cols = map.Cols,
            Rows = map.Rows,
            Tiles = new byte[map.Cols * map.Rows],
        };
        for (int r = 0; r < map.Rows; r++)
            for (int c = 0; c < map.Cols; c++)
                dto.Tiles[r * map.Cols + c] = (byte)map[c, r];

        return JsonSerializer.Serialize(dto, MapSerializerContext.Default.MapDto);
    }

    public static TileMap Deserialize(string json)
    {
        var dto = JsonSerializer.Deserialize(json, MapSerializerContext.Default.MapDto)
                  ?? throw new InvalidOperationException("Invalid map JSON.");
        var map = new TileMap(dto.Cols, dto.Rows);
        for (int i = 0; i < dto.Tiles.Length; i++)
            map[i % dto.Cols, i / dto.Cols] = (TileType)dto.Tiles[i];
        return map;
    }
}
