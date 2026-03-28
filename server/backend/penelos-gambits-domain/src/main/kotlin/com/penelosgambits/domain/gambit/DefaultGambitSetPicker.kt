package com.penelosgambits.domain.gambit

/**
 * Default picker that always returns the [defaultGambitSet].
 * Future versions can switch based on mapId or spec.
 */
class DefaultGambitSetPicker : GambitSetPicker {
    override fun pick(mapId: Int): GambitSet = defaultGambitSet
}

