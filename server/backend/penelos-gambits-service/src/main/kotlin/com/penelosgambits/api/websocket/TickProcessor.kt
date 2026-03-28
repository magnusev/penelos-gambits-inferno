package com.penelosgambits.api.websocket

import com.penelosgambits.data.dto.CommandDto
import com.penelosgambits.domain.gambit.ActionIntent
import com.penelosgambits.domain.gambit.DefaultGambitSetPicker
import com.penelosgambits.domain.gambit.EvaluationResult
import com.penelosgambits.domain.gambit.GambitSetPicker
import com.penelosgambits.domain.gambit.evaluate
import com.penelosgambits.domain.model.TickContext
import com.penelosgambits.domain.model.TickState
import com.penelosgambits.domain.port.GameQueryPort
import com.penelosgambits.domain.port.MessageSender
import kotlinx.serialization.json.Json
import org.slf4j.LoggerFactory
import java.util.concurrent.atomic.AtomicLong

/**
 * Processes each tick: evaluates gambit rules against the current state
 * and sends the resulting command to the bot.
 */
class TickProcessor(
    private val queryPort: GameQueryPort,
    private val messageSender: MessageSender,
    private val gambitSetPicker: GambitSetPicker = DefaultGambitSetPicker(),
) {
    private val logger = LoggerFactory.getLogger(TickProcessor::class.java)
    private val commandCounter = AtomicLong(0)
    private val json = Json { ignoreUnknownKeys = true }

    suspend fun processTick(state: TickState) {
        val context = TickContext(state, queryPort)
        val gambitSet = gambitSetPicker.pick(state.mapId)
        val result = evaluate(gambitSet, context)

        if (result == EvaluationResult.NONE) {
            logger.debug("No gambit matched, sending NONE")
            sendCommand(ActionIntent.None, null)
            return
        }

        logger.info(
            "Gambit fired: {} → {} on {}",
            result.gambitName,
            result.action,
            result.target?.unitId ?: "self",
        )

        sendCommand(result.action, result.target?.unitId)
    }

    private suspend fun sendCommand(action: ActionIntent, targetUnitId: String?) {
        val commandId = "cmd-${commandCounter.incrementAndGet()}"

        val command = when (action) {
            is ActionIntent.Cast -> CommandDto(
                commandId = commandId,
                action = "CAST",
                spell = action.spell,
                target = targetUnitId,
            )
            is ActionIntent.Macro -> CommandDto(
                commandId = commandId,
                action = "MACRO",
                macro = action.macro,
            )
            is ActionIntent.None -> CommandDto(
                commandId = commandId,
                action = "NONE",
            )
        }

        messageSender.send(json.encodeToString(CommandDto.serializer(), command))
    }
}

