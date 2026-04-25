using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OhbPortal.Web.Services;

namespace OhbPortal.Web.Controllers;

[Authorize]
public class SearchController : Controller
{
    private readonly SmartSearchService _search;

    public SearchController(SmartSearchService search) => _search = search;

    public async Task<IActionResult> Index(string? q, string mode = "smart")
    {
        var vm = new SearchViewModel { Query = q, Mode = mode };

        if (!string.IsNullOrWhiteSpace(q))
        {
            vm.Searched = true;
            if (mode != "ai")
            {
                var result = await _search.SearchAsync(q);
                vm.Dokumente = result.Dokumente;
                vm.Kapitel = result.Kapitel;
                vm.ElapsedMs = result.ElapsedMs;
                vm.SearchTokens = result.SearchTokens;
                vm.ExpandedTokens = result.ExpandedTokens;
                vm.MaxDokScore = result.Dokumente.Any() ? result.Dokumente.Max(d => d.Score) : 1;
                vm.MaxKapScore = result.Kapitel.Any() ? result.Kapitel.Max(k => k.Score) : 1;
            }
        }
        return View(vm);
    }
}

public class SearchViewModel
{
    public string? Query { get; set; }
    public string Mode { get; set; } = "smart";
    public bool Searched { get; set; }
    public List<ScoredDokument> Dokumente { get; set; } = new();
    public List<ScoredKapitel> Kapitel { get; set; } = new();
    public long ElapsedMs { get; set; }
    public double MaxDokScore { get; set; }
    public double MaxKapScore { get; set; }
    public string[] SearchTokens { get; set; } = Array.Empty<string>();
    public string[] ExpandedTokens { get; set; } = Array.Empty<string>();
    public int TotalResults => Dokumente.Count + Kapitel.Count;
    public bool HasResults => TotalResults > 0;
}
