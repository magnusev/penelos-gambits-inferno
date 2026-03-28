package com.penelosgambits.data.mapper

import com.penelosgambits.data.dto.ConnectDto
import com.penelosgambits.data.dto.StateUpdateDto
import com.penelosgambits.domain.model.BossState
import com.penelosgambits.domain.model.ConnectionInfo
import com.penelosgambits.domain.model.GroupInfo
import com.penelosgambits.domain.model.PlayerState
import com.penelosgambits.domain.model.TargetState
import com.penelosgambits.domain.model.TickState

fun StateUpdateDto.toDomain(): TickState = TickState(
    timestamp = timestamp,
    mapId = mapId,
    globalCooldown = globalCooldown,
    combatTime = combatTime,
    player = player?.let {
        PlayerState(
            health = it.health,
            spec = it.spec,
            castingSpellId = it.castingSpellId,
            inCombat = it.inCombat,
            isMoving = it.isMoving,
        )
    },
    target = target?.let {
        TargetState(
            exists = it.exists,
            name = it.name,
            health = it.health,
            castingSpellId = it.castingSpellId,
        )
    },
    group = group?.let {
        GroupInfo(
            type = it.type,
            size = it.size,
        )
    },
    bosses = bosses.map {
        BossState(
            unitId = it.unitId,
            name = it.name,
            health = it.health,
            castingSpellId = it.castingSpellId,
        )
    },
)

fun ConnectDto.toDomain(): ConnectionInfo = ConnectionInfo(
    character = character,
    spec = spec,
)

