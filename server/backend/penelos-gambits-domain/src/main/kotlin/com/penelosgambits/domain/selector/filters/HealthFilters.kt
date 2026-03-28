package com.penelosgambits.domain.selector.filters

import com.penelosgambits.domain.model.UnitState
import com.penelosgambits.domain.selector.UnitFilter

/** Removes dead units. */
class IsNotDeadFilter : UnitFilter {
    override fun filter(units: List<UnitState>): List<UnitState> =
        units.filter { !it.isDead }
}

/** Sorts by health ascending and returns only the lowest. */
class LowestHealthFilter : UnitFilter {
    override fun filter(units: List<UnitState>): List<UnitState> {
        val lowest = units.minByOrNull { it.health } ?: return emptyList()
        return listOf(lowest)
    }
}

/** Keeps units with health below [threshold], then sorts ascending. */
class LowestHealthUnderThresholdFilter(private val threshold: Int) : UnitFilter {
    override fun filter(units: List<UnitState>): List<UnitState> =
        units.filter { it.health < threshold }.sortedBy { it.health }
}

