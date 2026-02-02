//! Event serialization for the event loop callback
//!
//! Converts tao events to JSON strings for the callback.

use serde::Serialize;
use serde_json::json;
#[cfg(target_os = "macos")]
use serde_json::Map;
use tao::event::{
    ElementState, Event, MouseButton, MouseScrollDelta,
    WindowEvent as TaoWindowEvent,
};
use tao::keyboard::ModifiersState;

use crate::types::WryUserEvent;

#[cfg(target_os = "macos")]
use crate::types::{WryTrayEvent, WryTrayEventKind};

// ============================================================================
// Event Payload Types
// ============================================================================

#[derive(Serialize)]
struct EventPosition {
    x: f64,
    y: f64,
}

#[derive(Serialize)]
struct EventSize {
    width: f64,
    height: f64,
}

#[derive(Serialize)]
struct EventModifiers {
    shift: bool,
    control: bool,
    alt: bool,
    super_key: bool,
}

fn modifiers_payload(modifiers: ModifiersState) -> EventModifiers {
    EventModifiers {
        shift: modifiers.shift_key(),
        control: modifiers.control_key(),
        alt: modifiers.alt_key(),
        super_key: modifiers.super_key(),
    }
}

// ============================================================================
// Event Serialization
// ============================================================================

pub fn serialize_event(event: &Event<WryUserEvent>) -> String {
    let value = match event {
        Event::NewEvents(cause) => json!({
            "type": "new-events",
            "cause": format!("{:?}", cause),
        }),
        Event::MainEventsCleared => json!({ "type": "main-events-cleared" }),
        Event::RedrawEventsCleared => json!({ "type": "redraw-events-cleared" }),
        Event::LoopDestroyed => json!({ "type": "loop-destroyed" }),
        Event::Suspended => json!({ "type": "suspended" }),
        Event::Resumed => json!({ "type": "resumed" }),
        Event::RedrawRequested(window_id) => json!({
            "type": "window-redraw-requested",
            "window_id": format!("{window_id:?}"),
        }),
        Event::UserEvent(WryUserEvent::Exit) => json!({ "type": "user-exit" }),
        Event::UserEvent(WryUserEvent::Custom(payload)) => json!({
            "type": "user-event",
            "payload": payload,
        }),
        #[cfg(target_os = "macos")]
        Event::UserEvent(WryUserEvent::Menu(menu_id)) => json!({
            "type": "menu-event",
            "menu_id": menu_id,
        }),
        #[cfg(target_os = "macos")]
        Event::UserEvent(WryUserEvent::Tray(event)) => serialize_tray_event(event),
        Event::DeviceEvent {
            device_id, event, ..
        } => json!({
            "type": "device-event",
            "device_id": format!("{device_id:?}"),
            "event": format!("{:?}", event),
        }),
        Event::Opened { urls } => json!({
            "type": "opened",
            "urls": urls.iter().map(|u| u.to_string()).collect::<Vec<_>>(),
        }),
        Event::Reopen {
            has_visible_windows,
            ..
        } => json!({
            "type": "reopen",
            "has_visible_windows": has_visible_windows,
        }),
        Event::WindowEvent {
            window_id, event, ..
        } => serialize_window_event(window_id, event),
        other => json!({
            "type": "raw",
            "debug": format!("{other:?}"),
        }),
    };

    serde_json::to_string(&value).unwrap_or_else(|_| "{}".into())
}

fn serialize_window_event(
    window_id: &tao::window::WindowId,
    event: &TaoWindowEvent,
) -> serde_json::Value {
    match event {
        TaoWindowEvent::CloseRequested => json!({
            "type": "window-close-requested",
            "window_id": format!("{window_id:?}"),
        }),
        TaoWindowEvent::Destroyed => json!({
            "type": "window-destroyed",
            "window_id": format!("{window_id:?}"),
        }),
        TaoWindowEvent::Resized(size) => json!({
            "type": "window-resized",
            "window_id": format!("{window_id:?}"),
            "size": EventSize {
                width: size.width as f64,
                height: size.height as f64,
            },
        }),
        TaoWindowEvent::Moved(position) => json!({
            "type": "window-moved",
            "window_id": format!("{window_id:?}"),
            "position": EventPosition {
                x: position.x as f64,
                y: position.y as f64,
            },
        }),
        TaoWindowEvent::Focused(focused) => json!({
            "type": "window-focused",
            "window_id": format!("{window_id:?}"),
            "isFocused": focused,
        }),
        TaoWindowEvent::ScaleFactorChanged {
            scale_factor,
            new_inner_size,
        } => json!({
            "type": "window-scale-factor-changed",
            "window_id": format!("{window_id:?}"),
            "scale_factor": scale_factor,
            "size": EventSize {
                width: new_inner_size.width as f64,
                height: new_inner_size.height as f64,
            },
        }),
        TaoWindowEvent::KeyboardInput {
            event: key_event,
            is_synthetic,
            ..
        } => json!({
            "type": "window-keyboard-input",
            "window_id": format!("{window_id:?}"),
            "state": format!("{:?}", key_event.state),
            "logical_key": format!("{:?}", key_event.logical_key),
            "physical_key": format!("{:?}", key_event.physical_key),
            "text": key_event.text.map(|s| s.to_string()),
            "repeat": key_event.repeat,
            "location": format!("{:?}", key_event.location),
            "is_synthetic": is_synthetic,
        }),
        TaoWindowEvent::ReceivedImeText(text) => json!({
            "type": "window-ime-text",
            "window_id": format!("{window_id:?}"),
            "text": text,
        }),
        TaoWindowEvent::ModifiersChanged(modifiers) => json!({
            "type": "window-modifiers-changed",
            "window_id": format!("{window_id:?}"),
            "modifiers": modifiers_payload(*modifiers),
        }),
        TaoWindowEvent::CursorMoved { position, .. } => json!({
            "type": "window-cursor-moved",
            "window_id": format!("{window_id:?}"),
            "position": EventPosition {
                x: position.x,
                y: position.y,
            },
        }),
        TaoWindowEvent::CursorEntered { device_id } => json!({
            "type": "window-cursor-entered",
            "window_id": format!("{window_id:?}"),
            "device_id": format!("{device_id:?}"),
        }),
        TaoWindowEvent::CursorLeft { device_id } => json!({
            "type": "window-cursor-left",
            "window_id": format!("{window_id:?}"),
            "device_id": format!("{device_id:?}"),
        }),
        TaoWindowEvent::MouseInput { state, button, .. } => {
            let state_str = match state {
                ElementState::Pressed => "pressed",
                ElementState::Released => "released",
                _ => "unknown",
            };

            let button_str = match button {
                MouseButton::Left => "left".to_string(),
                MouseButton::Right => "right".to_string(),
                MouseButton::Middle => "middle".to_string(),
                MouseButton::Other(value) => format!("other:{value}"),
                _ => "unknown".to_string(),
            };

            json!({
                "type": "window-mouse-input",
                "window_id": format!("{window_id:?}"),
                "state": state_str,
                "button": button_str,
            })
        }
        TaoWindowEvent::MouseWheel { delta, phase, .. } => {
            let delta_value = match delta {
                MouseScrollDelta::LineDelta(x, y) => json!({
                    "unit": "line",
                    "x": x,
                    "y": y,
                }),
                MouseScrollDelta::PixelDelta(position) => json!({
                    "unit": "pixel",
                    "x": position.x,
                    "y": position.y,
                }),
                _ => json!({
                    "unit": "unknown",
                }),
            };

            json!({
                "type": "window-mouse-wheel",
                "window_id": format!("{window_id:?}"),
                "delta": delta_value,
                "phase": format!("{:?}", phase),
            })
        }
        TaoWindowEvent::DroppedFile(path) => json!({
            "type": "window-dropped-file",
            "window_id": format!("{window_id:?}"),
            "path": path.to_string_lossy(),
        }),
        TaoWindowEvent::HoveredFile(path) => json!({
            "type": "window-hovered-file",
            "window_id": format!("{window_id:?}"),
            "path": path.to_string_lossy(),
        }),
        TaoWindowEvent::HoveredFileCancelled => json!({
            "type": "window-hovered-file-cancelled",
            "window_id": format!("{window_id:?}"),
        }),
        TaoWindowEvent::ThemeChanged(theme) => json!({
            "type": "window-theme-changed",
            "window_id": format!("{window_id:?}"),
            "theme": format!("{:?}", theme),
        }),
        other => json!({
            "type": "window-event",
            "window_id": format!("{window_id:?}"),
            "kind": format!("{:?}", other),
        }),
    }
}

#[cfg(target_os = "macos")]
fn serialize_tray_event(event: &WryTrayEvent) -> serde_json::Value {
    let mut payload = Map::new();
    payload.insert("type".into(), json!("tray-event"));
    payload.insert("tray_id".into(), json!(event.identifier));
    payload.insert(
        "event_type".into(),
        json!(match event.kind {
            WryTrayEventKind::Click => "click",
            WryTrayEventKind::DoubleClick => "double-click",
            WryTrayEventKind::Enter => "enter",
            WryTrayEventKind::Move => "move",
            WryTrayEventKind::Leave => "leave",
        }),
    );
    if let Some((x, y)) = event.position {
        payload.insert("position".into(), json!({"x": x, "y": y}));
    }
    if let Some(rect) = event.rect {
        payload.insert(
            "rect".into(),
            json!({
                "x": rect.origin_x,
                "y": rect.origin_y,
                "width": rect.width,
                "height": rect.height,
            }),
        );
    }
    if let Some(button) = &event.button {
        payload.insert("button".into(), json!(button));
    }
    if let Some(state) = &event.button_state {
        payload.insert("button_state".into(), json!(state));
    }
    serde_json::Value::Object(payload)
}
