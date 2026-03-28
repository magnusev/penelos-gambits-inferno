package com.penelosgambits.libs.ulid

import java.security.SecureRandom
import java.util.UUID
import kotlin.text.iterator

@JvmInline
value class ULID private constructor(
    val value: String,
) {
    companion object {
        private const val ULID_LENGTH = 26
        private const val ENCODING_CHARS = "0123456789ABCDEFGHJKMNPQRSTVWXYZ"
        private val ENCODING_LOOKUP = ENCODING_CHARS.toCharArray()

        private val DECODING_MAP: Map<Char, Int> =
            run {
                val base = mutableMapOf<Char, Int>()
                ENCODING_CHARS.withIndex().forEach { (idx, ch) ->
                    base[ch] = idx
                    base[ch.lowercaseChar()] = idx
                }

                base['I'] = base['1']!!
                base['i'] = base['1']!!
                base['L'] = base['1']!!
                base['l'] = base['1']!!
                base['O'] = base['0']!!
                base['o'] = base['0']!!
                base
            }

        private val secureRandom by lazy { SecureRandom() }

        @Volatile
        private var lastTimestamp: Long = -1L
        private var lastRandom: ByteArray = ByteArray(10)

        fun fromString(ulidString: String): ULID {
            require(ulidString.length == ULID_LENGTH) { "ULID must be exactly $ULID_LENGTH characters long." }
            require(ulidString.all { it in DECODING_MAP }) { "ULID contains invalid characters." }
            return ULID(ulidString.uppercase())
        }

        @Synchronized
        fun generate(): ULID {
            val bytes = ByteArray(16)

            val timestamp = System.currentTimeMillis()
            repeat(6) { i ->
                bytes[5 - i] = (timestamp ushr (8 * i)).toByte()
            }

            if (timestamp == lastTimestamp) {
                incrementRandom(lastRandom)
                System.arraycopy(lastRandom, 0, bytes, 6, 10)
            } else {
                secureRandom.nextBytes(bytes, 6, 10)
                System.arraycopy(bytes, 6, lastRandom, 0, 10)
                lastTimestamp = timestamp
            }

            return ULID(bytes.toBase32())
        }

        fun fromUUID(uuid: UUID): ULID {
            val bytes = ByteArray(16)
            val msb = uuid.mostSignificantBits
            val lsb = uuid.leastSignificantBits

            repeat(8) { i ->
                bytes[i] = (msb ushr (8 * (7 - i))).toByte()
                bytes[i + 8] = (lsb ushr (8 * (7 - i))).toByte()
            }
            return ULID(bytes.toBase32())
        }

        fun ULID.toUUID(): UUID {
            val bytes = value.fromBase32() // must be 16 bytes
            var msb = 0L
            var lsb = 0L

            repeat(8) { i ->
                msb = (msb shl 8) or (bytes[i].toLong() and 0xFF)
                lsb = (lsb shl 8) or (bytes[i + 8].toLong() and 0xFF)
            }
            return UUID(msb, lsb)
        }

        private fun ByteArray.toBase32(): String {
            require(size == 16) { "ULID byte array must be 16 bytes." }

            var bitBuffer = 0
            var bitCount = 0
            val output = StringBuilder(ULID_LENGTH)

            for (b in this) {
                bitBuffer = (bitBuffer shl 8) or (b.toInt() and 0xFF)
                bitCount += 8
                while (bitCount >= 5) {
                    val idx = (bitBuffer shr (bitCount - 5)) and 0x1F
                    output.append(ENCODING_LOOKUP[idx])
                    bitCount -= 5
                }
            }
            if (bitCount > 0) {
                val idx = (bitBuffer shl (5 - bitCount)) and 0x1F
                output.append(ENCODING_LOOKUP[idx])
            }
            // Ensure canonical ULID length of 26 characters
            val str = output.toString()
            require(str.length == ULID_LENGTH) { "Invalid ULID encoding length: ${str.length}" }
            return str
        }

        private fun String.fromBase32(): ByteArray {
            require(length == ULID_LENGTH) { "String must be exactly $ULID_LENGTH characters long." }

            val result = ByteArray(16)
            var bitBuffer = 0
            var bitCount = 0
            var idx = 0

            for (ch in this) {
                val v = DECODING_MAP[ch] ?: error("Invalid Base32 character: $ch")
                bitBuffer = (bitBuffer shl 5) or v
                bitCount += 5
                if (bitCount >= 8) {
                    result[idx++] = (bitBuffer shr (bitCount - 8)).toByte()
                    bitCount -= 8
                }
            }
            require(idx == 16) { "Decoded ULID did not produce 16 bytes (got $idx)." }
            return result
        }

        private fun SecureRandom.nextBytes(
            bytes: ByteArray,
            offset: Int,
            count: Int,
        ) {
            val slice = ByteArray(count).also { nextBytes(it) }
            System.arraycopy(slice, 0, bytes, offset, count)
        }

        private fun incrementRandom(random: ByteArray) {
            // Treat as 80-bit big-endian number and add 1
            var carry = 1
            for (i in random.lastIndex downTo 0) {
                val sum = (random[i].toInt() and 0xFF) + carry
                random[i] = sum.toByte()
                carry = sum ushr 8
                if (carry == 0) break
            }
        }
    }

    override fun toString(): String = value
}

fun String.toULID(): ULID = ULID.fromString(this)

fun UUID.toULID(): ULID = ULID.fromUUID(this)
