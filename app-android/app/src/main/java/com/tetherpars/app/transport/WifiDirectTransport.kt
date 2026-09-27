package com.tetherpars.app.transport

import android.net.wifi.p2p.WifiP2pGroup
import android.net.wifi.p2p.WifiP2pManager

/**
 * Task 5: WiFi Direct transport.
 * The phone becomes Group Owner (usually 192.168.49.1) and Windows
 * connects to the P2P SSID, then reaches LocalProxyServer (Task 3)
 * at 192.168.49.1:8000 (manual-proxy mode, same as USB).
 */
class WifiDirectTransport(
    private val manager: WifiP2pManager,
    private val channel: WifiP2pManager.Channel
) {

    /** Credentials Windows needs: join [networkName] with [passphrase], then proxy at [proxyEndpoint]. */
    data class GroupCredentials(
        val networkName: String,
        val passphrase: String,
        val goAddress: String = DEFAULT_GO_ADDRESS
    ) {
        val proxyEndpoint: String get() = "$goAddress:$PROXY_PORT"
    }

    fun createGroup(
        onInfo: (networkName: String, passphrase: String) -> Unit,
        onError: (reason: Int) -> Unit = {}
    ) {
        manager.createGroup(channel, object : WifiP2pManager.ActionListener {
            override fun onSuccess() {
                manager.requestGroupInfo(channel) { group: WifiP2pGroup? ->
                    if (group == null) {
                        onError(ERROR_NO_GROUP)
                    } else {
                        onInfo(group.networkName, group.passphrase)
                    }
                }
            }

            override fun onFailure(reason: Int) {
                onError(reason)
            }
        })
    }

    fun createGroupDetailed(
        onCredentials: (GroupCredentials) -> Unit,
        onError: (reason: Int) -> Unit = {}
    ) {
        createGroup(
            onInfo = { ssid, pass ->
                onCredentials(GroupCredentials(ssid, pass))
            },
            onError = onError
        )
    }

    fun removeGroup(
        onDone: () -> Unit = {},
        onError: (reason: Int) -> Unit = {}
    ) {
        manager.removeGroup(channel, object : WifiP2pManager.ActionListener {
            override fun onSuccess() = onDone()
            override fun onFailure(reason: Int) = onError(reason)
        })
    }

    companion object {
        /** Standard Group Owner address on Android P2P groups. */
        const val DEFAULT_GO_ADDRESS = "192.168.49.1"
        const val PROXY_PORT = 8000

        /** Internal error code when group info comes back null. */
        const val ERROR_NO_GROUP = -1

        fun proxyEndpoint(goAddress: String = DEFAULT_GO_ADDRESS): String =
            "$goAddress:$PROXY_PORT"
    }
}
