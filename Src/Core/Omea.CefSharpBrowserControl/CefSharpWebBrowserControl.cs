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

    public override bool ShowImages
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public override string CurrentUrl
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public override string SelectedHtml => throw new NotImplementedException();

    public override string SelectedText => throw new NotImplementedException();

    public override string Title => throw new NotImplementedException();

    public override event EventHandler TitleChanged
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    public override WebSecurityContext SecurityContext
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public override IContextProvider ContextProvider
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public override string Html
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public override AbstractWebBrowser NewInstance()
    {
        throw new NotImplementedException();
    }

    public override BorderStyle BorderStyle
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public override IHtmlDomDocument HtmlDocument => throw new NotImplementedException();

    public override event KeyEventHandler KeyDown
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    public override event ContextMenuEventHandler ContextMenu
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    public override BrowserReadyState ReadyState => throw new NotImplementedException();

    public override event DocumentCompleteEventHandler DocumentComplete
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    public override void Stop()
    {
        throw new NotImplementedException();
    }

    public override event BeforeNavigateEventHandler BeforeNavigate
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    public override event BeforeShowHtmlEventHandler BeforeShowHtml
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }
}
