package com.tetherpars.app.util

/**
 * Task 7 (Android mirror of Windows VersionHandshake).
 * Same rules: `HELLO version=X` on both sides, equal versions only.
 */
object VersionCheck {
    const val CURRENT_VERSION = "0.1.0"
    const val MISMATCH_MESSAGE = "Version Mismatched"

    fun isCompatible(phone: String?, pc: String?): Boolean {
        if (phone.isNullOrBlank() || pc.isNullOrBlank()) return false
        return phone.trim() == pc.trim()
    }

    fun buildHello(version: String): String = "HELLO version=${version.trim()}"

    fun parseHello(line: String?): String? {
        if (line.isNullOrBlank()) return null
        val prefix = "HELLO version="
        val t = line.trim()
        if (!t.startsWith(prefix, ignoreCase = true)) return null
        return t.substring(prefix.length).trim().takeIf { it.isNotEmpty() }
    }
}
