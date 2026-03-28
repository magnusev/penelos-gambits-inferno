package no.politiet.avhor.libs.ulid

import io.kotest.core.spec.style.FunSpec
import io.kotest.matchers.collections.shouldContainInOrder
import io.kotest.matchers.shouldBe
import no.politiet.avhor.libs.ulid.ULID.Companion.toUUID

class ULIDTest :
    FunSpec({

        test("ULID is sortable") {
            val ulid1 = ULID.generate()
            val ulid2 = ULID.generate()
            val ulid3 = ULID.generate()
            val ulid4 = ULID.generate()
            val ulid5 = ULID.generate()
            val ulid6 = ULID.generate()
            val ulid7 = ULID.generate()

            val unsorted = listOf(ulid7, ulid6, ulid5, ulid4, ulid3, ulid2, ulid1)

            val sorted = unsorted.sortedBy { it.toString() }

            sorted shouldContainInOrder listOf(ulid1, ulid2, ulid3, ulid4, ulid5, ulid6, ulid7)
        }

        test("ULID to UUID to ULID gives same ULID") {
            val ulid1 = ULID.generate()

            val uuid = ulid1.toUUID()

            val ulid2 = uuid.toULID()

            ulid1 shouldBe ulid2
        }
    })
