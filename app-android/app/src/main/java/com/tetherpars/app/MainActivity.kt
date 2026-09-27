package com.tetherpars.app

import android.content.Intent
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.background
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Bluetooth
import androidx.compose.material.icons.filled.Share
import androidx.compose.material.icons.filled.Star
import androidx.compose.material.icons.filled.Usb
import androidx.compose.material.icons.filled.Wifi
import androidx.compose.material3.Button
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.ElevatedCard
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.tetherpars.app.data.LicenseStore

private val BrandTeal = Color(0xFF0E7C6B)
private val BrandTealDark = Color(0xFF4FD1B5)
private val BrandInk = Color(0xFF0E3B35)

/**
 * Home hub: 3 transport actions + statusText (Task 2 contract, kept verbatim
 * for the UI tests) + Help (Task 8) + license card (Task 9).
 */
class MainActivity : ComponentActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            TetherParsTheme {
                var showHelp by remember { mutableStateOf(false) }
                if (showHelp) {
                    HelpScreen(onBack = { showHelp = false })
                } else {
                    HomeScreen(
                        onUsb = { startTether("usb") },
                        onWifi = { startTether("wifi") },
                        onBt = { startTether("bt") },
                        onHelp = { showHelp = true }
                    )
                }
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
fun TetherParsTheme(content: @Composable () -> Unit) {
    val scheme = if (isSystemInDarkTheme()) {
        darkColorScheme(primary = BrandTealDark, secondary = BrandTealDark)
    } else {
        lightColorScheme(primary = BrandTeal, secondary = BrandTeal)
    }
    MaterialTheme(colorScheme = scheme, content = content)
}

@Composable
fun HomeScreen(
    onUsb: () -> Unit,
    onWifi: () -> Unit,
    onBt: () -> Unit,
    onHelp: () -> Unit = {}
) {
    var statusText by remember { mutableStateOf("Idle") }
    val active = statusText != "Idle"

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(20.dp),
        verticalArrangement = Arrangement.spacedBy(14.dp)
    ) {
        HeaderCard()

        StatusCard(statusText = statusText, active = active)

        Text(
            text = "روش اتصال",
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.Bold
        )

        TransportCard(
            icon = Icons.Filled.Usb,
            title = "USB Tether",
            description = "پایدارترین راه — گوشی را با کابل به ویندوز وصل کن",
            badge = "پیشنهاد",
            onConnect = {
                statusText = "USB: starting…"
                onUsb()
            }
        )

        TransportCard(
            icon = Icons.Filled.Wifi,
            title = "WiFi Direct Hotspot",
            description = "بی‌سیم و سریع — گوشی هات‌اسپات می‌شود، لپ‌تاپ وصل می‌شود",
            badge = null,
            onConnect = {
                statusText = "WiFi: starting…"
                onWifi()
            }
        )

        TransportCard(
            icon = Icons.Filled.Bluetooth,
            title = "Bluetooth",
            description = "کم‌مصرف ولی کندتر — برای مواقع ضروری",
            badge = null,
            onConnect = {
                statusText = "Bluetooth: starting…"
                onBt()
            }
        )

        LicenseCard()

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(
                text = "TetherPars 0.1.0",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
            TextButton(onClick = onHelp) { Text("راهنما") }
        }
    }
}

@Composable
private fun HeaderCard() {
    ElevatedCard(modifier = Modifier.fillMaxWidth()) {
        Row(
            modifier = Modifier.padding(18.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Box(
                modifier = Modifier
                    .size(52.dp)
                    .clip(RoundedCornerShape(16.dp))
                    .background(BrandTeal),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    imageVector = Icons.Filled.Share,
                    contentDescription = null,
                    tint = Color.White,
                    modifier = Modifier.size(28.dp)
                )
            }
            Spacer(Modifier.width(14.dp))
            Column {
                Text(
                    text = "TetherPars",
                    style = MaterialTheme.typography.headlineSmall,
                    fontWeight = FontWeight.ExtraBold,
                    color = BrandInk
                )
                Text(
                    text = "اینترنت گوشی، روی ویندوز",
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
        }
    }
}

@Composable
private fun StatusCard(statusText: String, active: Boolean) {
    ElevatedCard(
        modifier = Modifier.fillMaxWidth(),
        colors = CardDefaults.elevatedCardColors(
            containerColor = if (active) BrandTeal.copy(alpha = 0.12f)
            else MaterialTheme.colorScheme.surfaceVariant
        )
    ) {
        Row(
            modifier = Modifier.padding(16.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Box(
                modifier = Modifier
                    .size(12.dp)
                    .clip(CircleShape)
                    .background(if (active) BrandTeal else Color.Gray)
            )
            Spacer(Modifier.width(10.dp))
            Column {
                Text(
                    text = "وضعیت",
                    style = MaterialTheme.typography.labelMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
                Text(
                    text = statusText,
                    fontSize = 16.sp,
                    fontWeight = FontWeight.SemiBold
                )
            }
        }
    }
}

@Composable
private fun TransportCard(
    icon: ImageVector,
    title: String,
    description: String,
    badge: String?,
    onConnect: () -> Unit
) {
    ElevatedCard(modifier = Modifier.fillMaxWidth()) {
        Column(modifier = Modifier.padding(16.dp)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Icon(
                    imageVector = icon,
                    contentDescription = null,
                    tint = BrandTeal,
                    modifier = Modifier.size(28.dp)
                )
                Spacer(Modifier.width(10.dp))
                Text(
                    text = title,
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                    modifier = Modifier.weight(1f)
                )
                if (badge != null) {
                    Surface(
                        shape = RoundedCornerShape(8.dp),
                        color = BrandTeal.copy(alpha = 0.15f)
                    ) {
                        Text(
                            text = badge,
                            modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp),
                            style = MaterialTheme.typography.labelSmall,
                            color = BrandTeal,
                            fontWeight = FontWeight.Bold
                        )
                    }
                }
            }
            Spacer(Modifier.height(6.dp))
            Text(
                text = description,
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
            Spacer(Modifier.height(10.dp))
            Button(onClick = onConnect, modifier = Modifier.fillMaxWidth()) {
                Text("اتصال")
            }
        }
    }
}

@Composable
private fun LicenseCard() {
    var note by remember { mutableStateOf<String?>(null) }
    ElevatedCard(modifier = Modifier.fillMaxWidth()) {
        Row(
            modifier = Modifier.padding(16.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Icon(
                imageVector = Icons.Filled.Star,
                contentDescription = null,
                tint = Color(0xFFB8860B),
                modifier = Modifier.size(28.dp)
            )
            Spacer(Modifier.width(10.dp))
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = "نسخه رایگان",
                    style = MaterialTheme.typography.titleSmall,
                    fontWeight = FontWeight.Bold
                )
                Text(
                    text = "هر ${LicenseStore.FREE_TIMEOUT_MINUTES} دقیقه قطع می‌شود و با یک کلیک وصل می‌شود.",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
                if (note != null) {
                    Spacer(Modifier.height(4.dp))
                    Text(
                        text = note!!,
                        style = MaterialTheme.typography.bodySmall,
                        color = BrandTeal
                    )
                }
            }
        }
        TextButton(
            onClick = { note = "خرید کلید پرو به‌زودی در همین‌جا فعال می‌شود." },
            modifier = Modifier.padding(start = 8.dp, bottom = 8.dp)
        ) {
            Text("ارتقا به پرو")
        }
    }
}

/**
 * Task 8: Persian Help page for common errors (fixed text).
 * Mirrors Windows ErrorCatalog; no complex logic.
 */
@Composable
fun HelpScreen(onBack: () -> Unit = {}) {
    val errors = listOf(
        "گوشی پیدا نشد: USB Debugging روشن است؟ کابل اصلی است؟",
        "Version Mismatched: نسخه ویندوز و گوشی یکی نیست، هر دو را آپدیت کن",
        "Conflict adb.exe: مسیر c:\\program files (x86) را چک کن و adb را rename کن",
        "قطعی code=1: پورت USB را عوض کن، لپ‌تاپ به شارژ باشد"
    )
    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(20.dp),
        verticalArrangement = Arrangement.spacedBy(10.dp)
    ) {
        Text(
            text = "راهنمای خطاها",
            style = MaterialTheme.typography.headlineSmall,
            fontWeight = FontWeight.ExtraBold
        )
        errors.forEach { e ->
            ElevatedCard(modifier = Modifier.fillMaxWidth()) {
                Text(
                    text = e,
                    modifier = Modifier.padding(14.dp),
                    style = MaterialTheme.typography.bodyMedium
                )
            }
        }
        ElevatedCard(modifier = Modifier.fillMaxWidth()) {
            Text(
                text = "اشتراک WiFi: به SSID گروه وصل شو و Proxy دستی 192.168.49.1:8000 را ست کن.",
                modifier = Modifier.padding(14.dp),
                style = MaterialTheme.typography.bodyMedium
            )
        }
        Spacer(Modifier.height(8.dp))
        Button(onClick = onBack, modifier = Modifier.fillMaxWidth()) { Text("بازگشت") }
    }
}
