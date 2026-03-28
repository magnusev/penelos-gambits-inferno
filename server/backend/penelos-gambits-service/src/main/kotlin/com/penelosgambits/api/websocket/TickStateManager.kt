package com.penelosgambits.api.websocket

import com.penelosgambits.domain.model.TickState
import java.util.concurrent.atomic.AtomicReference

/**
 * Thread-safe holder for the latest tick state received from the bot.
 * Updated every tick (~2-3 times per second).
 */
class TickStateManager {
    private val _currentState = AtomicReference<TickState?>(null)

    val currentState: TickState? get() = _currentState.get()

    /** Set after construction once the tick processor is available. */
    var tickProcessor: TickProcessor? = null

    fun update(state: TickState) {
        _currentState.set(state)
    }

    /**
     * Called from the coroutine scope to process the latest tick through the gambit system.
     */
    suspend fun processLatestTick() {
        val state = currentState ?: return
        tickProcessor?.processTick(state)
    }
}

