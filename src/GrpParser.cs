namespace GoodRP;

public class GrpScript
{
    public string? Event { get; set; }
    public int TimeoutMs { get; set; } = 10000;
    public string? Language { get; set; }
    public string? WebhookUrl { get; set; }
    public string? Template { get; set; }
    public bool Embed { get; set; }
    public bool Log { get; set; }
    public string? LogFile { get; set; }
    public string CodeBody { get; set; } = "";
}

public static class GrpParser
{
    public static GrpScript Parse(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        var script = new GrpScript();
        var codeLines = new List<string>();
        bool inCodeBlock = false;

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith("#") || string.IsNullOrWhiteSpace(trimmed))
            {
                if (inCodeBlock) codeLines.Add(line);
                continue;
            }

            if (trimmed.StartsWith("@"))
            {
                var spaceIdx = trimmed.IndexOf(' ');
                var directive = spaceIdx > 0 ? trimmed[..spaceIdx].ToLower() : trimmed.ToLower();
                var value = spaceIdx > 0 ? trimmed[(spaceIdx + 1)..].Trim() : "";

                switch (directive)
                {
                    case "@event":
                        script.Event = value;
                        break;
                    case "@timeout":
                        script.TimeoutMs = int.TryParse(value, out var t) ? t : 10000;
                        break;
                    case "@language":
                        script.Language = value.ToLower();
                        inCodeBlock = true;
                        break;
                    case "@webhook":
                        script.WebhookUrl = value;
                        break;
                    case "@template":
                        script.Template = value;
                        break;
                    case "@embed":
                        script.Embed = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                        break;
                    case "@log":
                        script.Log = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                        break;
                    case "@logfile":
                        script.LogFile = value;
                        break;
                }
            }
            else if (inCodeBlock)
            {
                codeLines.Add(line);
            }
        }

        script.CodeBody = string.Join(Environment.NewLine, codeLines).Trim();
        return script;
    }
}
