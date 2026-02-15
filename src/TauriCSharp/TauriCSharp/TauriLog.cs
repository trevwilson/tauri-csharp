using Microsoft.Extensions.Logging;

namespace TauriCSharp;

/// <summary>
/// High-performance logging using source generators.
/// See: https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/high-performance-logging
/// </summary>
public static partial class TauriLog
{
    // ========================================================================
    // Debug - API Method Calls (EventId 1-99)
    // ========================================================================

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "[{WindowTitle}] Load({Uri})")]
    public static partial void Load(ILogger logger, string windowTitle, string uri);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "[{WindowTitle}] LoadRawString({ContentPreview})")]
    public static partial void LoadRawString(ILogger logger, string windowTitle, string contentPreview);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "[{WindowTitle}] Center()")]
    public static partial void Center(ILogger logger, string windowTitle);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "[{WindowTitle}] MoveTo({Location}, allowOutsideWorkArea={AllowOutsideWorkArea})")]
    public static partial void MoveTo(ILogger logger, string windowTitle, string location, bool allowOutsideWorkArea);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "[{WindowTitle}] Offset({Offset})")]
    public static partial void Offset(ILogger logger, string windowTitle, string offset);

    [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetChromeless({Chromeless})")]
    public static partial void SetChromeless(ILogger logger, string windowTitle, bool chromeless);

    [LoggerMessage(EventId = 7, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetTransparent({Enabled})")]
    public static partial void SetTransparent(ILogger logger, string windowTitle, bool enabled);

    [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetContextMenuEnabled({Enabled})")]
    public static partial void SetContextMenuEnabled(ILogger logger, string windowTitle, bool enabled);

    [LoggerMessage(EventId = 9, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetDevTools({Enabled})")]
    public static partial void SetDevTools(ILogger logger, string windowTitle, bool enabled);

    [LoggerMessage(EventId = 10, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetFullScreen({FullScreen})")]
    public static partial void SetFullScreen(ILogger logger, string windowTitle, bool fullScreen);

    [LoggerMessage(EventId = 11, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetGrantBrowserPermission({Grant})")]
    public static partial void SetGrantBrowserPermission(ILogger logger, string windowTitle, bool grant);

    [LoggerMessage(EventId = 12, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetUserAgent({UserAgent})")]
    public static partial void SetUserAgent(ILogger logger, string windowTitle, string userAgent);

    [LoggerMessage(EventId = 13, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetBrowserControlInitParameters({Parameters})")]
    public static partial void SetBrowserControlInitParameters(ILogger logger, string windowTitle, string parameters);

    [LoggerMessage(EventId = 14, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetNotificationRegistrationId({RegistrationId})")]
    public static partial void SetNotificationRegistrationId(ILogger logger, string windowTitle, string registrationId);

    [LoggerMessage(EventId = 15, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetMediaAutoplayEnabled({Enable})")]
    public static partial void SetMediaAutoplayEnabled(ILogger logger, string windowTitle, bool enable);

    [LoggerMessage(EventId = 16, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetFileSystemAccessEnabled({Enable})")]
    public static partial void SetFileSystemAccessEnabled(ILogger logger, string windowTitle, bool enable);

    [LoggerMessage(EventId = 17, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetWebSecurityEnabled({Enable})")]
    public static partial void SetWebSecurityEnabled(ILogger logger, string windowTitle, bool enable);

    [LoggerMessage(EventId = 18, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetJavascriptClipboardAccessEnabled({Enable})")]
    public static partial void SetJavascriptClipboardAccessEnabled(ILogger logger, string windowTitle, bool enable);

    [LoggerMessage(EventId = 19, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetMediaStreamEnabled({Enable})")]
    public static partial void SetMediaStreamEnabled(ILogger logger, string windowTitle, bool enable);

    [LoggerMessage(EventId = 20, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetSmoothScrollingEnabled({Enable})")]
    public static partial void SetSmoothScrollingEnabled(ILogger logger, string windowTitle, bool enable);

    [LoggerMessage(EventId = 21, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetIgnoreCertificateErrorsEnabled({Enable})")]
    public static partial void SetIgnoreCertificateErrorsEnabled(ILogger logger, string windowTitle, bool enable);

    [LoggerMessage(EventId = 22, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetNotificationsEnabled({Enable})")]
    public static partial void SetNotificationsEnabled(ILogger logger, string windowTitle, bool enable);

    [LoggerMessage(EventId = 23, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetHeight({Height})")]
    public static partial void SetHeight(ILogger logger, string windowTitle, int height);

    [LoggerMessage(EventId = 24, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetIconFile({IconFile})")]
    public static partial void SetIconFile(ILogger logger, string windowTitle, string iconFile);

    [LoggerMessage(EventId = 25, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetLeft({Left})")]
    public static partial void SetLeft(ILogger logger, string windowTitle, int left);

    [LoggerMessage(EventId = 26, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetResizable({Resizable})")]
    public static partial void SetResizable(ILogger logger, string windowTitle, bool resizable);

    [LoggerMessage(EventId = 27, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetSize({Width}x{Height})")]
    public static partial void SetSize(ILogger logger, string windowTitle, int width, int height);

    [LoggerMessage(EventId = 28, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetLocation({Location})")]
    public static partial void SetLocation(ILogger logger, string windowTitle, string location);

    [LoggerMessage(EventId = 29, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetMaximized({Maximized})")]
    public static partial void SetMaximized(ILogger logger, string windowTitle, bool maximized);

    [LoggerMessage(EventId = 30, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetMaxSize({MaxWidth}x{MaxHeight})")]
    public static partial void SetMaxSize(ILogger logger, string windowTitle, int maxWidth, int maxHeight);

    [LoggerMessage(EventId = 31, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetMaxHeight({MaxHeight})")]
    public static partial void SetMaxHeight(ILogger logger, string windowTitle, int maxHeight);

    [LoggerMessage(EventId = 32, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetMaxWidth({MaxWidth})")]
    public static partial void SetMaxWidth(ILogger logger, string windowTitle, int maxWidth);

    [LoggerMessage(EventId = 33, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetMinimized({Minimized})")]
    public static partial void SetMinimized(ILogger logger, string windowTitle, bool minimized);

    [LoggerMessage(EventId = 34, Level = LogLevel.Debug, Message = "[{WindowTitle}] Restore()")]
    public static partial void Restore(ILogger logger, string windowTitle);

    [LoggerMessage(EventId = 35, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetMinSize({MinWidth}x{MinHeight})")]
    public static partial void SetMinSize(ILogger logger, string windowTitle, int minWidth, int minHeight);

    [LoggerMessage(EventId = 36, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetMinHeight({MinHeight})")]
    public static partial void SetMinHeight(ILogger logger, string windowTitle, int minHeight);

    [LoggerMessage(EventId = 37, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetMinWidth({MinWidth})")]
    public static partial void SetMinWidth(ILogger logger, string windowTitle, int minWidth);

    [LoggerMessage(EventId = 38, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetTemporaryFilesPath({TempFilesPath})")]
    public static partial void SetTemporaryFilesPath(ILogger logger, string windowTitle, string tempFilesPath);

    [LoggerMessage(EventId = 39, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetTitle({Title})")]
    public static partial void SetTitle(ILogger logger, string windowTitle, string title);

    [LoggerMessage(EventId = 40, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetTop({Top})")]
    public static partial void SetTop(ILogger logger, string windowTitle, int top);

    [LoggerMessage(EventId = 41, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetTopMost({TopMost})")]
    public static partial void SetTopMost(ILogger logger, string windowTitle, bool topMost);

    [LoggerMessage(EventId = 42, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetWidth({Width})")]
    public static partial void SetWidth(ILogger logger, string windowTitle, int width);

    [LoggerMessage(EventId = 43, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetZoom({Zoom})")]
    public static partial void SetZoom(ILogger logger, string windowTitle, int zoom);

    [LoggerMessage(EventId = 44, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetUseOsDefaultLocation({UseOsDefault})")]
    public static partial void SetUseOsDefaultLocation(ILogger logger, string windowTitle, bool useOsDefault);

    [LoggerMessage(EventId = 45, Level = LogLevel.Debug, Message = "[{WindowTitle}] SetUseOsDefaultSize({UseOsDefault})")]
    public static partial void SetUseOsDefaultSize(ILogger logger, string windowTitle, bool useOsDefault);

    [LoggerMessage(EventId = 46, Level = LogLevel.Debug, Message = "[{WindowTitle}] Close()")]
    public static partial void Close(ILogger logger, string windowTitle);

    [LoggerMessage(EventId = 47, Level = LogLevel.Debug, Message = "[{WindowTitle}] SendWebMessage({Message})")]
    public static partial void SendWebMessage(ILogger logger, string windowTitle, string message);

    [LoggerMessage(EventId = 48, Level = LogLevel.Debug, Message = "[{WindowTitle}] ExecuteScript({Script})")]
    public static partial void ExecuteScript(ILogger logger, string windowTitle, string script);

    [LoggerMessage(EventId = 49, Level = LogLevel.Debug, Message = "[{WindowTitle}] OpenDevTools()")]
    public static partial void OpenDevTools(ILogger logger, string windowTitle);

    [LoggerMessage(EventId = 50, Level = LogLevel.Debug, Message = "[{WindowTitle}] CloseDevTools()")]
    public static partial void CloseDevTools(ILogger logger, string windowTitle);

    [LoggerMessage(EventId = 51, Level = LogLevel.Debug, Message = "[{WindowTitle}] Show()")]
    public static partial void Show(ILogger logger, string windowTitle);

    [LoggerMessage(EventId = 52, Level = LogLevel.Debug, Message = "[{WindowTitle}] Hide()")]
    public static partial void Hide(ILogger logger, string windowTitle);

    [LoggerMessage(EventId = 53, Level = LogLevel.Debug, Message = "[{WindowTitle}] Focus()")]
    public static partial void Focus(ILogger logger, string windowTitle);

    [LoggerMessage(EventId = 54, Level = LogLevel.Debug, Message = "[{WindowTitle}] SendNotification({Title}, {Body})")]
    public static partial void SendNotification(ILogger logger, string windowTitle, string title, string body);

    [LoggerMessage(EventId = 55, Level = LogLevel.Debug, Message = "[{WindowTitle}] InvokingTransparentEnabled({Value})")]
    public static partial void InvokingTransparentEnabled(ILogger logger, string windowTitle, bool value);

    // ========================================================================
    // Warning - Feature Limitations (EventId 100-199)
    // ========================================================================

    [LoggerMessage(EventId = 100, Level = LogLevel.Warning, Message = "[{WindowTitle}] Centering window after creation is not supported with wry-ffi backend")]
    public static partial void CenteringNotSupported(ILogger logger, string windowTitle);

    [LoggerMessage(EventId = 101, Level = LogLevel.Warning, Message = "[{WindowTitle}] Transparent cannot be changed after window creation with wry-ffi backend")]
    public static partial void TransparentCannotChange(ILogger logger, string windowTitle);

    [LoggerMessage(EventId = 102, Level = LogLevel.Warning, Message = "[{WindowTitle}] ContextMenuEnabled cannot be changed after window creation with wry-ffi backend")]
    public static partial void ContextMenuCannotChange(ILogger logger, string windowTitle);

    [LoggerMessage(EventId = 103, Level = LogLevel.Warning, Message = "[{WindowTitle}] DevToolsEnabled cannot be changed after window creation with wry-ffi backend")]
    public static partial void DevToolsCannotChange(ILogger logger, string windowTitle);

    [LoggerMessage(EventId = 104, Level = LogLevel.Warning, Message = "[{WindowTitle}] Icon file not supported in wry-ffi backend")]
    public static partial void IconFileNotSupported(ILogger logger, string windowTitle);

    [LoggerMessage(EventId = 105, Level = LogLevel.Warning, Message = "[{WindowTitle}] SetMaxSize after creation not supported in wry-ffi")]
    public static partial void MaxSizeNotSupported(ILogger logger, string windowTitle);

    [LoggerMessage(EventId = 106, Level = LogLevel.Warning, Message = "[{WindowTitle}] SetMinSize after creation not supported in wry-ffi")]
    public static partial void MinSizeNotSupported(ILogger logger, string windowTitle);

    [LoggerMessage(EventId = 107, Level = LogLevel.Warning, Message = "[{WindowTitle}] Resizable cannot be changed after window creation with wry-ffi backend")]
    public static partial void ResizableCannotChange(ILogger logger, string windowTitle);

    [LoggerMessage(EventId = 108, Level = LogLevel.Warning, Message = "[{WindowTitle}] Topmost cannot be changed after window creation with wry-ffi backend")]
    public static partial void TopmostCannotChange(ILogger logger, string windowTitle);

    [LoggerMessage(EventId = 109, Level = LogLevel.Warning, Message = "[{WindowTitle}] DevTools were not enabled at window creation time")]
    public static partial void DevToolsNotEnabled(ILogger logger, string windowTitle);

    [LoggerMessage(EventId = 110, Level = LogLevel.Warning, Message = "[{WindowTitle}] CloseDevTools not available in wry-ffi backend")]
    public static partial void CloseDevToolsNotAvailable(ILogger logger, string windowTitle);

    [LoggerMessage(EventId = 111, Level = LogLevel.Warning, Message = "[{WindowTitle}] ClearBrowserAutoFill not supported with wry-ffi backend")]
    public static partial void ClearBrowserAutoFillNotSupported(ILogger logger, string windowTitle);

    [LoggerMessage(EventId = 112, Level = LogLevel.Warning, Message = "[{WindowTitle}] WebView2 runtime path not applicable with wry-ffi backend")]
    public static partial void WebView2PathNotApplicable(ILogger logger, string windowTitle);

    [LoggerMessage(EventId = 113, Level = LogLevel.Warning, Message = "[{WindowTitle}] ClearBrowserAutoFill is only supported on the Windows platform")]
    public static partial void ClearBrowserAutoFillWindowsOnly(ILogger logger, string windowTitle);

    [LoggerMessage(EventId = 114, Level = LogLevel.Warning, Message = "[{WindowTitle}] Win32SetWebView2Path is only supported on the Windows platform")]
    public static partial void WebView2PathWindowsOnly(ILogger logger, string windowTitle);

    [LoggerMessage(EventId = 115, Level = LogLevel.Information, Message = "[{WindowTitle}] DevTools controlled via F12/context menu when devtools enabled at creation time")]
    public static partial void DevToolsControlNote(ILogger logger, string windowTitle);

    // ========================================================================
    // Error - Exceptions and Failures (EventId 200-299)
    // ========================================================================

    [LoggerMessage(EventId = 200, Level = LogLevel.Error, Message = "[{WindowTitle}] File not found: {Path}")]
    public static partial void FileNotFound(ILogger logger, string windowTitle, string path);

    [LoggerMessage(EventId = 201, Level = LogLevel.Error, Message = "[{WindowTitle}] Protocol handler error: {ErrorMessage}")]
    public static partial void ProtocolHandlerError(ILogger logger, string windowTitle, string errorMessage);

    [LoggerMessage(EventId = 202, Level = LogLevel.Error, Message = "[{WindowTitle}] Event handling error: {ErrorMessage}")]
    public static partial void EventHandlingError(ILogger logger, string windowTitle, string errorMessage);

    [LoggerMessage(EventId = 203, Level = LogLevel.Error, Message = "[{WindowTitle}] JSON parse error: {ErrorMessage}")]
    public static partial void JsonParseError(ILogger logger, string windowTitle, string errorMessage);

    [LoggerMessage(EventId = 204, Level = LogLevel.Error, Message = "[{WindowTitle}] Native error: {ErrorMessage} (Error #{ErrorCode})")]
    public static partial void NativeError(ILogger logger, string windowTitle, string errorMessage, int errorCode, Exception exception);

    [LoggerMessage(EventId = 205, Level = LogLevel.Error, Message = "[{WindowTitle}] TauriException: {ErrorMessage} (Error code: {ErrorCode})")]
    public static partial void TauriError(ILogger logger, string windowTitle, string errorMessage, int errorCode, Exception exception);

    // ========================================================================
    // Trace - Detailed Debugging (EventId 300-399)
    // ========================================================================

    [LoggerMessage(EventId = 300, Level = LogLevel.Trace, Message = "[{WindowTitle}] Unhandled event: {EventType}")]
    public static partial void UnhandledEvent(ILogger logger, string windowTitle, string eventType);

    [LoggerMessage(EventId = 301, Level = LogLevel.Trace, Message = "[{WindowTitle}] MoveTo details - Current: {CurrentLocation}, New: {NewLocation}")]
    public static partial void MoveToDetails(ILogger logger, string windowTitle, string currentLocation, string newLocation);
}
