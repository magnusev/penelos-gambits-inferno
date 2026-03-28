package com.penelosgambits.domain.gambit

/**
 * Picks the correct [GambitSet] based on the current map/zone ID.
 */
interface GambitSetPicker {
    fun pick(mapId: Int): GambitSet
}

