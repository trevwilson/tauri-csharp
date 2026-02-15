//! Native notification support via notify-rust
//!
//! Provides FFI functions for showing desktop notifications.

use crate::helpers::*;
use crate::types::*;

// ============================================================================
// Notification Functions
// ============================================================================

/// Show a desktop notification.
///
/// Returns true on success, false on failure.
/// On platforms where notifications aren't available, fails gracefully.
#[no_mangle]
pub extern "C" fn wry_notification_show(options: *const WryNotificationOptions) -> bool {
    if options.is_null() {
        return false;
    }

    guard_panic_bool(|| {
        let opts = unsafe { &*options };

        let title = opt_cstring(opts.title).unwrap_or_default();
        let body = opt_cstring(opts.body).unwrap_or_default();
        let icon = opt_cstring(opts.icon);

        let mut notification = notify_rust::Notification::new();
        notification.summary(&title).body(&body);

        if let Some(icon_path) = icon {
            notification.icon(&icon_path);
        }

        // Timeout: -1 = default, 0 = never, >0 = milliseconds
        match opts.timeout_ms {
            -1 => {} // use default
            0 => {
                notification.timeout(notify_rust::Timeout::Never);
            }
            ms if ms > 0 => {
                notification.timeout(notify_rust::Timeout::Milliseconds(ms as u32));
            }
            _ => {} // negative values other than -1: use default
        }

        // Urgency (Linux-specific, ignored on other platforms)
        #[cfg(target_os = "linux")]
        {
            let urgency = match opts.urgency {
                WryNotificationUrgency::Low => notify_rust::Urgency::Low,
                WryNotificationUrgency::Normal => notify_rust::Urgency::Normal,
                WryNotificationUrgency::Critical => notify_rust::Urgency::Critical,
            };
            notification.urgency(urgency);
        }

        match notification.show() {
            Ok(_) => true,
            Err(e) => {
                log::warn!("Failed to show notification: {e}");
                false
            }
        }
    })
}
