package com.tetherpars.app

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.content.Intent
import android.net.VpnService
import android.os.Build
import com.tetherpars.app.proxy.LocalProxyServer

/**
 * Task 3 core: Foreground VpnService hosting LocalProxyServer(8000).
 * Transports (Task 4-6) only carry the Windows client to this proxy.
 * Full VPN packet routing is out of scope for Task 3 — the service stays
 * alive in foreground and keeps the proxy bound while tethering.
 */
class TetherService : VpnService() {

    private var proxy: LocalProxyServer? = null

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        startForegroundCompat()
        synchronized(this) {
            if (proxy == null) {
                val p = LocalProxyServer(8000)
                p.start()
                proxy = p
            }
        }
        return START_STICKY
    }

    override fun onRevoke() {
        stopProxy()
        stopSelf()
    }

    override fun onDestroy() {
        stopProxy()
        super.onDestroy()
    }

    private fun stopProxy() {
        synchronized(this) {
            try { proxy?.stop() } catch (_: Exception) { }
            proxy = null
        }
    }

    private fun startForegroundCompat() {
        val channelId = "tetherpars"
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val nm = getSystemService(NotificationManager::class.java)
            val ch = NotificationChannel(
                channelId,
                "TetherPars",
                NotificationManager.IMPORTANCE_MIN
            )
            nm?.createNotificationChannel(ch)
        }
        val notification: Notification = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            Notification.Builder(this, channelId)
                .setContentTitle("TetherPars")
                .setContentText("Sharing active :8000")
                .setSmallIcon(android.R.drawable.stat_sys_data_bluetooth)
                .build()
        } else {
            @Suppress("DEPRECATION")
            Notification.Builder(this)
                .setContentTitle("TetherPars")
                .setContentText("Sharing active :8000")
                .setSmallIcon(android.R.drawable.stat_sys_data_bluetooth)
                .build()
        }
        startForeground(1, notification)
    }
}
