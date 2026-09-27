package com.tetherpars.app

import android.app.Service
import android.content.Intent
import android.os.IBinder

/**
 * Minimal stub for Task 2 (button wiring target).
 * Task 3 expands this into ForegroundService + VpnService + LocalProxyServer(8000).
 */
class TetherService : Service() {
    override fun onBind(intent: Intent?): IBinder? = null

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        return START_STICKY
    }
}
