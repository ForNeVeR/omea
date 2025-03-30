using System;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace CefSharpBrowserControl;

public class CefSharpWebBrowser : AbstractWebBrowser
{
    public override void Navigate(string url)
    {
        throw new NotImplementedException();
    }

    public override void NavigateInPlace(string url)
    {
        throw new NotImplementedException();
    }

    public override void ShowHtml(string html)
    {
        throw new NotImplementedException();
    }

    public override void ShowHtml(string html, WebSecurityContext ctx)
    {
        throw new NotImplementedException();
    }

    public override void ShowHtml(string html, WebSecurityContext ctx, WordPtr[] wordsToHighlight)
    {
        throw new NotImplementedException();
    }

    public override void HighlightWords(WordPtr[] words, int startOffset)
    {
        throw new NotImplementedException();
    }

    public override bool CanExecuteCommand(string command)
    {
        throw new NotImplementedException();
    }

    public override void ExecuteCommand(string command)
    {
        throw new NotImplementedException();
    }

    public override bool ShowImages { get; set; }
    public override string CurrentUrl { get; set; }
    public override string SelectedHtml { get; }
    public override string SelectedText { get; }
    public override string Title { get; }
    public override event EventHandler TitleChanged;
    public override WebSecurityContext SecurityContext { get; set; }
    public override IContextProvider ContextProvider { get; set; }
    public override string Html { get; set; }
    public override AbstractWebBrowser NewInstance()
    {
        throw new NotImplementedException();
    }

    public override BorderStyle BorderStyle { get; set; }
    public override IHtmlDomDocument HtmlDocument { get; }
    public override event KeyEventHandler KeyDown;
    public override event ContextMenuEventHandler ContextMenu;
    public override BrowserReadyState ReadyState { get; }
    public override event DocumentCompleteEventHandler DocumentComplete;
    public override void Stop()
    {
        throw new NotImplementedException();
    }

    public override event BeforeNavigateEventHandler BeforeNavigate;
    public override event BeforeShowHtmlEventHandler BeforeShowHtml;
}
