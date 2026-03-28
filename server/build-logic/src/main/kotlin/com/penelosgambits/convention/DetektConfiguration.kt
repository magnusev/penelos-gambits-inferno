package com.penelosgambits.convention

import io.gitlab.arturbosch.detekt.extensions.DetektExtension
import org.gradle.api.Project
import org.gradle.kotlin.dsl.configure

fun Project.applyDetektConfiguration() {
    pluginManager.apply("io.gitlab.arturbosch.detekt")

    configure<DetektExtension> {
        config.setFrom(files("$rootDir/detekt.yml"))
    }
}
