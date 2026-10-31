using System;
using System.IO;
using ExiledToolkit;
using GameCore.HotUpdate;
using GameCore.HotUpdate.ReduxUI;
using SaveBackups.Core;

namespace SaveBackups.Adapters;

/// <summary>
/// The mod's half in the game's web UI: page.js, run in the root document the game's pages sit in, draws the save
/// manager over the title screen. The web view can reload at any time and lose the script, so it is checked for every
/// couple of seconds and sent again only when the page says it is missing.
/// </summary>
internal sealed class WebPage
{
    private const double CheckSeconds = 2;
    private const string ScriptResource = "SaveBackups.page.js";
    private const string Api = "window.__slsavebackups";
    private const string Probe = Api + "||window.vuplex&&window.vuplex.postMessage('" + PageCommand.Prefix + "noscript')";

    private readonly Action<string> _log;
    private string? _script;
    private double _nextCheckAt;
    private bool _hasReportedRunFailure;

    public WebPage(Action<string> log)
    {
        _log = log;
    }

    /// <summary>True once the script said it is ready, until the page says it lost it.</summary>
    public bool IsReady { get; set; }

    public void KeepInstalled(double now)
    {
        if (now < _nextCheckAt) return;

        _nextCheckAt = now + CheckSeconds;
        Run(Probe);
    }

    public void Install()
    {
        if (Run(Script())) _log("page script sent to the web UI");
    }

    public void Open(string viewJson) => Run(Api + "&&" + Api + ".open(" + JsText.Literal(viewJson) + ")");

    /// <summary>Redraws the manager if it is still open.</summary>
    public void Show(string viewJson) => Run(Api + "&&" + Api + ".show(" + JsText.Literal(viewJson) + ")");

    public void Close() => Run(Api + "&&" + Api + ".close()");

    /// <summary>The detail view's extra data for one run (stats, items, the game's snapshots, ending photos).</summary>
    public void Detail(string detailJson) => Run(Api + "&&" + Api + ".detail(" + JsText.Literal(detailJson) + ")");

    /// <summary>The files waiting in the Import folder, for the Import dialog.</summary>
    public void Imports(string importsJson) => Run(Api + "&&" + Api + ".imports(" + JsText.Literal(importsJson) + ")");

    /// <summary>The page gets no keys; Esc closes its open dialog, or the manager when none is open.</summary>
    public void Escape() => Run(Api + "&&" + Api + ".escape()");

    /// <summary>Lets the page take clicks again after a step it was waiting on.</summary>
    public void Done() => Run(Api + "&&" + Api + ".done()");

    public void Toast(string text, ToastKind kind = ToastKind.Info, double seconds = 0)
    {
        var request = new ToastRequest(Texts.Source.Pick(GameLanguage.IsChinese()), text) { Kind = kind };

        if (seconds > 0) request = request with { Seconds = seconds };
        if (Toasts.Show(request).IsNone) _log("toast not shown: " + text);
    }

    private bool Run(string javaScript)
    {
        var webView = GameSingleton.Existing<ReduxUISystem>()?.GetWebUILayer()?.canvasWebViewPrefab?.WebView;

        if (webView == null) return false;

        try
        {
            webView.ExecuteJavaScript(javaScript, null);

            return true;
        }
        catch (Exception ex)
        {
            if (!_hasReportedRunFailure) _log("could not run script in the web UI (reported once): " + ex.Message);

            _hasReportedRunFailure = true;

            return false;
        }
    }

    private string Script()
    {
        if (_script != null) return _script;

        using var stream = typeof(WebPage).Assembly.GetManifestResourceStream(ScriptResource) ?? throw new InvalidOperationException(ScriptResource + " is not embedded");
        using var reader = new StreamReader(stream);

        return _script = reader.ReadToEnd();
    }
}
