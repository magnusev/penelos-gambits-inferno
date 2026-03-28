package com.penelosgambits.domain.gambit

/**
 * A named set of gambit rules with optional chaining.
 *
 * @param name Human-readable name for this set.
 * @param gambits The rules in this set, evaluated in priority order.
 * @param before Optional chain evaluated before this set's own gambits (e.g. emergency heals).
 * @param fallback Optional chain evaluated if no gambit in this set matches.
 */
data class GambitSet(
    val name: String,
    val gambits: List<GambitRule>,
    val before: GambitSet? = null,
    val fallback: GambitSet? = null,
)

