package com.penelosgambits.domain.selector

import com.penelosgambits.domain.model.TickContext
import com.penelosgambits.domain.model.UnitState

/**
 * Runs a list of units through a pipeline of [UnitFilter]s and returns the first survivor.
 * [unitProvider] supplies the initial candidate list from the tick context.
 */
class FilterPipelineSelector(
    private val unitProvider: (TickContext) -> List<UnitState>,
    private val filters: List<UnitFilter>,
) : TargetSelector {

    override suspend fun select(context: TickContext): UnitState? {
        var units = unitProvider(context)
        for (filter in filters) {
            units = filter.filter(units)
            if (units.isEmpty()) return null
        }
        return units.firstOrNull()
    }
}


