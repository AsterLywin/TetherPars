package com.tetherpars.app

import com.tetherpars.app.proxy.LocalProxyServer
import org.junit.Assert.assertTrue
import org.junit.Test
import java.net.Socket

/**
 * Task 3 unit test (TDD RED first).
 * LocalProxyServer(port) must accept `CONNECT host:port` and tunnel bytes.
 * Uses port 18000 to avoid clashing with production 8000.
 */
class LocalProxyServerTest {

    @Test
    fun `CONNECT tunnels bytes`() {
        val proxy = LocalProxyServer(18000)
        proxy.start()
        try {
            Socket("127.0.0.1", 18000).use { s ->
                s.soTimeout = 15000
                s.getOutputStream().write("CONNECT example.com:80 HTTP/1.1\r\n\r\n".toByteArray())
                s.getOutputStream().flush()
                val reply = s.getInputStream().bufferedReader().readLine()
                assertTrue("expected 200, got: $reply", reply != null && reply.contains("200"))
            }
        } finally {
            proxy.stop()
        }
    }
}
