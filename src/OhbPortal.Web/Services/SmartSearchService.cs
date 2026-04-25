using Microsoft.EntityFrameworkCore;
using OhbPortal.Application.Interfaces;
using OhbPortal.Domain.Entities;

namespace OhbPortal.Web.Services;

public class SmartSearchService
{
    private readonly IApplicationDbContext _db;
    const double K1 = 1.4;
    const double B = 0.75;

    public SmartSearchService(IApplicationDbContext db) => _db = db;

    public async Task<SmartSearchResult> SearchAsync(string query)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var rawTokens = Tokenize(query);
        if (rawTokens.Length == 0) return new SmartSearchResult();

        var expanded = ExpandWithSynonyms(rawTokens);

        var dokumente = await _db.Dokumente
            .Include(d => d.Kapitel)
            .Include(d => d.ErstelltVon)
            .Include(d => d.GeaendertVon)
            .Where(d => !d.Geloescht && !d.Archiviert)
            .ToListAsync();

        var kapitel = await _db.Kapitel.ToListAsync();

        var dokDocs = dokumente.Select(d => new SearchDoc<Dokument>(d, BuildDokFields(d))).ToList();
        var kapDocs = kapitel.Select(k => new SearchDoc<Kapitel>(k, BuildKapFields(k))).ToList();

        var dokResults = RankBm25(dokDocs, expanded)
            .Take(50)
            .Select(r => new ScoredDokument
            {
                Dokument = r.Doc.Entity,
                Score = Math.Round(r.Score, 2),
                MatchedFields = r.MatchedFields,
                MatchMethod = r.MatchMethod
            }).ToList();

        var kapResults = RankBm25(kapDocs, expanded)
            .Take(20)
            .Select(r => new ScoredKapitel
            {
                Kapitel = r.Doc.Entity,
                Score = Math.Round(r.Score, 2),
                MatchedFields = r.MatchedFields,
                MatchMethod = r.MatchMethod
            }).ToList();

        sw.Stop();
        return new SmartSearchResult
        {
            Dokumente = dokResults,
            Kapitel = kapResults,
            ElapsedMs = sw.ElapsedMilliseconds,
            SearchTokens = rawTokens,
            ExpandedTokens = expanded.SelectMany(e => e.Variants).Distinct().ToArray()
        };
    }

    static List<SearchField> BuildDokFields(Dokument d)
    {
        var stripped = StripHtml(d.InhaltHtml);
        return new()
        {
            new("Titel",           d.Titel,            3.0),
            new("Beschreibung",    d.Kurzbeschreibung, 2.5),
            new("Inhalt",          stripped,            1.0),
            new("Kategorie",       d.Kategorie,        2.0),
            new("Tags",            d.Tags,             2.5),
            new("Kapitel",         d.Kapitel?.Titel,    1.5),
            new("Autor",           d.ErstelltVon?.Anzeigename, 1.0),
        };
    }

    static List<SearchField> BuildKapFields(Kapitel k) => new()
    {
        new("Titel",        k.Titel,        3.0),
        new("Beschreibung", k.Beschreibung, 2.0),
    };

    static string? StripHtml(string? html)
    {
        if (string.IsNullOrEmpty(html)) return null;
        return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ")
            .Replace("&nbsp;", " ").Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">");
    }

    // ═══════════════════════════════════════════════════════════════════
    // BM25 RANKING
    // ═══════════════════════════════════════════════════════════════════
    static List<RankedResult<T>> RankBm25<T>(List<SearchDoc<T>> docs, List<ExpandedToken> tokens)
    {
        int N = docs.Count;
        if (N == 0) return new();

        var allFieldNames = docs.SelectMany(d => d.Fields.Select(f => f.Name)).Distinct().ToList();
        var avgFieldLengths = new Dictionary<string, double>();
        foreach (var fn in allFieldNames)
        {
            var lengths = docs.Select(d => d.Fields.FirstOrDefault(f => f.Name == fn)?.TokenCount ?? 0);
            avgFieldLengths[fn] = lengths.Average();
        }

        var docFreq = new Dictionary<string, int>();
        foreach (var et in tokens)
            foreach (var v in et.Variants)
            {
                if (docFreq.ContainsKey(v)) continue;
                docFreq[v] = docs.Count(d => d.Fields.Any(f => f.LowerValue.Contains(v)));
            }

        var results = new List<RankedResult<T>>();
        foreach (var doc in docs)
        {
            double totalScore = 0;
            var matchedFields = new List<string>();
            var methods = new HashSet<string>();
            bool allMatched = true;

            foreach (var et in tokens)
            {
                double bestScore = 0; string? bestField = null; string? bestMethod = null;
                foreach (var field in doc.Fields)
                {
                    if (string.IsNullOrEmpty(field.LowerValue)) continue;
                    double avgDl = avgFieldLengths.GetValueOrDefault(field.Name, 1);
                    foreach (var v in et.Variants)
                    {
                        int tf = CountTf(field.LowerValue, field.Words, v);
                        if (tf == 0) continue;
                        int df = docFreq.GetValueOrDefault(v, 0);
                        double idf = Math.Log((N - df + 0.5) / (df + 0.5) + 1.0);
                        double norm = 1.0 - B + B * (field.TokenCount / Math.Max(avgDl, 1));
                        double tfN = (tf * (K1 + 1.0)) / (tf + K1 * norm);
                        double s = idf * tfN * field.Weight;
                        string m = v == et.Original ? "BM25" : et.IsFuzzy(v) ? "Fuzzy" : "Synonym";
                        if (m == "Synonym") s *= 0.85;
                        if (m == "Fuzzy") s *= 0.7;
                        if (s > bestScore) { bestScore = s; bestField = field.Name; bestMethod = m; }
                    }
                }
                if (bestScore > 0)
                {
                    totalScore += bestScore;
                    if (bestField != null && !matchedFields.Contains(bestField)) matchedFields.Add(bestField);
                    if (bestMethod != null) methods.Add(bestMethod);
                }
                else
                {
                    var fuzzy = FuzzyFallback(doc, et.Original);
                    if (fuzzy.Score > 0)
                    {
                        totalScore += fuzzy.Score * Math.Log((N + 0.5) / 1.5) * 0.5;
                        if (fuzzy.Field != null && !matchedFields.Contains(fuzzy.Field)) matchedFields.Add(fuzzy.Field);
                        methods.Add("Fuzzy");
                    }
                    else allMatched = false;
                }
            }

            if (!allMatched && tokens.Count > 1) continue;
            if (totalScore > 0)
            {
                if (tokens.Count > 1 && allMatched) totalScore *= 1.0 + 0.1 * (tokens.Count - 1);
                results.Add(new RankedResult<T> { Doc = doc, Score = totalScore, MatchedFields = matchedFields, MatchMethod = string.Join(" + ", methods.OrderBy(m => m)) });
            }
        }
        return results.OrderByDescending(r => r.Score).ToList();
    }

    static int CountTf(string lv, string[] words, string term)
    {
        int c = 0;
        foreach (var w in words) if (w == term || w.StartsWith(term)) c++;
        if (c == 0 && lv.Contains(term)) c = 1;
        return c;
    }

    static (double Score, string? Field) FuzzyFallback<T>(SearchDoc<T> doc, string token)
    {
        double best = 0; string? bf = null;
        foreach (var f in doc.Fields)
        {
            if (string.IsNullOrEmpty(f.LowerValue)) continue;
            foreach (var w in f.Words)
            {
                int d = Lev(token, w.Length > token.Length + 2 ? w[..(token.Length + 2)] : w);
                int th = token.Length <= 4 ? 1 : 2;
                if (d <= th) { double s = f.Weight * Math.Max(0.3, 0.6 - d * 0.15); if (s > best) { best = s; bf = f.Name; } break; }
            }
            if (best == 0 && token.Length >= 3) { double sim = Trigram(token, f.LowerValue); if (sim >= 0.35) { double s = f.Weight * sim * 0.4; if (s > best) { best = s; bf = f.Name; } } }
        }
        return (best, bf);
    }

    // ═══════════════════════════════════════════════════════════════════
    // SYNONYMS
    // ═══════════════════════════════════════════════════════════════════
    static readonly Dictionary<string, string[]> Synonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        { "dok",         new[] { "dokument", "dokumentation" } },
        { "dokument",    new[] { "dok", "dokumentation", "unterlage" } },
        { "doku",        new[] { "dokument", "dokumentation" } },
        { "freigabe",    new[] { "genehmigung", "approval" } },
        { "genehmigung", new[] { "freigabe", "approval" } },
        { "kapitel",     new[] { "abschnitt", "thema", "bereich" } },
        { "abschnitt",   new[] { "kapitel", "bereich" } },
        { "entwurf",     new[] { "draft", "konzept" } },
        { "draft",       new[] { "entwurf" } },
        { "freigegeben", new[] { "genehmigt", "approved" } },
        { "version",     new[] { "revision", "ausgabe" } },
        { "anhang",      new[] { "anlage", "attachment", "datei" } },
        { "anlage",      new[] { "anhang", "attachment" } },
        { "prüfung",     new[] { "pruefung", "review", "prüfen" } },
        { "pruefung",    new[] { "prüfung", "review" } },
        { "review",      new[] { "prüfung", "überprüfung" } },
        { "handbuch",    new[] { "manual", "anleitung", "leitfaden" } },
        { "anleitung",   new[] { "handbuch", "manual", "leitfaden" } },
        { "richtlinie",  new[] { "policy", "vorgabe", "regel" } },
        { "policy",      new[] { "richtlinie", "vorgabe" } },
        { "prozess",     new[] { "verfahren", "ablauf", "workflow" } },
        { "verfahren",   new[] { "prozess", "ablauf" } },
        { "workflow",    new[] { "prozess", "ablauf" } },
        { "sicherheit",  new[] { "security", "schutz" } },
        { "security",    new[] { "sicherheit" } },
        { "qualität",    new[] { "qualitaet", "quality" } },
        { "quality",     new[] { "qualität" } },
        { "schulung",    new[] { "training", "weiterbildung" } },
        { "training",    new[] { "schulung", "weiterbildung" } },
    };

    static List<ExpandedToken> ExpandWithSynonyms(string[] tokens)
    {
        var result = new List<ExpandedToken>();
        foreach (var t in tokens)
        {
            var vars = new List<string> { t };
            var synVars = new List<string>();
            if (Synonyms.TryGetValue(t, out var s)) synVars.AddRange(s);
            var norm = t.Replace("ae", "ä").Replace("oe", "ö").Replace("ue", "ü").Replace("ss", "ß");
            if (norm != t) { vars.Add(norm); if (Synonyms.TryGetValue(norm, out var ns)) synVars.AddRange(ns); }
            var exp = t.Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("ß", "ss");
            if (exp != t && exp != norm) vars.Add(exp);
            vars.AddRange(synVars);
            result.Add(new ExpandedToken(t, vars.Distinct().ToList(), synVars));
        }
        return result;
    }

    static string[] Tokenize(string q) => q.ToLowerInvariant()
        .Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(t => t.Length >= 2).ToArray();

    static int Lev(string s, string t)
    {
        if (s.Length == 0) return t.Length; if (t.Length == 0) return s.Length;
        var d = new int[s.Length + 1, t.Length + 1];
        for (int i = 0; i <= s.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= t.Length; j++) d[0, j] = j;
        for (int i = 1; i <= s.Length; i++)
            for (int j = 1; j <= t.Length; j++)
            { int c = s[i - 1] == t[j - 1] ? 0 : 1; d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + c); }
        return d[s.Length, t.Length];
    }

    static double Trigram(string a, string b)
    {
        var ta = Tg(a); var tb = Tg(b);
        if (ta.Count == 0 || tb.Count == 0) return 0;
        return (double)ta.Intersect(tb).Count() / Math.Max(ta.Count, tb.Count);
    }
    static HashSet<string> Tg(string s) { var r = new HashSet<string>(); var p = $"  {s} "; for (int i = 0; i <= p.Length - 3; i++) r.Add(p.Substring(i, 3)); return r; }
}

// ═══════════════════════════════════════════════════════════════════════
public class SearchField
{
    public string Name { get; } public string LowerValue { get; } public string[] Words { get; } public int TokenCount { get; } public double Weight { get; }
    public SearchField(string name, string? value, double weight)
    { Name = name; Weight = weight; LowerValue = (value ?? "").ToLowerInvariant(); Words = LowerValue.Split(new[] { ' ', ',', '.', '-', '/', '(', ')' }, StringSplitOptions.RemoveEmptyEntries); TokenCount = Words.Length; }
}
public class SearchDoc<T> { public T Entity { get; } public List<SearchField> Fields { get; } public SearchDoc(T e, List<SearchField> f) { Entity = e; Fields = f; } }
public class ExpandedToken
{
    public string Original { get; } public List<string> Variants { get; } HashSet<string> _syns;
    public ExpandedToken(string o, List<string> v, List<string> sv) { Original = o; Variants = v; _syns = new(sv, StringComparer.OrdinalIgnoreCase); }
    public bool IsFuzzy(string v) => v != Original && !_syns.Contains(v);
}
public class RankedResult<T> { public SearchDoc<T> Doc { get; set; } = null!; public double Score { get; set; } public List<string> MatchedFields { get; set; } = new(); public string MatchMethod { get; set; } = ""; }

public class SmartSearchResult
{
    public List<ScoredDokument> Dokumente { get; set; } = new();
    public List<ScoredKapitel> Kapitel { get; set; } = new();
    public long ElapsedMs { get; set; }
    public int TotalResults => Dokumente.Count + Kapitel.Count;
    public string[] SearchTokens { get; set; } = Array.Empty<string>();
    public string[] ExpandedTokens { get; set; } = Array.Empty<string>();
}
public class ScoredDokument { public Dokument Dokument { get; set; } = null!; public double Score { get; set; } public List<string> MatchedFields { get; set; } = new(); public string MatchMethod { get; set; } = ""; }
public class ScoredKapitel { public Kapitel Kapitel { get; set; } = null!; public double Score { get; set; } public List<string> MatchedFields { get; set; } = new(); public string MatchMethod { get; set; } = ""; }
