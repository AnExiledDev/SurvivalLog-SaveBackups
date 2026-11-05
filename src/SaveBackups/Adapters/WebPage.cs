using System;
using ExiledToolkit;
using ExiledToolkit.Game;
using ExiledToolkit.Web;
using SaveBackups.Core;
using ToolkitPage = ExiledToolkit.Web.WebPage;

namespace SaveBackups.Adapters;

/// <summary>
/// The mod's half in the game's web UI: page.js, run in the root document the game's pages sit in, draws the save
/// manager over the title screen. The web view can reload at any time and lose the script, so Exiled's Toolkit checks
/// for it every couple of seconds and the session sends it again only when the page says it is missing.
/// </summary>
internal sealed class WebPage
{
    private const string ScriptResource = "SaveBackups.page.js";
    private const string Api = "window.__slsavebackups";

    private readonly ToolkitPage _page;
    private readonly Action<string> _log;

    public WebPage(Action<string> log)
    {
        _log = log;
        _page = new ToolkitPage(typeof(WebPage).Assembly, ScriptResource, Api, PageCommand.Prefix + "noscript", log);
    }

    /// <summary>True once the script said it is ready, until the page says it lost it.</summary>
    public bool IsReady { get; set; }

    public void KeepInstalled(double now) => _page.KeepInstalled(now);

    public void Install() => _page.Install();

    public void Open(string viewJson) => Run(".open(" + JsText.Literal(viewJson) + ")");

    /// <summary>Redraws the manager if it is still open.</summary>
    public void Show(string viewJson) => Run(".show(" + JsText.Literal(viewJson) + ")");

    public void Close() => Run(".close()");

    /// <summary>The detail view's extra data for one run (stats, items, the game's snapshots, ending photos).</summary>
    public void Detail(string detailJson) => Run(".detail(" + JsText.Literal(detailJson) + ")");

    /// <summary>The files waiting in the Import folder, for the Import dialog.</summary>
    public void Imports(string importsJson) => Run(".imports(" + JsText.Literal(importsJson) + ")");

    /// <summary>The page gets no keys; Esc closes its open dialog, or the manager when none is open.</summary>
    public void Escape() => Run(".escape()");

    /// <summary>Lets the page take clicks again after a step it was waiting on.</summary>
    public void Done() => Run(".done()");

    public void Toast(string text, ToastKind kind = ToastKind.Info, double seconds = 0)
    {
        var request = new ToastRequest(Texts.Source.Pick(GameLanguage.IsChinese()), text) { Kind = kind };

        if (seconds > 0) request = request with { Seconds = seconds };
        if (Toasts.Show(request).IsNone) _log("toast not shown: " + text);
    }

    /// <summary>Calls the page's own function when its script is there; a page without it changes nothing.</summary>
    private void Run(string call) => _page.Run(Api + "&&" + Api + call);
}
