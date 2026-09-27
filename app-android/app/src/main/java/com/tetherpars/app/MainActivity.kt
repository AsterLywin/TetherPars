package com.tetherpars.app

import android.content.Intent
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp

/**
 * Task 2: Home hub with 3 transport buttons + statusText.
 * Task 3-5 hook their logic into the same statusText + TetherService.
 */
class MainActivity : ComponentActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            MaterialTheme {
                HomeScreen(
                    onUsb = { startTether("usb") },
                    onWifi = { startTether("wifi") },
                    onBt = { startTether("bt") }
                )
            }
        }
    }

    private fun startTether(mode: String) {
        val intent = Intent(this, TetherService::class.java).apply {
            action = "com.tetherpars.app.START_$mode"
        }
        startService(intent)
    }
}

@Composable
fun HomeScreen(
    onUsb: () -> Unit,
    onWifi: () -> Unit,
    onBt: () -> Unit
) {
    var statusText by remember { mutableStateOf("Idle") }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(24.dp),
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Button(onClick = {
            statusText = "USB: starting…"
            onUsb()
        }) { Text("USB Tether") }

        Spacer(Modifier.height(12.dp))

        Button(onClick = {
            statusText = "WiFi: starting…"
            onWifi()
        }) { Text("WiFi Direct Hotspot") }

        Spacer(Modifier.height(12.dp))

        Button(onClick = {
            statusText = "Bluetooth: starting…"
            onBt()
        }) { Text("Bluetooth") }

        Spacer(Modifier.height(24.dp))

        Text(text = statusText)
    }
}
