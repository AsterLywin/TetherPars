package com.tetherpars.app.data

import android.content.SharedPreferences

/**
 * Task 9: free build is time-limited, Pro key removes the timeout.
 * Only license state lives in SharedPreferences (per Global Constraints).
 */
class LicenseStore(val isPro: Boolean) {

    fun isUnlimited(): Boolean = isPro

    companion object {
        const val PREFS_NAME = "tetherpars"
        const val KEY_PRO = "pro"

        /** Free session length before disconnect (user reconnects with one click). */
        const val FREE_TIMEOUT_MINUTES = 10

        fun load(prefs: SharedPreferences): LicenseStore =
            LicenseStore(prefs.getBoolean(KEY_PRO, false))

        fun save(prefs: SharedPreferences, isPro: Boolean) {
            prefs.edit().putBoolean(KEY_PRO, isPro).apply()
        }
    }
}
