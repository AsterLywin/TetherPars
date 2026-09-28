package com.tetherpars.app.proxy

import java.io.InputStream
import java.io.OutputStream
import java.net.InetSocketAddress
import java.net.ServerSocket
import java.net.Socket
import java.util.concurrent.atomic.AtomicBoolean
import kotlin.concurrent.thread

/**
 * Task 3 core: internal proxy on 127.0.0.1:8000.
 * Handles HTTP CONNECT + SOCKS5; transports (USB/WiFi/BT) only carry
 * the Windows client to this port (Task 4-7 connect here).
 *
 * Pure JVM (no Android imports) so unit tests run on host.
 */
class LocalProxyServer(val port: Int = 8000) {

    private var server: ServerSocket? = null
    private var acceptThread: Thread? = null
    private val running = AtomicBoolean(false)

    fun start() {
        if (running.getAndSet(true)) return
        val srv = ServerSocket()
        srv.reuseAddress = true
        srv.bind(InetSocketAddress("127.0.0.1", port))
        server = srv
        acceptThread = thread(name = "tetherpars-proxy-accept", isDaemon = true) {
            while (running.get()) {
                try {
                    val client = srv.accept()
                    thread(name = "tetherpars-proxy-conn", isDaemon = true) {
                        try {
                            handleClient(client)
                        } catch (_: Exception) {
                            try { client.close() } catch (_: Exception) { }
                        }
                    }
                } catch (_: Exception) {
                    if (!running.get()) break
                }
            }
        }
    }

    fun stop() {
        running.set(false)
        try { server?.close() } catch (_: Exception) { }
        server = null
    }

    private fun handleClient(client: Socket) {
        client.tcpNoDelay = true
        val input = client.getInputStream()
        val output = client.getOutputStream()

        // Peek first byte to distinguish SOCKS5 (0x05) from HTTP text.
        client.soTimeout = 15000
        val first = input.read()
        if (first == -1) { client.close(); return }

        if (first == 0x05) {
            handleSocks5(client, input, output, first)
        } else {
            handleHttpConnect(client, input, output, first)
        }
    }

    // ---- HTTP CONNECT ----

    private fun handleHttpConnect(client: Socket, input: InputStream, output: OutputStream, firstByte: Int) {
        val header = readHttpHeader(input, firstByte) ?: run {
            output.write("HTTP/1.1 400 Bad Request\r\n\r\n".toByteArray())
            output.flush(); client.close(); return
        }
        val requestLine = header.lineSequence().firstOrNull() ?: ""
        val parts = requestLine.split(" ")
        if (parts.size < 2 || !parts[0].equals("CONNECT", ignoreCase = true)) {
            output.write("HTTP/1.1 405 Method Not Allowed\r\n\r\n".toByteArray())
            output.flush(); client.close(); return
        }
        val target = parts[1].trim()
        val (host, port) = parseHostPort(target) ?: run {
            output.write("HTTP/1.1 400 Bad Request\r\n\r\n".toByteArray())
            output.flush(); client.close(); return
        }
        val remote = try {
            val s = Socket()
            s.tcpNoDelay = true
            s.connect(InetSocketAddress(host, port), 15000)
            s
        } catch (_: Exception) {
            output.write("HTTP/1.1 502 Bad Gateway\r\n\r\n".toByteArray())
            output.flush(); client.close(); return
        }
        output.write("HTTP/1.1 200 Connection Established\r\n\r\n".toByteArray())
        output.flush()
        pipe(client, remote)
    }

    private fun readHttpHeader(input: InputStream, firstByte: Int): String? {
        val buf = StringBuilder()
        buf.append(firstByte.toChar())
        val window = ArrayDeque<Char>()
        window.add(firstByte.toChar())
        fun endsWithDoubleCrlf(): Boolean {
            if (window.size < 4) return false
            val last4 = window.takeLast(4).joinToString("")
            return last4 == "\r\n\r\n"
        }
        if (endsWithDoubleCrlf()) return buf.toString()
        while (buf.length < 32 * 1024) {
            val b = try { input.read() } catch (_: Exception) { return null }
            if (b == -1) return null
            val c = b.toChar()
            buf.append(c)
            window.add(c)
            if (window.size > 4) window.removeFirst()
            if (endsWithDoubleCrlf()) return buf.toString()
        }
        return null
    }

    private fun parseHostPort(target: String): Pair<String, Int>? {
        // target forms: "host:port" (CONNECT). Strip brackets for IPv6 literals.
        val t = target.trim()
        if (t.isEmpty()) return null
        if (t.startsWith("[")) {
            val end = t.indexOf(']')
            if (end == -1) return null
            val host = t.substring(1, end)
            val rest = t.substring(end + 1)
            val port = if (rest.startsWith(":")) rest.substring(1).toIntOrNull() ?: return null else 80
            return host to port
        }
        val idx = t.lastIndexOf(':')
        if (idx == -1) return t to 80
        val host = t.substring(0, idx)
        val port = t.substring(idx + 1).toIntOrNull() ?: return null
        if (host.isEmpty()) return null
        return host to port
    }

    // ---- SOCKS5 (RFC 1928, CONNECT only, no-auth) ----

    private fun handleSocks5(client: Socket, input: InputStream, output: OutputStream, ver: Int) {
        // Greeting already consumed VER(0x05); next: NMETHODS + METHODS
        val nMethods = input.read()
        if (nMethods < 0) { client.close(); return }
        repeat(nMethods) { if (input.read() < 0) { client.close(); return } }
        output.write(byteArrayOf(0x05, 0x00)) // VER=5, METHOD=no-auth
        output.flush()

        // Request: VER CMD RSV ATYP ADDR PORT
        val reqVer = input.read()
        val cmd = input.read()
        input.read() // RSV
        val atyp = input.read()
        if (reqVer != 0x05 || cmd != 0x01) {
            output.write(byteArrayOf(0x05, 0x07, 0x00, 0x01, 0, 0, 0, 0, 0, 0))
            output.flush(); client.close(); return
        }
        val host: String = when (atyp) {
            0x01 -> { // IPv4
                val b = ByteArray(4); if (!readFully(input, b)) { client.close(); return }
                b.joinToString(".") { (it.toInt() and 0xFF).toString() }
            }
            0x03 -> { // DOMAIN
                val len = input.read()
                if (len < 0) { client.close(); return }
                val b = ByteArray(len); if (!readFully(input, b)) { client.close(); return }
                String(b, Charsets.US_ASCII)
            }
            0x04 -> { // IPv6
                val b = ByteArray(16); if (!readFully(input, b)) { client.close(); return }
                // Rely on InetAddress formatting via connect below; keep raw for now
                java.net.InetAddress.getByAddress(b).hostAddress ?: run { client.close(); return }
            }
            else -> { client.close(); return }
        }
        val portHi = input.read(); val portLo = input.read()
        if (portHi < 0 || portLo < 0) { client.close(); return }
        val port = (portHi shl 8) or portLo

        val remote = try {
            val s = Socket()
            s.tcpNoDelay = true
            s.connect(InetSocketAddress(host, port), 15000)
            s
        } catch (_: Exception) {
            output.write(byteArrayOf(0x05, 0x05, 0x00, 0x01, 0, 0, 0, 0, 0, 0))
            output.flush(); client.close(); return
        }
        output.write(byteArrayOf(0x05, 0x00, 0x00, 0x01, 0, 0, 0, 0, 0, 0))
        output.flush()
        pipe(client, remote)
    }

    private fun readFully(input: InputStream, buf: ByteArray): Boolean {
        var off = 0
        while (off < buf.size) {
            val n = try { input.read(buf, off, buf.size - off) } catch (_: Exception) { return false }
            if (n <= 0) return false
            off += n
        }
        return true
    }

    // ---- Bidirectional pipe ----

    private fun pipe(a: Socket, b: Socket) {
        val t1 = thread(isDaemon = true) { copy(a.getInputStream(), b.getOutputStream(), a, b) }
        val t2 = thread(isDaemon = true) { copy(b.getInputStream(), a.getOutputStream(), a, b) }
        t1.join()
        // Close both once one direction ends; interrupt the mirror.
        try { a.close() } catch (_: Exception) { }
        try { b.close() } catch (_: Exception) { }
        try { t2.interrupt() } catch (_: Exception) { }
    }

    private fun copy(input: InputStream, output: OutputStream, a: Socket, b: Socket) {
        val buf = ByteArray(32 * 1024)
        try {
            while (true) {
                val n = input.read(buf)
                if (n <= 0) break
                output.write(buf, 0, n)
                output.flush()
            }
        } catch (_: Exception) {
        } finally {
            try { a.shutdownOutput() } catch (_: Exception) { }
            try { b.shutdownInput() } catch (_: Exception) { }
        }
    }
}
