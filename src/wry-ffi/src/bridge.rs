//! JavaScript bridge injection
//!
//! Injects the `window.tauri` object into webviews for IPC communication.

/// The JavaScript bridge code to inject into every webview.
///
/// This creates a `window.tauri` object with methods for:
/// - `invoke(command, payload)` - Send command to backend, returns Promise
/// - `listen(event, callback)` - Listen for events from backend
/// - `__receive(message)` - Internal: receive messages from backend
pub const BRIDGE_SCRIPT: &str = r#"
(function() {
    // Avoid re-initialization
    if (window.__tauriInitialized) return;
    window.__tauriInitialized = true;

    // State
    var nextId = 1;
    var pending = {};
    var listeners = {};

    // The tauri object
    window.tauri = {
        // Send a command to the backend, returns a Promise
        invoke: function(command, payload) {
            return new Promise(function(resolve, reject) {
                var id = nextId++;
                pending[id] = { resolve: resolve, reject: reject };

                var message = JSON.stringify({
                    id: id,
                    command: command,
                    payload: payload || {}
                });

                // Use the IPC mechanism
                if (window.ipc && window.ipc.postMessage) {
                    window.ipc.postMessage(message);
                } else {
                    console.error('tauri: IPC not available');
                    reject(new Error('IPC not available'));
                    delete pending[id];
                }
            });
        },

        // Listen for events from the backend
        listen: function(event, callback) {
            if (!listeners[event]) {
                listeners[event] = [];
            }
            listeners[event].push(callback);

            // Return unsubscribe function
            return function() {
                var idx = listeners[event].indexOf(callback);
                if (idx !== -1) {
                    listeners[event].splice(idx, 1);
                }
            };
        },

        // Emit an event to all listeners (used by backend)
        emit: function(event, payload) {
            var eventListeners = listeners[event] || [];
            for (var i = 0; i < eventListeners.length; i++) {
                try {
                    eventListeners[i](payload);
                } catch (e) {
                    console.error('tauri: listener error:', e);
                }
            }
        },

        // Internal: receive message from backend
        __receive: function(messageStr) {
            try {
                var msg = typeof messageStr === 'string' ? JSON.parse(messageStr) : messageStr;

                if (msg.responseId !== undefined) {
                    // Response to an invoke() call
                    var handler = pending[msg.responseId];
                    if (handler) {
                        delete pending[msg.responseId];
                        if (msg.error) {
                            handler.reject(new Error(msg.error));
                        } else {
                            handler.resolve(msg.payload);
                        }
                    }
                } else if (msg.event) {
                    // Event from backend
                    window.tauri.emit(msg.event, msg.payload);
                }
            } catch (e) {
                console.error('tauri: failed to process message:', e);
            }
        }
    };

    console.log('tauri: bridge initialized');
})();
"#;

/// Get the bridge initialization script
pub fn get_bridge_script() -> &'static str {
    BRIDGE_SCRIPT
}
