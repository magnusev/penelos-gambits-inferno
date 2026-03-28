package com.penelosgambits.domain.selector

import com.penelosgambits.domain.model.UnitState

/**
 * Filters a list of units, removing those that don't match.
 * Filters are composed into pipelines via [FilterPipelineSelector].
 */
fun interface UnitFilter {
    fun filter(units: List<UnitState>): List<UnitState>
}

