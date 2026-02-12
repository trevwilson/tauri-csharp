using System.Runtime.InteropServices;
using TauriCSharp.Interop;

namespace TauriCSharp;

/// <summary>
/// Provides cross-platform native dialog functionality.
/// </summary>
public static class Dialogs
{
    /// <summary>
    /// Shows a file open dialog and returns the selected path(s).
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="defaultPath">Initial directory or file path.</param>
    /// <param name="allowDirectories">Allow selecting directories.</param>
    /// <param name="allowMultiple">Allow selecting multiple files.</param>
    /// <returns>Array of selected paths, or empty array if cancelled.</returns>
    public static string[] OpenFile(
        string? title = null,
        string? defaultPath = null,
        bool allowDirectories = false,
        bool allowMultiple = false)
    {
        var titlePtr = title != null ? Marshal.StringToCoTaskMemUTF8(title) : IntPtr.Zero;
        var defaultPathPtr = defaultPath != null ? Marshal.StringToCoTaskMemUTF8(defaultPath) : IntPtr.Zero;

        try
        {
            var options = new WryDialogOpenOptions
            {
                Title = titlePtr,
                DefaultPath = defaultPathPtr,
                Filters = IntPtr.Zero,
                FilterCount = 0,
                AllowDirectories = allowDirectories,
                AllowMultiple = allowMultiple
            };

            var selection = WryInterop.DialogOpen(in options);
            var paths = ExtractPaths(selection);
            WryInterop.DialogSelectionFree(selection);
            return paths;
        }
        finally
        {
            if (titlePtr != IntPtr.Zero) Marshal.FreeCoTaskMem(titlePtr);
            if (defaultPathPtr != IntPtr.Zero) Marshal.FreeCoTaskMem(defaultPathPtr);
        }
    }

    /// <summary>
    /// Shows a file save dialog and returns the selected path.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="defaultPath">Initial directory path.</param>
    /// <param name="defaultName">Default file name.</param>
    /// <returns>Selected path, or null if cancelled.</returns>
    public static string? SaveFile(
        string? title = null,
        string? defaultPath = null,
        string? defaultName = null)
    {
        var titlePtr = title != null ? Marshal.StringToCoTaskMemUTF8(title) : IntPtr.Zero;
        var defaultPathPtr = defaultPath != null ? Marshal.StringToCoTaskMemUTF8(defaultPath) : IntPtr.Zero;
        var defaultNamePtr = defaultName != null ? Marshal.StringToCoTaskMemUTF8(defaultName) : IntPtr.Zero;

        try
        {
            var options = new WryDialogSaveOptions
            {
                Title = titlePtr,
                DefaultPath = defaultPathPtr,
                DefaultName = defaultNamePtr,
                Filters = IntPtr.Zero,
                FilterCount = 0
            };

            var selection = WryInterop.DialogSave(in options);
            var paths = ExtractPaths(selection);
            WryInterop.DialogSelectionFree(selection);
            return paths.Length > 0 ? paths[0] : null;
        }
        finally
        {
            if (titlePtr != IntPtr.Zero) Marshal.FreeCoTaskMem(titlePtr);
            if (defaultPathPtr != IntPtr.Zero) Marshal.FreeCoTaskMem(defaultPathPtr);
            if (defaultNamePtr != IntPtr.Zero) Marshal.FreeCoTaskMem(defaultNamePtr);
        }
    }

    /// <summary>
    /// Shows a confirmation dialog with Ok/Cancel buttons.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Message to display.</param>
    /// <param name="level">Message severity level.</param>
    /// <returns>True if user clicked Ok, false if cancelled.</returns>
    public static bool Confirm(
        string title,
        string message,
        MessageLevel level = MessageLevel.Info)
    {
        var titlePtr = Marshal.StringToCoTaskMemUTF8(title);
        var messagePtr = Marshal.StringToCoTaskMemUTF8(message);

        try
        {
            var options = new WryConfirmDialogOptions
            {
                Title = titlePtr,
                Message = messagePtr,
                Level = (WryMessageDialogLevel)level,
                OkLabel = IntPtr.Zero,
                CancelLabel = IntPtr.Zero
            };

            return WryInterop.DialogConfirm(in options);
        }
        finally
        {
            Marshal.FreeCoTaskMem(titlePtr);
            Marshal.FreeCoTaskMem(messagePtr);
        }
    }

    /// <summary>
    /// Shows a Yes/No question dialog.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Question to display.</param>
    /// <param name="level">Message severity level.</param>
    /// <returns>True if user clicked Yes, false if No.</returns>
    public static bool Ask(
        string title,
        string message,
        MessageLevel level = MessageLevel.Info)
    {
        var titlePtr = Marshal.StringToCoTaskMemUTF8(title);
        var messagePtr = Marshal.StringToCoTaskMemUTF8(message);

        try
        {
            var options = new WryAskDialogOptions
            {
                Title = titlePtr,
                Message = messagePtr,
                Level = (WryMessageDialogLevel)level,
                YesLabel = IntPtr.Zero,
                NoLabel = IntPtr.Zero
            };

            return WryInterop.DialogAsk(in options);
        }
        finally
        {
            Marshal.FreeCoTaskMem(titlePtr);
            Marshal.FreeCoTaskMem(messagePtr);
        }
    }

    /// <summary>
    /// Shows a message dialog with customizable buttons.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Message to display.</param>
    /// <param name="level">Message severity level.</param>
    /// <param name="buttons">Button configuration.</param>
    /// <returns>True for Ok/Yes, false for Cancel/No.</returns>
    public static bool Message(
        string title,
        string message,
        MessageLevel level = MessageLevel.Info,
        MessageButtons buttons = MessageButtons.Ok)
    {
        var titlePtr = Marshal.StringToCoTaskMemUTF8(title);
        var messagePtr = Marshal.StringToCoTaskMemUTF8(message);

        try
        {
            var options = new WryMessageDialogOptions
            {
                Title = titlePtr,
                Message = messagePtr,
                Level = (WryMessageDialogLevel)level,
                Buttons = (WryMessageDialogButtons)buttons,
                OkLabel = IntPtr.Zero,
                CancelLabel = IntPtr.Zero,
                YesLabel = IntPtr.Zero,
                NoLabel = IntPtr.Zero
            };

            return WryInterop.DialogMessage(in options);
        }
        finally
        {
            Marshal.FreeCoTaskMem(titlePtr);
            Marshal.FreeCoTaskMem(messagePtr);
        }
    }

    /// <summary>
    /// Shows a text input prompt dialog.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Message/label to display.</param>
    /// <param name="placeholder">Placeholder text for the input field.</param>
    /// <param name="defaultValue">Default value in the input field.</param>
    /// <returns>The entered text, or null if cancelled.</returns>
    public static string? Prompt(
        string title,
        string message,
        string? placeholder = null,
        string? defaultValue = null)
    {
        var titlePtr = Marshal.StringToCoTaskMemUTF8(title);
        var messagePtr = Marshal.StringToCoTaskMemUTF8(message);
        var placeholderPtr = placeholder != null ? Marshal.StringToCoTaskMemUTF8(placeholder) : IntPtr.Zero;
        var defaultValuePtr = defaultValue != null ? Marshal.StringToCoTaskMemUTF8(defaultValue) : IntPtr.Zero;

        try
        {
            var options = new WryPromptDialogOptions
            {
                Title = titlePtr,
                Message = messagePtr,
                Placeholder = placeholderPtr,
                DefaultValue = defaultValuePtr,
                OkLabel = IntPtr.Zero,
                CancelLabel = IntPtr.Zero
            };

            var result = WryInterop.DialogPrompt(in options);
            string? value = null;

            if (result.Accepted && result.Value != IntPtr.Zero)
            {
                value = Marshal.PtrToStringUTF8(result.Value);
            }

            WryInterop.DialogPromptResultFree(result);
            return value;
        }
        finally
        {
            Marshal.FreeCoTaskMem(titlePtr);
            Marshal.FreeCoTaskMem(messagePtr);
            if (placeholderPtr != IntPtr.Zero) Marshal.FreeCoTaskMem(placeholderPtr);
            if (defaultValuePtr != IntPtr.Zero) Marshal.FreeCoTaskMem(defaultValuePtr);
        }
    }

    private static string[] ExtractPaths(WryDialogSelection selection)
    {
        if (selection.Count == 0 || selection.Paths == IntPtr.Zero)
            return [];

        var paths = new string[(int)selection.Count];
        for (var i = 0; i < (int)selection.Count; i++)
        {
            var pathPtr = Marshal.ReadIntPtr(selection.Paths, i * IntPtr.Size);
            paths[i] = Marshal.PtrToStringUTF8(pathPtr) ?? string.Empty;
        }
        return paths;
    }
}

/// <summary>
/// Message dialog severity level.
/// </summary>
public enum MessageLevel
{
    Info = 0,
    Warning = 1,
    Error = 2
}

/// <summary>
/// Message dialog button configuration.
/// </summary>
public enum MessageButtons
{
    Ok = 0,
    OkCancel = 1,
    YesNo = 2,
    YesNoCancel = 3
}
