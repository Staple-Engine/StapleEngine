using System;

namespace Staple.Editor;

internal partial class StapleEditor
{
    public void ShowMessageBox(string message, string buttonTitle, Action callback)
    {
        showingMessageBox = true;
        messageBoxMessage = message;

        messageBoxNoTitle = null;

        messageBoxYesTitle = buttonTitle;
        messageBoxYesAction = callback;
    }

    public void ShowMessageBox(string message, string yesTitle, string noTitle, Action onYes, Action onNo)
    {
        showingMessageBox = true;
        messageBoxMessage = message;

        messageBoxYesTitle = yesTitle;
        messageBoxYesAction = onYes;

        messageBoxNoTitle = noTitle;
        messageBoxNoAction = onNo;
    }
}
