package com.tetherpars.app

import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.withText
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import androidx.test.ext.junit.rules.ActivityScenarioRule
import androidx.test.ext.junit.runners.AndroidJUnit4
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith

/**
 * Task 2 UI test (TDD RED first).
 * Covers: 3 transport buttons + statusText wiring.
 * Plan snippet adapted: clicking "USB Tether" must wire to TetherService
 * (verified via statusText, since Espresso-Intents `intended()` only tracks activities).
 */
@RunWith(AndroidJUnit4::class)
class MainActivityTest {

    @get:Rule
    val activityRule = ActivityScenarioRule(MainActivity::class.java)

    @Test
    fun usbButton_startsTetherService() {
        onView(withText("USB Tether")).perform(click())
        // TetherService intent fired -> status reflects USB mode (Task 3-5 hook into same statusText)
        onView(withText("USB: starting…")).check(matches(isDisplayed()))
    }

    @Test
    fun wifiButton_showsStatus() {
        onView(withText("WiFi Direct Hotspot")).perform(click())
        onView(withText("WiFi: starting…")).check(matches(isDisplayed()))
    }

    @Test
    fun bluetoothButton_showsStatus() {
        onView(withText("Bluetooth")).perform(click())
        onView(withText("Bluetooth: starting…")).check(matches(isDisplayed()))
    }

    @Test
    fun allButtons_areDisplayed() {
        onView(withText("USB Tether")).check(matches(isDisplayed()))
        onView(withText("WiFi Direct Hotspot")).check(matches(isDisplayed()))
        onView(withText("Bluetooth")).check(matches(isDisplayed()))
    }
}
