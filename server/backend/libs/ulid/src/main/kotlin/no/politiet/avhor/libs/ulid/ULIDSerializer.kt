package no.politiet.avhor.libs.ulid

import kotlinx.serialization.KSerializer
import kotlinx.serialization.builtins.serializer
import kotlinx.serialization.encoding.Decoder
import kotlinx.serialization.encoding.Encoder

object ULIDSerializer : KSerializer<ULID> {
    override val descriptor = String.serializer().descriptor
    override fun deserialize(decoder: Decoder) = ULID.fromString(decoder.decodeString())
    override fun serialize(encoder: Encoder, value: ULID) = encoder.encodeString(value.toString())
}
