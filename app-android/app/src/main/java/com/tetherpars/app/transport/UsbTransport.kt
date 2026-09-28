package com.tetherpars.app.transport

/**
 * Task 4: USB transport.
 * The proxy itself lives in LocalProxyServer(8000) (Task 3).
 * USB is only the road: `adb forward tcp:8000 tcp:8000` exposes the
 * phone proxy on Windows 127.0.0.1:8000 (manual-proxy mode).
 */
object UsbTransport {
    val steps = listOf(
        "کابل اصلی + پورت USB3",
        "USB Debugging روشن",
        "Connect در Tray ویندوز"
    )

    const val ADB_FORWARD_COMMAND = "adb forward tcp:8000 tcp:8000"
    const val WINDOWS_PROXY_HOST = "127.0.0.1"
    const val WINDOWS_PROXY_PORT = 8000

    /** Human-readable checklist shown in-app (same content as [steps]). */
    fun guideText(): String = buildString {
        appendLine("اتصال USB (پایدارترین راه):")
        steps.forEachIndexed { i, s -> appendLine("${i + 1}. $s") }
        appendLine("سپس در مرورگر ویندوز Proxy دستی ${WINDOWS_PROXY_HOST}:${WINDOWS_PROXY_PORT} را ست کن.")
    }
}
