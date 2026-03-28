plugins {
    alias(libs.plugins.kotlin.jvm) apply false
    alias(libs.plugins.kotlin.serialization) apply false
    alias(libs.plugins.jib) apply false
    alias(libs.plugins.ktor.plugin) apply false
    alias(libs.plugins.openapi.generator) apply false
    alias(libs.plugins.ktlint) apply false
    alias(libs.plugins.detekt) apply false
    alias(libs.plugins.dependency.analysis) apply false
}
