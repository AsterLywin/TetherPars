package com.tetherpars.app

import com.tetherpars.app.data.LicenseStore
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * Task 9 test (TDD RED first).
 * Free build is time-limited, Pro key removes the timeout.
 */
class LicenseStoreTest {

    @Test
    fun `free build disconnects after timeout flag`() {
        assertFalse(LicenseStore(isPro = false).isUnlimited())
        assertTrue(LicenseStore(isPro = true).isUnlimited())
    }
}
