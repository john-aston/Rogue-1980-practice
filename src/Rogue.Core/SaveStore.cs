using System.Text.Json;

namespace Rogue.Core;

public sealed class ScoreEntry
{
    public string Name { get; set; } = "";
    public int Score { get; set; }
    public int Level { get; set; }
    public int Gold { get; set; }
    public bool Won { get; set; }
    public string Date { get; set; } = "";
}

/// <summary>存档与排行榜的文件读写。放在 CLI 层，Core 只管数据。</summary>
public sealed class SaveStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
    };

    private readonly string _savePath;
    private readonly string _scorePath;

    public SaveStore(string? directory = null)
    {
        directory ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".rogue1980");

        _savePath = Path.Combine(directory, "save.json");
        _scorePath = Path.Combine(directory, "scores.json");
    }

    public bool TryLoad(out SaveData? data)
    {
        data = null;

        if (!File.Exists(_savePath))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(_savePath);
            data = JsonSerializer.Deserialize<SaveData>(json);

            if (data is null || data.Version != SaveData.CurrentVersion)
            {
                data = null;
                DeleteSave();
                return false;
            }

            return true;
        }
        catch (Exception)
        {
            data = null;
            DeleteSave();
            return false;
        }
    }

    public void Save(SaveData data)
    {
        string? directory = Path.GetDirectoryName(_savePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_savePath, JsonSerializer.Serialize(data, Options));
    }

    public void DeleteSave()
    {
        try
        {
            if (File.Exists(_savePath))
            {
                File.Delete(_savePath);
            }
        }
        catch (Exception)
        {
            // 删不掉就算了，下一次读档会再尝试
        }
    }

    public List<ScoreEntry> AddScore(ScoreEntry entry)
    {
        List<ScoreEntry> scores = LoadScores();
        scores.Add(entry);

        scores = scores
            .OrderByDescending(score => score.Score)
            .Take(10)
            .ToList();

        string? directory = Path.GetDirectoryName(_scorePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_scorePath, JsonSerializer.Serialize(scores, Options));
        return scores;
    }

    private List<ScoreEntry> LoadScores()
    {
        if (!File.Exists(_scorePath))
        {
            return new List<ScoreEntry>();
        }

        try
        {
            string json = File.ReadAllText(_scorePath);
            return JsonSerializer.Deserialize<List<ScoreEntry>>(json) ?? new List<ScoreEntry>();
        }
        catch (Exception)
        {
            return new List<ScoreEntry>();
        }
    }
}
