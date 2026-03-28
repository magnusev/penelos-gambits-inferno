package com.penelosgambits.domain.selector

import com.penelosgambits.domain.model.TickContext
import com.penelosgambits.domain.model.UnitState

/**
 * Selects a single target unit from the current tick context.
 * Returns null if no suitable target is found.
 */
fun interface TargetSelector {
    suspend fun select(context: TickContext): UnitState?
}

