package com.tetherpars.app.transport

import android.bluetooth.BluetoothAdapter

/**
 * Task 6 (optional, last): Bluetooth transport.
 * Same pattern as USB/WiFi: Bluetooth is only the road to
 * LocalProxyServer(8000) (Task 3) — slower than WiFi but low-power.
 * Pair phone + laptop, enable Bluetooth tethering, then the laptop
 * reaches the proxy at the tethering gateway (usually 192.168.44.1:8000).
 */
class BluetoothTransport(private val adapter: BluetoothAdapter?) {

    fun isSupported(): Boolean = adapter != null

    fun isEnabled(): Boolean = try {
        adapter?.isEnabled == true
    } catch (_: SecurityException) {
        false
    }

    fun bondedDeviceNames(): List<String> = try {
        adapter?.bondedDevices?.map { it.name ?: it.address } ?: emptyList()
    } catch (_: SecurityException) {
        emptyList()
    }

    companion object {
        val steps = listOf(
            "Pair گوشی و لپ‌تاپ",
            "Bluetooth Tethering روشن",
            "Proxy دستی در مرورگر لپ‌تاپ"
        )

        const val PROXY_PORT = 8000

        /** Typical Bluetooth-tethering gateway; varies by OEM/ROM. */
        const val DEFAULT_GATEWAY = "192.168.44.1"

        fun proxyEndpoint(gateway: String = DEFAULT_GATEWAY): String =
            "$gateway:$PROXY_PORT"

        fun guideText(gateway: String = DEFAULT_GATEWAY): String = buildString {
            appendLine("اتصال بلوتوث (کندتر از WiFi ولی کم‌مصرف):")
            steps.forEachIndexed { i, s -> appendLine("${i + 1}. $s") }
            appendLine("سپس در مرورگر Proxy دستی ${proxyEndpoint(gateway)} را ست کن.")
        }
    }
}
