package com.penelosgambits.convention

import org.gradle.api.Project
import org.gradle.api.tasks.testing.Test
import org.gradle.api.tasks.testing.logging.TestExceptionFormat
import org.gradle.kotlin.dsl.withType

fun Project.applyTestConfiguration() {
    addDependencyBundle("kotest", configurationName = "testImplementation")
    addDependency("mockk", configurationName = "testImplementation")

    tasks.withType<Test> {
        useJUnitPlatform()

        testLogging {
            events("skipped", "failed")
            showCauses = true
            showExceptions = true
            exceptionFormat = TestExceptionFormat.FULL
        }
    }
}
