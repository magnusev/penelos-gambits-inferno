plugins {
    alias(libs.plugins.convention.library)
    alias(libs.plugins.kotlin.serialization)
}

dependencies {
    implementation(project(":backend:penelos-gambits-domain"))
    implementation(libs.kotlin.serialization)
}

