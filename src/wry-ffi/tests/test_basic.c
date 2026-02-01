/**
 * Basic integration test for wry-ffi
 *
 * Compile with:
 *   gcc -o test_basic test_basic.c -L../target/release -lwry_ffi -Wl,-rpath,'$ORIGIN/../target/release'
 *
 * Or on Linux after build:
 *   LD_LIBRARY_PATH=../target/release ./test_basic
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdbool.h>

// FFI type definitions
typedef void* WryApp;
typedef void* WryWindow;

typedef struct {
    const char* title;
    const char* url;
    const char* html;
    const char* user_agent;
    const char* data_directory;
    int x;
    int y;
    unsigned int width;
    unsigned int height;
    unsigned int min_width;
    unsigned int min_height;
    unsigned int max_width;
    unsigned int max_height;
    bool resizable;
    bool fullscreen;
    bool maximized;
    bool minimized;
    bool visible;
    bool transparent;
    bool decorations;
    bool always_on_top;
    bool devtools_enabled;
    bool autoplay_enabled;
} WryWindowParams;

typedef struct {
    bool success;
    int error_code;
    const char* error_message;
} WryResult;

// Function declarations
extern WryApp wry_app_create(void);
extern WryResult wry_app_run(WryApp app);
extern void wry_app_quit(WryApp app);
extern void wry_app_destroy(WryApp app);
extern WryWindow wry_window_create(WryApp app, const WryWindowParams* params);
extern void wry_window_destroy(WryWindow window);
extern const char* wry_get_last_error(void);
extern const char* wry_version(void);
extern void wry_string_free(char* s);

int main(int argc, char** argv) {
    printf("wry-ffi test starting...\n");

    // Get version
    const char* version = wry_version();
    printf("wry-ffi version: %s\n", version);

    // Create app
    printf("Creating app...\n");
    WryApp app = wry_app_create();
    if (app == NULL) {
        const char* error = wry_get_last_error();
        fprintf(stderr, "Failed to create app: %s\n", error ? error : "unknown error");
        return 1;
    }
    printf("App created successfully\n");

    // Create window parameters
    WryWindowParams params = {
        .title = "Test Window",
        .url = NULL,
        .html = "<html><body><h1>Hello from wry-ffi!</h1><p>Press Ctrl+W or close the window to exit.</p></body></html>",
        .user_agent = NULL,
        .data_directory = NULL,
        .x = 100,
        .y = 100,
        .width = 800,
        .height = 600,
        .min_width = 400,
        .min_height = 300,
        .max_width = 0,
        .max_height = 0,
        .resizable = true,
        .fullscreen = false,
        .maximized = false,
        .minimized = false,
        .visible = true,
        .transparent = false,
        .decorations = true,
        .always_on_top = false,
        .devtools_enabled = true,
        .autoplay_enabled = false
    };

    // Create window
    printf("Creating window...\n");
    WryWindow window = wry_window_create(app, &params);
    if (window == NULL) {
        const char* error = wry_get_last_error();
        fprintf(stderr, "Failed to create window: %s\n", error ? error : "unknown error");
        wry_app_destroy(app);
        return 1;
    }
    printf("Window created successfully\n");

    // Run event loop
    printf("Running event loop...\n");
    WryResult result = wry_app_run(app);
    if (!result.success) {
        const char* error = wry_get_last_error();
        fprintf(stderr, "Event loop error: %s\n", error ? error : "unknown error");
    } else {
        printf("Event loop exited normally\n");
    }

    // Cleanup
    printf("Cleaning up...\n");
    wry_window_destroy(window);
    wry_app_destroy(app);

    printf("Test completed successfully!\n");
    return 0;
}
