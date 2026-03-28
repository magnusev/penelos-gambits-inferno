package com.penelosgambits.domain.gambit

/**
 * The intended action for a gambit rule.
 */
sealed class ActionIntent {
    data class Cast(val spell: String) : ActionIntent()
    data class Macro(val macro: String) : ActionIntent()
    data object None : ActionIntent()
}

