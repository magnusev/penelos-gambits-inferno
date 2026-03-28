package com.penelosgambits.api.websocket

import com.penelosgambits.domain.model.TickState
import org.slf4j.LoggerFactory
import java.util.concurrent.atomic.AtomicReference

/**
 * Thread-safe holder for the latest tick state received from the bot.
 * Updated every tick (~2-3 times per second).
 */
class TickStateManager {
    private val logger = LoggerFactory.getLogger(TickStateManager::class.java)
    private val _currentState = AtomicReference<TickState?>(null)

    val currentState: TickState? get() = _currentState.get()

    fun update(state: TickState) {
        _currentState.set(state)
        logger.info(
            "Tick: mapId={}, player.hp={}, target={}, bosses={}",
            state.mapId,
            state.player?.health,
            state.target?.name ?: "none",
            state.bosses.size,
        )
    }
}

