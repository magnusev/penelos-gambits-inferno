plugins {
    alias(libs.plugins.convention.library)
    alias(libs.plugins.kotlin.serialization)
}

dependencies {
    implementation(libs.bundles.ktor.server)
    implementation(libs.bundles.ktor.client)

    implementation(libs.caffeine)
}
